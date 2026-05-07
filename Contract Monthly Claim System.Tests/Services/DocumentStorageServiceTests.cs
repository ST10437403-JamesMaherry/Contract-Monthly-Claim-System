using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text;

namespace Contract_Monthly_Claim_System.Tests.Services
{
    public class DocumentStorageServiceTests : IDisposable
    {
        private readonly string _rootPath;
        private readonly DocumentStorageService _storageService;

        public DocumentStorageServiceTests()
        {
            _rootPath = Path.Combine(Path.GetTempPath(), "cmcs-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_rootPath);

            _storageService = new DocumentStorageService(
                Options.Create(new DocumentStorageOptions
                {
                    UploadPath = "uploads",
                    EncryptionKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("0123456789abcdef")),
                    AllowedExtensions = new[] { ".pdf", ".docx", ".xlsx" },
                    MaxFileSizeBytes = 1024,
                    MaxFilesPerClaim = 3
                }),
                new TestWebHostEnvironment(_rootPath));
        }

        [Fact]
        public async Task PrepareUploadAsync_AcceptsPdfWhenSignatureMatches()
        {
            var fileData = Encoding.ASCII.GetBytes("%PDF-1.7 test document");
            var file = CreateFormFile("invoice.pdf", fileData);

            var result = await _storageService.PrepareUploadAsync(file);

            Assert.True(result.Success);
            Assert.Equal("invoice.pdf", result.FileName);
            Assert.Equal(".pdf", result.FileExtension);
            Assert.Equal(fileData, result.FileData);
        }

        [Fact]
        public async Task PrepareUploadAsync_RejectsPdfWhenSignatureDoesNotMatch()
        {
            var file = CreateFormFile("invoice.pdf", Encoding.ASCII.GetBytes("not really a pdf"));

            var result = await _storageService.PrepareUploadAsync(file);

            Assert.False(result.Success);
            Assert.Contains("does not match", result.ErrorMessage);
        }

        [Fact]
        public async Task PrepareUploadAsync_RejectsFilesOverTheConfiguredLimit()
        {
            var file = CreateFormFile("large.pdf", Encoding.ASCII.GetBytes("%PDF-" + new string('x', 2048)));

            var result = await _storageService.PrepareUploadAsync(file);

            Assert.False(result.Success);
            Assert.Contains("maximum file size", result.ErrorMessage);
        }

        [Fact]
        public async Task SaveDocumentFileAsync_EncryptsFileAndReadsOriginalContent()
        {
            var documentId = 42;
            var fileData = Encoding.UTF8.GetBytes("claim evidence");

            await _storageService.SaveDocumentFileAsync(documentId, fileData);

            var storedPath = Path.Combine(_rootPath, "uploads", $"{documentId}.enc");
            var storedData = await File.ReadAllBytesAsync(storedPath);
            var fileHeader = Encoding.ASCII.GetBytes("CMSDOC1");

            Assert.True(File.Exists(storedPath));
            Assert.NotEqual(fileData, storedData);
            Assert.Equal(fileHeader, storedData.Take(fileHeader.Length).ToArray());

            var result = await _storageService.GetDocumentFileAsync(documentId);

            Assert.NotNull(result);
            Assert.Equal(fileData, result);
        }

        [Fact]
        public async Task GetDocumentFileAsync_ReturnsNullWhenEncryptedFileIsTampered()
        {
            var documentId = 58;

            await _storageService.SaveDocumentFileAsync(documentId, Encoding.UTF8.GetBytes("original document"));

            var storedPath = Path.Combine(_rootPath, "uploads", $"{documentId}.enc");
            var storedData = await File.ReadAllBytesAsync(storedPath);
            storedData[^1] ^= 0x01;
            await File.WriteAllBytesAsync(storedPath, storedData);

            var result = await _storageService.GetDocumentFileAsync(documentId);

            Assert.Null(result);
        }

        public void Dispose()
        {
            if (Directory.Exists(_rootPath))
                Directory.Delete(_rootPath, recursive: true);
        }

        private static FormFile CreateFormFile(string fileName, byte[] fileData)
        {
            var stream = new MemoryStream(fileData);

            return new FormFile(stream, 0, stream.Length, "documents", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };
        }

        private class TestWebHostEnvironment : IWebHostEnvironment
        {
            public TestWebHostEnvironment(string contentRootPath)
            {
                ContentRootPath = contentRootPath;
                ContentRootFileProvider = new NullFileProvider();
                WebRootPath = contentRootPath;
                WebRootFileProvider = new NullFileProvider();
            }

            public string ApplicationName { get; set; } = "Contract Monthly Claim System Tests";
            public IFileProvider ContentRootFileProvider { get; set; }
            public string ContentRootPath { get; set; }
            public string EnvironmentName { get; set; } = Environments.Development;
            public IFileProvider WebRootFileProvider { get; set; }
            public string WebRootPath { get; set; }
        }
    }
}
