using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.Http;

namespace Contract_Monthly_Claim_System.Tests.Services
{
    public class DocumentAccessServiceTests
    {
        [Fact]
        public async Task GetAuthorizedDownloadAsync_AllowsLecturerToDownloadTheirOwnDocument()
        {
            var fileData = new byte[] { 1, 2, 3 };
            var service = CreateService(
                CreateDocument(7, 12, ClaimStatusType.Submitted, ".pdf"),
                fileData);

            var result = await service.GetAuthorizedDownloadAsync(7, 12, nameof(UserRoleType.Lecturer));

            Assert.True(result.Found);
            Assert.True(result.Authorized);
            Assert.Equal("supporting-document.pdf", result.FileName);
            Assert.Equal("application/pdf", result.ContentType);
            Assert.Equal(fileData, result.FileData);
        }

        [Fact]
        public async Task GetAuthorizedDownloadAsync_DeniesLecturerForAnotherUsersDocument()
        {
            var service = CreateService(
                CreateDocument(7, 12, ClaimStatusType.Submitted, ".pdf"),
                new byte[] { 1, 2, 3 });

            var result = await service.GetAuthorizedDownloadAsync(7, 99, nameof(UserRoleType.Lecturer));

            Assert.True(result.Found);
            Assert.False(result.Authorized);
            Assert.Null(result.FileData);
        }

        [Fact]
        public async Task GetAuthorizedDownloadAsync_DeniesCoordinatorForManagerOnlyStatus()
        {
            var service = CreateService(
                CreateDocument(9, 12, ClaimStatusType.Paid, ".pdf"),
                new byte[] { 1, 2, 3 });

            var result = await service.GetAuthorizedDownloadAsync(9, 3, nameof(UserRoleType.Coordinator));

            Assert.True(result.Found);
            Assert.False(result.Authorized);
        }

        [Fact]
        public async Task GetAuthorizedDownloadAsync_ReturnsMissingWhenStoredFileIsGone()
        {
            var service = CreateService(
                CreateDocument(10, 12, ClaimStatusType.ApprovedByCoordinator, ".docx"),
                null);

            var result = await service.GetAuthorizedDownloadAsync(10, 4, nameof(UserRoleType.Manager));

            Assert.False(result.Found);
            Assert.False(result.Authorized);
        }

        private static DocumentAccessService CreateService(Document? document, byte[]? fileData)
        {
            return new DocumentAccessService(
                new StubDataService(document),
                new StubDocumentStorageService(fileData));
        }

        private static Document CreateDocument(int documentId, int ownerUserId, ClaimStatusType status, string fileType)
        {
            return new Document
            {
                documentId = documentId,
                claimId = 100,
                fileName = $"supporting-document{fileType}",
                fileType = fileType,
                fileSize = 100,
                uploadDate = DateTime.UtcNow,
                Claim = new Claim
                {
                    claimId = 100,
                    userId = ownerUserId,
                    statusId = (int)status,
                    User = default!,
                    Status = default!
                }
            };
        }

        private class StubDataService : IDataService
        {
            private readonly Document? _document;

            public StubDataService(Document? document)
            {
                _document = document;
            }

            public Task<List<Claim>> GetClaimsAsync() => throw new NotImplementedException();
            public Task<Claim?> GetClaimAsync(int claimId) => throw new NotImplementedException();
            public Task AddClaimAsync(Claim claim) => throw new NotImplementedException();
            public Task UpdateClaimAsync(Claim claim) => throw new NotImplementedException();
            public Task<List<ClaimReview>> GetClaimReviewsAsync() => throw new NotImplementedException();
            public Task<List<Document>> GetDocumentsAsync() => throw new NotImplementedException();
            public Task AddDocumentAsync(Document document) => throw new NotImplementedException();
            public Task<List<User>> GetUsersAsync() => throw new NotImplementedException();
            public Task AddUserAsync(User user) => throw new NotImplementedException();
            public Task UpdateUserAsync(User user) => throw new NotImplementedException();
            public Task<List<ClaimStatus>> GetClaimStatusesAsync() => throw new NotImplementedException();

            public Task<Document?> GetDocumentAsync(int documentId)
            {
                return Task.FromResult(_document?.documentId == documentId ? _document : null);
            }
        }

        private class StubDocumentStorageService : IDocumentStorageService
        {
            private readonly byte[]? _fileData;

            public StubDocumentStorageService(byte[]? fileData)
            {
                _fileData = fileData;
            }

            public int MaxFilesPerClaim => 5;
            public long MaxFileSizeBytes => 10 * 1024 * 1024;
            public IReadOnlyCollection<string> AllowedExtensions => new[] { ".pdf", ".docx", ".xlsx" };

            public Task<DocumentUploadResult> PrepareUploadAsync(IFormFile file) => throw new NotImplementedException();
            public Task SaveDocumentFileAsync(int documentId, byte[] fileData) => throw new NotImplementedException();

            public Task<byte[]?> GetDocumentFileAsync(int documentId)
            {
                return Task.FromResult(_fileData);
            }
        }
    }
}
