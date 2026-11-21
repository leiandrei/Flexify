using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GymCrudApp.Controllers;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.ReceiptOcr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace GymCrudApp.Tests
{
    public class ReceiptOcrServiceTests
    {
        private class StubOcrService : IOcrService
        {
            private readonly string _cannedText;
            public StubOcrService(string cannedText) => _cannedText = cannedText;

            public Task<string> ExtractRawTextAsync(
                byte[] fileBytes,
                string mimeType,
                CancellationToken cancellationToken = default)
                => Task.FromResult(_cannedText);
        }

        private class FakeWebHostEnvironment : IWebHostEnvironment
        {
            public string WebRootPath { get; set; }
            public string ContentRootPath { get; set; }
            public string EnvironmentName { get; set; } = "Development";
            public string ApplicationName { get; set; } = "GymCrudApp";
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

            public FakeWebHostEnvironment(string root)
            {
                WebRootPath = root;
                ContentRootPath = root;
            }
        }

        private static GymDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new GymDbContext(options);

            context.Staff.Add(new Staff
            {
                StaffID = 1,
                userId = 5,
                fullName = "Marco Santos",
                position = "Receptionist",
                dateOfBirth = new DateTime(1998, 5, 12)
            });

            context.MembershipPlans.Add(new MembershipPlans
            {
                planId = 1,
                planName = "Monthly Basic",
                planPrice = 1500m,
                durationDays = 30,
                planDescription = "30-day access",
                isActive = true
            });

            context.Members.Add(new Members
            {
                memberId = 1,
                userId = 10,
                fullName = "Andrea Reyes",
                userEmail = "andrea@gmail.com",
                contactNumber = "09171234567",
                birthdate = new DateTime(2000, 4, 15),
                joinDate = new DateTime(2026, 10, 1),
                isActive = true
            });

            context.Memberships.Add(new Memberships
            {
                membershipId = 1,
                memberId = 1,
                planId = 1,
                startDate = new DateTime(2026, 11, 1),
                endDate = new DateTime(2026, 12, 1),
                isActive = true
            });

            context.SaveChanges();
            return context;
        }

        private static IFormFile CreateFormFile(string fileName, byte[] content)
        {
            var stream = new MemoryStream(content);
            return new FormFile(stream, 0, content.Length, "ReceiptFile", fileName);
        }

        private static ClaimsPrincipal CreateStaffPrincipal(int userId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Role, "Staff")
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        [Fact]
        public void ValidateUpload_RejectsOversizedAndUnsupportedFiles()
        {
            using var context = CreateContext();
            var service = new ReceiptOcrService(context, new StubOcrService(""), new FakeWebHostEnvironment(Path.GetTempPath()));

            var exeFile = CreateFormFile("payload.exe", new byte[100]);
            var (exeValid, exeError) = service.ValidateUpload(exeFile);
            Assert.False(exeValid);
            Assert.Contains("Unsupported file type", exeError);

            var largeFile = CreateFormFile("receipt.jpg", new byte[ReceiptOcrService.MaxFileSizeBytes + 1]);
            var (sizeValid, sizeError) = service.ValidateUpload(largeFile);
            Assert.False(sizeValid);
            Assert.Contains("5 MB", sizeError);

            var validPdf = CreateFormFile("gcash.pdf", new byte[1024]);
            var (pdfValid, pdfError) = service.ValidateUpload(validPdf);
            Assert.True(pdfValid);
            Assert.Null(pdfError);
        }

        [Fact]
        public void ParseReceiptText_ExtractsAmountNearestTotalDateAndReferenceNumber()
        {
            using var context = CreateContext();
            var service = new ReceiptOcrService(context, new StubOcrService(""), new FakeWebHostEnvironment(Path.GetTempPath()));

            var sampleGcashText = """
                GCash Express Send
                Flexify Fitness Hub
                Subtotal: PHP 1,450.00
                Convenience Fee: PHP 50.00
                Total Amount Paid: PHP 1,500.00
                Ref No. 9012847563210
                Nov 20, 2026 10:15 AM
                """;

            var parsed = service.ParseReceiptText(sampleGcashText);

            Assert.Equal(1500.00m, parsed.Amount);
            Assert.Equal(new DateTime(2026, 11, 20), parsed.PaymentDate);
            Assert.Equal("9012847563210", parsed.ReferenceNumber);
            Assert.Equal("GCash", parsed.PaymentType);
            Assert.Equal(3, parsed.ExtractedFieldCount);
        }

        [Fact]
        public async Task ProcessReceiptUploadAsync_WhenOcrFindsNothing_OpensBlankFormFlowSafely()
        {
            using var context = CreateContext();
            var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            try
            {
                var service = new ReceiptOcrService(context, new StubOcrService(""), new FakeWebHostEnvironment(tempRoot));
                var upload = CreateFormFile("blurry_receipt.png", Encoding.UTF8.GetBytes("fake-png-bytes"));

                var (success, error, model) = await service.ProcessReceiptUploadAsync(upload);

                Assert.True(success);
                Assert.Null(error);
                Assert.NotNull(model);
                Assert.True(model!.OcrFoundNothing);
                Assert.Null(model.Amount);
                Assert.Null(model.ReferenceNumber);
                Assert.StartsWith("/uploads/receipts/", model.ReceiptImagePath);
                Assert.DoesNotContain("blurry_receipt", model.ReceiptImagePath);
                Assert.Single(model.AvailableMemberships);
                Assert.Empty(context.Payments);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Fact]
        public async Task ConfirmAndSavePaymentAsync_SavesPaymentWithReceiptPathAndOcrRawText()
        {
            using var context = CreateContext();
            var service = new ReceiptOcrService(context, new StubOcrService(""), new FakeWebHostEnvironment(Path.GetTempPath()));
            var staffUser = CreateStaffPrincipal(5);

            var confirmModel = new ReceiptConfirmViewModel
            {
                ReceiptImagePath = "/uploads/receipts/abc123.jpg",
                OcrRawText = "GCash Total: 1500.00 Ref No. 8899776655",
                MembershipId = 1,
                Amount = 1500m,
                PaymentType = "GCash",
                ReferenceNumber = "8899776655",
                PaymentDate = new DateTime(2026, 11, 21)
            };

            var (success, error, paymentId) = await service.ConfirmAndSavePaymentAsync(confirmModel, staffUser);

            Assert.True(success);
            Assert.Null(error);
            Assert.NotNull(paymentId);

            var savedPayment = await context.Payments.SingleAsync();
            Assert.Equal(1, savedPayment.recordedByStaffId);
            Assert.Equal(1500m, savedPayment.amount);
            Assert.Equal("8899776655", savedPayment.referenceNumber);
            Assert.Equal("/uploads/receipts/abc123.jpg", savedPayment.receiptImagePath);
            Assert.Equal("GCash Total: 1500.00 Ref No. 8899776655", savedPayment.ocrRawText);
            Assert.False(savedPayment.isVoided);
        }

        [Fact]
        public void ReceiptOcrController_RequiresAdminOrStaffAndDependsOnlyOnIReceiptOcrService()
        {
            var authorizeAttr = typeof(ReceiptOcrController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(authorizeAttr);
            Assert.Equal("Admin,Staff", authorizeAttr!.Roles);

            var ctorParams = typeof(ReceiptOcrController)
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Single()
                .GetParameters();

            Assert.Single(ctorParams);
            Assert.Equal(typeof(IReceiptOcrService), ctorParams[0].ParameterType);
        }
    }
}