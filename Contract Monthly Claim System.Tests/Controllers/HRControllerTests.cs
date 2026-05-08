using Contract_Monthly_Claim_System.Controllers;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Contract_Monthly_Claim_System.Tests.Controllers
{
    public class HRControllerTests
    {
        [Fact]
        public async Task AddUser_Post_MarksNewUserForFirstLoginPasswordChange()
        {
            var dataService = new StubDataService();
            var authService = new StubAuthenticationService();
            var controller = new HRController(
                dataService,
                new StubPdfService(),
                new StubReportExportService(),
                authService)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), new StubTempDataProvider())
            };

            var user = new User
            {
                firstName = "New",
                lastName = "Lecturer",
                email = "newlecturer@university.co.za",
                phoneNumber = "+27 11 000 0000",
                userRole = "Lecturer",
                hourlyRate = 175
            };

            var result = await controller.AddUser(user, "TempPass123", "TempPass123");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("HRDashboard", redirect.ActionName);
            Assert.NotNull(dataService.AddedUser);
            Assert.Equal("hashed-TempPass123", dataService.AddedUser.passwordHash);
            Assert.Equal("test-salt", dataService.AddedUser.passwordSalt);
            Assert.True(dataService.AddedUser.mustChangePassword);
        }

        private class StubDataService : IDataService
        {
            public User? AddedUser { get; private set; }

            public Task<List<User>> GetUsersAsync() => Task.FromResult(new List<User>());

            public Task AddUserAsync(User user)
            {
                AddedUser = user;
                return Task.CompletedTask;
            }

            public Task<List<Claim>> GetClaimsAsync() => throw new NotImplementedException();
            public Task<Claim?> GetClaimAsync(int claimId) => throw new NotImplementedException();
            public Task AddClaimAsync(Claim claim) => throw new NotImplementedException();
            public Task UpdateClaimAsync(Claim claim) => throw new NotImplementedException();
            public Task<List<ClaimReview>> GetClaimReviewsAsync() => throw new NotImplementedException();
            public Task<List<Document>> GetDocumentsAsync() => throw new NotImplementedException();
            public Task<Document?> GetDocumentAsync(int documentId) => throw new NotImplementedException();
            public Task AddDocumentAsync(Document document) => throw new NotImplementedException();
            public Task UpdateUserAsync(User user) => throw new NotImplementedException();
            public Task<List<ClaimStatus>> GetClaimStatusesAsync() => throw new NotImplementedException();
        }

        private class StubAuthenticationService : IAuthenticationService
        {
            public string HashPassword(string password, out string salt)
            {
                salt = "test-salt";
                return $"hashed-{password}";
            }

            public bool VerifyPassword(string password, string hash, string salt) => throw new NotImplementedException();
            public Task<User?> ValidateUserAsync(string email, string password) => throw new NotImplementedException();
            public Task<bool> SetUserPasswordAsync(int userId, string password, bool mustChangePassword = false) => throw new NotImplementedException();
            public Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword) => throw new NotImplementedException();
        }

        private class StubPdfService : IPdfService
        {
            public byte[] GeneratePaymentReport(List<Claim> claims, List<User> users) => throw new NotImplementedException();
            public byte[] GenerateInvoice(Claim claim, User user) => throw new NotImplementedException();
            public byte[] GenerateUserReport(List<User> users, string? filterRole = null) => throw new NotImplementedException();
        }

        private class StubReportExportService : IReportExportService
        {
            public byte[] GeneratePaymentReportCsv(List<Claim> claims, List<User> users) => throw new NotImplementedException();
            public byte[] GenerateUserReportCsv(List<User> users, string? filterRole = null) => throw new NotImplementedException();
            public byte[] GenerateMonthlyPaymentBatchCsv(List<Claim> claims, List<User> users, int year, int month) => throw new NotImplementedException();
            public List<Claim> GetMonthlyPaymentBatchClaims(List<Claim> claims, int year, int month) => throw new NotImplementedException();
        }

        private class StubTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
            public void SaveTempData(HttpContext context, IDictionary<string, object> values)
            {
            }
        }
    }
}
