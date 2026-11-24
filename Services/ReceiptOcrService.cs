using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.ViewModels.ReceiptOcr;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Services
{
    public class ReceiptOcrService : IReceiptOcrService
    {
        public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".pdf"
        };

        private readonly GymDbContext _context;
        private readonly IOcrService _ocrService;
        private readonly IWebHostEnvironment _environment;

        public ReceiptOcrService(
            GymDbContext context,
            IOcrService ocrService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _ocrService = ocrService;
            _environment = environment;
        }

        public (bool IsValid, string? ErrorMessage) ValidateUpload(IFormFile? receiptFile)
        {
            if (receiptFile == null || receiptFile.Length == 0)
            {
                return (false, "Please select a non-empty receipt file.");
            }

            if (receiptFile.Length > MaxFileSizeBytes)
            {
                return (false, "Receipt file exceeds the 5 MB size limit.");
            }

            var extension = Path.GetExtension(receiptFile.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                return (false, "Unsupported file type. Upload a JPG, PNG, WebP, or PDF receipt.");
            }

            return (true, null);
        }

        public Task<(bool Success, string? ErrorMessage, ReceiptConfirmViewModel? ConfirmModel)> ProcessReceiptUploadAsync(
            IFormFile receiptFile,
            CancellationToken cancellationToken = default)
            => ProcessReceiptUploadAsync(receiptFile, null, cancellationToken);

        public async Task<(bool Success, string? ErrorMessage, ReceiptConfirmViewModel? ConfirmModel)> ProcessReceiptUploadAsync(
            IFormFile receiptFile,
            string? clientRawText,
            CancellationToken cancellationToken = default)
        {
            var (isValid, validationError) = ValidateUpload(receiptFile);
            if (!isValid)
            {
                return (false, validationError, null);
            }

            var extension = Path.GetExtension(receiptFile.FileName).ToLowerInvariant();
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "receipts");
            Directory.CreateDirectory(uploadsFolder);

            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadsFolder, storedFileName);
            var relativePath = $"/uploads/receipts/{storedFileName}";

            byte[] fileBytes;
            await using (var ms = new MemoryStream())
            {
                await receiptFile.CopyToAsync(ms, cancellationToken);
                fileBytes = ms.ToArray();
            }

            await File.WriteAllBytesAsync(physicalPath, fileBytes, cancellationToken);

            var rawText = clientRawText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                var mimeType = extension switch
                {
                    ".pdf" => "application/pdf",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".webp" => "image/webp",
                    _ => "image/png"
                };

                rawText = await _ocrService.ExtractRawTextAsync(fileBytes, mimeType, cancellationToken);
            }

            var candidates = ParseReceiptText(rawText);

            var confirmModel = new ReceiptConfirmViewModel
            {
                ReceiptImagePath = relativePath,
                OcrRawText = string.IsNullOrWhiteSpace(rawText) ? null : rawText,
                Amount = candidates.Amount,
                PaymentType = candidates.PaymentType ?? "GCash",
                ReferenceNumber = candidates.ReferenceNumber,
                PaymentDate = candidates.PaymentDate ?? DateTime.Today,
                ExtractedFieldCount = candidates.ExtractedFieldCount,
                OcrFoundNothing = candidates.ExtractedFieldCount == 0
            };

            await PrepareConfirmationModelAsync(confirmModel, cancellationToken);
            return (true, null, confirmModel);
        }

        public async Task PrepareConfirmationModelAsync(
            ReceiptConfirmViewModel model,
            CancellationToken cancellationToken = default)
        {
            var memberships = await _context.Memberships
                .Include(m => m.Member)
                .Include(m => m.Plan)
                .OrderByDescending(m => m.isActive)
                .ThenBy(m => m.Member != null ? m.Member.fullName : string.Empty)
                .ToListAsync(cancellationToken);

            model.AvailableMemberships = memberships
                .Where(m => m.Member != null && m.Plan != null)
                .Select(m => new MembershipOptionViewModel
                {
                    MembershipId = m.membershipId,
                    DisplayLabel = $"{m.Member!.fullName} — {m.Plan!.planName} ({(m.isActive ? "Active" : "Inactive")}, ends {m.endDate:MMM d, yyyy})",
                    PlanPrice = m.Plan!.planPrice
                })
                .ToList();
        }

        public async Task<(bool Success, string? ErrorMessage, int? PaymentId)> ConfirmAndSavePaymentAsync(
            ReceiptConfirmViewModel model,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            var membership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.membershipId == model.MembershipId, cancellationToken);

            if (membership == null)
            {
                return (false, "Selected membership could not be found.", null);
            }

            var cleanedRef = string.IsNullOrWhiteSpace(model.ReferenceNumber)
                ? null
                : Regex.Replace(model.ReferenceNumber.Trim(), @"\s+", "");

            if (!string.Equals(model.PaymentType, "Cash", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(cleanedRef))
            {
                return (false, "Reference number is required for non-cash payments.", null);
            }

            var staffId = await ResolveRecordingStaffIdAsync(user, cancellationToken);
            if (staffId == 0)
            {
                return (false, "No staff profile found to record this payment.", null);
            }

            var payment = new Payments
            {
                memberId = membership.memberId,
                membershipId = membership.membershipId,
                amount = model.Amount!.Value,
                paymentType = model.PaymentType,
                referenceNumber = cleanedRef,
                receiptImagePath = model.ReceiptImagePath,
                ocrRawText = model.OcrRawText,
                isVoided = false,
                paymentDate = model.PaymentDate.Date == DateTime.Today
                    ? DateTime.Now
                    : model.PaymentDate,
                recordedByStaffId = staffId
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);

            return (true, null, payment.paymentId);
        }

        private async Task<int> ResolveRecordingStaffIdAsync(
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idClaim, out var claimUserId))
            {
                var staffByUserId = await _context.Staff
                    .FirstOrDefaultAsync(s => s.userId == claimUserId, cancellationToken);

                if (staffByUserId != null)
                {
                    return staffByUserId.StaffID;
                }
            }

            var email = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(email))
            {
                var appUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.userEmail == email, cancellationToken);

                if (appUser != null)
                {
                    var matchingStaff = await _context.Staff
                        .FirstOrDefaultAsync(s => s.userId == appUser.userId, cancellationToken);

                    if (matchingStaff != null)
                    {
                        return matchingStaff.StaffID;
                    }
                }
            }

            var fallbackStaff = await _context.Staff
                .OrderBy(s => s.StaffID)
                .FirstOrDefaultAsync(cancellationToken);

            return fallbackStaff?.StaffID ?? 0;
        }

        public ParsedReceiptCandidates ParseReceiptText(string? rawText)
        {
            var result = new ParsedReceiptCandidates { RawText = rawText ?? string.Empty };
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return result;
            }

            // 1. Detect payment method
            if (Regex.IsMatch(rawText, @"\bgcash\b", RegexOptions.IgnoreCase))
            {
                result.PaymentType = "GCash";
            }
            else if (Regex.IsMatch(rawText, @"\b(pay)?maya\b", RegexOptions.IgnoreCase))
            {
                result.PaymentType = "Maya";
            }
            else if (Regex.IsMatch(rawText, @"\b(bank|bpi|bdo|unionbank|metrobank|landbank|instapay|pesonet|transfer)\b", RegexOptions.IgnoreCase))
            {
                result.PaymentType = "Bank Transfer";
            }
            else if (Regex.IsMatch(rawText, @"\b(visa|mastercard|card)\b", RegexOptions.IgnoreCase))
            {
                result.PaymentType = "Card";
            }

            // 2. Extract amount — prioritize "Total" labels before subtotals or generic currency values
            // 2. Extract amount — prioritize standalone "Total" labels (ignoring "Subtotal")
            var amountMatch = Regex.Match(
                rawText,
                @"\b(?:Total(?:\s*(?:Amount\s*Paid|Amount|Due|Paid))?|Amount\s*Paid)\s*:?\s*(?:PHP|Php|₱)?\s*([0-9]{1,3}(?:,[0-9]{3})*(?:\.[0-9]{2})|[0-9]+(?:\.[0-9]{2}))",
                RegexOptions.IgnoreCase);

            if (!amountMatch.Success)
            {
                amountMatch = Regex.Match(
                    rawText,
                    @"(?:PHP|Php|₱|Amount|Sent)\s*:?\s*₱?\s*([0-9]{1,3}(?:,[0-9]{3})*(?:\.[0-9]{2})|[0-9]+(?:\.[0-9]{2}))",
                    RegexOptions.IgnoreCase);
            }

            if (!amountMatch.Success)
            {
                amountMatch = Regex.Match(rawText, @"₱?\s*([0-9]{1,3}(?:,[0-9]{3})*\.[0-9]{2})");
            }

            if (amountMatch.Success &&
                decimal.TryParse(
                    amountMatch.Groups[1].Value.Replace(",", ""),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var parsedAmount) &&
                parsedAmount > 0)
            {
                result.Amount = parsedAmount;
            }

            // 3. Extract reference number
            var refMatch = Regex.Match(
                rawText,
                @"(?:Ref(?:erence)?\s*(?:No\.?|Number|#)?|Trace\s*No\.?|Transaction\s*ID)\s*:?[ \t]*([A-Za-z0-9][A-Za-z0-9 \t-]{4,35})",
                RegexOptions.IgnoreCase);

            if (refMatch.Success)
            {
                var normalizedRef = Regex.Replace(refMatch.Groups[1].Value.Trim(), @"\s+", "");
                normalizedRef = Regex.Replace(normalizedRef, @"[^a-zA-Z0-9-]", "");
                if (normalizedRef.Length >= 5)
                {
                    result.ReferenceNumber = normalizedRef;
                }
            }

            // 4. Extract payment date
            var dateMatch = Regex.Match(
                rawText,
                @"\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\.?\s+\d{1,2},?\s+\d{4}\b|\b\d{4}-\d{2}-\d{2}\b|\b\d{1,2}/\d{1,2}/\d{4}\b",
                RegexOptions.IgnoreCase);

            if (dateMatch.Success &&
                DateTime.TryParse(dateMatch.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) &&
                parsedDate.Year >= 2020 &&
                parsedDate.Date <= DateTime.Today.AddDays(1))
            {
                result.PaymentDate = parsedDate.Date;
            }

            return result;
        }
    }
}