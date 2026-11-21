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
        public const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private static readonly Dictionary<string, string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".pdf"] = "application/pdf"
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

        public (bool IsValid, string? ErrorMessage) ValidateUpload(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return (false, "Please select a non-empty receipt file (JPG, PNG, or PDF).");

            if (file.Length > MaxFileSizeBytes)
                return (false, "Receipt file size must be 5 MB or less.");

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.ContainsKey(extension))
                return (false, "Unsupported file type. Only JPG, PNG, and PDF receipts are allowed.");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage, ReceiptConfirmViewModel? Model)> ProcessReceiptUploadAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            var (isValid, error) = ValidateUpload(file);
            if (!isValid)
                return (false, error, null);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var mimeType = AllowedExtensions[extension];

            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms, cancellationToken);
                fileBytes = ms.ToArray();
            }

            var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
                : _environment.WebRootPath;

            var uploadsFolder = Path.Combine(webRoot, "uploads", "receipts");
            Directory.CreateDirectory(uploadsFolder);

            var randomFileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadsFolder, randomFileName);
            await File.WriteAllBytesAsync(physicalPath, fileBytes, cancellationToken);

            var relativePath = $"/uploads/receipts/{randomFileName}";

            var rawText = await _ocrService.ExtractRawTextAsync(fileBytes, mimeType, cancellationToken);
            var candidates = ParseReceiptText(rawText);

            var model = new ReceiptConfirmViewModel
            {
                ReceiptImagePath = relativePath,
                OcrRawText = string.IsNullOrWhiteSpace(rawText) ? null : rawText,
                Amount = candidates.Amount,
                PaymentDate = candidates.PaymentDate ?? DateTime.Today,
                ReferenceNumber = candidates.ReferenceNumber,
                PaymentType = candidates.PaymentType ?? "GCash",
                ExtractedFieldCount = candidates.ExtractedFieldCount,
                OcrFoundNothing = candidates.ExtractedFieldCount == 0
            };

            await PrepareConfirmationModelAsync(model, cancellationToken);
            return (true, null, model);
        }

        public ParsedReceiptCandidates ParseReceiptText(string rawText)
        {
            var result = new ParsedReceiptCandidates
            {
                RawText = rawText ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(rawText))
                return result;

            result.Amount = ExtractAmountNearestTotal(rawText);
            result.PaymentDate = ExtractDate(rawText);
            result.ReferenceNumber = ExtractReferenceNumber(rawText);
            result.PaymentType = DetectPaymentType(rawText);

            return result;
        }

        public async Task<ReceiptConfirmViewModel> PrepareConfirmationModelAsync(
            ReceiptConfirmViewModel model,
            CancellationToken cancellationToken = default)
        {
            var memberships = await _context.Memberships
                .Include(m => m.Member)
                .Include(m => m.Plan)
                .OrderByDescending(m => m.isActive)
                .ThenByDescending(m => m.endDate)
                .Select(m => new MembershipOptionViewModel
                {
                    MembershipId = m.membershipId,
                    PlanPrice = m.Plan != null ? m.Plan.planPrice : 0m,
                    DisplayLabel = $"{(m.Member != null ? m.Member.fullName : "Member #" + m.memberId)} — {(m.Plan != null ? m.Plan.planName : "Plan")} (Expires {m.endDate:MMM dd, yyyy})"
                })
                .ToListAsync(cancellationToken);

            model.AvailableMemberships = memberships;
            return model;
        }

        public async Task<(bool Success, string? ErrorMessage, int? PaymentId)> ConfirmAndSavePaymentAsync(
            ReceiptConfirmViewModel model,
            ClaimsPrincipal staffUser,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(model.ReceiptImagePath))
                return (false, "Receipt image path is required.", null);

            if (!model.Amount.HasValue || model.Amount.Value <= 0)
                return (false, "Payment amount must be greater than zero.", null);

            if (string.IsNullOrWhiteSpace(model.PaymentType))
                return (false, "Payment type is required.", null);

            var requiresReference = !model.PaymentType.Equals("Cash", StringComparison.OrdinalIgnoreCase);
            if (requiresReference && string.IsNullOrWhiteSpace(model.ReferenceNumber))
                return (false, $"A reference number is required for {model.PaymentType} payments.", null);

            var membershipExists = await _context.Memberships
                .AnyAsync(m => m.membershipId == model.MembershipId, cancellationToken);
            if (!membershipExists)
                return (false, "Selected membership record was not found.", null);

            var staffId = await ResolveStaffIdAsync(staffUser, cancellationToken);
            if (!staffId.HasValue)
                return (false, "Unable to identify the logged-in staff account.", null);

            var payment = new Payments
            {
                membershipId = model.MembershipId,
                recordedByStaffId = staffId.Value,
                amount = model.Amount.Value,
                paymentType = model.PaymentType.Trim(),
                referenceNumber = string.IsNullOrWhiteSpace(model.ReferenceNumber)
                    ? null
                    : model.ReferenceNumber.Trim(),
                paymentDate = model.PaymentDate,
                isVoided = false,
                receiptImagePath = model.ReceiptImagePath.Trim(),
                ocrRawText = model.OcrRawText
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);

            return (true, null, payment.paymentId);
        }

        private async Task<int?> ResolveStaffIdAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            var userIdStr = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
            {
                var staff = await _context.Staff
                    .FirstOrDefaultAsync(s => s.userId == userId, cancellationToken);
                if (staff != null)
                    return staff.StaffID;
            }

            var fallbackStaff = await _context.Staff.FirstOrDefaultAsync(cancellationToken);
            return fallbackStaff?.StaffID;
        }

        private static decimal? ExtractAmountNearestTotal(string text)
        {
            var totalRegex = new Regex(
                @"(?i)\btotal(?:\s+(?:amount|paid|due|payment))?\b[^\d\r\n]{0,20}(?:₱|PHP|Php|P)?\s*([0-9]{1,3}(?:,[0-9]{3})*(?:\.[0-9]{2})|[0-9]+(?:\.[0-9]{2})?)",
                RegexOptions.Multiline);

            var totalMatches = totalRegex.Matches(text);
            if (totalMatches.Count > 0)
            {
                var rawNum = totalMatches[^1].Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(rawNum, NumberStyles.Number, CultureInfo.InvariantCulture, out var totalVal) && totalVal > 0)
                    return totalVal;
            }

            var currencyRegex = new Regex(
                @"(?i)(?:\bamount(?:\s+paid|\s+sent)?\b[^\d\r\n]{0,15}|(?:₱|PHP)\s*)([0-9]{1,3}(?:,[0-9]{3})*(?:\.[0-9]{2})|[0-9]+\.[0-9]{2})");

            var currencyMatch = currencyRegex.Match(text);
            if (currencyMatch.Success)
            {
                var rawNum = currencyMatch.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(rawNum, NumberStyles.Number, CultureInfo.InvariantCulture, out var currVal) && currVal > 0)
                    return currVal;
            }

            return null;
        }

        private static DateTime? ExtractDate(string text)
        {
            var datePatterns = new[]
            {
                @"\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec)[a-z]*\.?\s+\d{1,2},?\s+\d{4}\b",
                @"\b\d{1,2}\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec)[a-z]*\.?\s+\d{4}\b",
                @"\b\d{4}-\d{2}-\d{2}\b",
                @"\b\d{1,2}/\d{1,2}/\d{4}\b"
            };

            foreach (var pattern in datePatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && DateTime.TryParse(match.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return parsed.Date;
                }
            }

            return null;
        }

        private static string? ExtractReferenceNumber(string text)
        {
            var refRegex = new Regex(
                @"(?i)\b(?:ref(?:erence)?\s*(?:no\.?|number|#|:)?|trace\s*(?:no\.?|#|:)?|transaction\s*(?:id|no\.?|#|:))\s*[:#-]?\s*([A-Z0-9-]{6,25})\b");

            var match = refRegex.Match(text);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private static string? DetectPaymentType(string text)
        {
            if (Regex.IsMatch(text, @"\bgcash\b", RegexOptions.IgnoreCase)) return "GCash";
            if (Regex.IsMatch(text, @"\bpaymaya\b|\bmaya\b", RegexOptions.IgnoreCase)) return "Maya";
            if (Regex.IsMatch(text, @"\binstapay\b|\bpesonet\b|\bbank\s*transfer\b|\bbpi\b|\bbdo\b|\bunionbank\b", RegexOptions.IgnoreCase)) return "Bank Transfer";
            if (Regex.IsMatch(text, @"\bvisa\b|\bmastercard\b|\bcredit\s*card\b|\bdebit\s*card\b", RegexOptions.IgnoreCase)) return "Card";
            return null;
        }
    }
}