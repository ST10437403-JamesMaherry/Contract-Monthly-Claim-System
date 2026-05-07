using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Contract_Monthly_Claim_System.Services
{
    public interface IDocumentStorageService
    {
        int MaxFilesPerClaim { get; }
        long MaxFileSizeBytes { get; }
        IReadOnlyCollection<string> AllowedExtensions { get; }
        Task<DocumentUploadResult> PrepareUploadAsync(IFormFile file);
        Task SaveDocumentFileAsync(int documentId, byte[] fileData);
        Task<byte[]?> GetDocumentFileAsync(int documentId);
    }

    public class DocumentStorageService : IDocumentStorageService
    {
        private static readonly byte[] EncryptedFileHeader = Encoding.ASCII.GetBytes("CMSDOC1");
        private const int GcmNonceSize = 12;
        private const int GcmTagSize = 16;
        private const int LegacyIvSize = 16;

        private readonly DocumentStorageOptions _options;
        private readonly string _filesPath;
        private readonly byte[] _encryptionKey;

        public DocumentStorageService(IOptions<DocumentStorageOptions> options, IWebHostEnvironment env)
        {
            _options = options.Value;
            _filesPath = Path.IsPathRooted(_options.UploadPath)
                ? _options.UploadPath
                : Path.Combine(env.ContentRootPath, _options.UploadPath);
            _encryptionKey = GetEncryptionKey(_options.EncryptionKey);

            Directory.CreateDirectory(_filesPath);
        }

        public int MaxFilesPerClaim => _options.MaxFilesPerClaim;
        public long MaxFileSizeBytes => _options.MaxFileSizeBytes;
        public IReadOnlyCollection<string> AllowedExtensions => _options.AllowedExtensions;

        // Validates and reads a submitted document before claim metadata is saved.
        public async Task<DocumentUploadResult> PrepareUploadAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return DocumentUploadResult.Failed("The uploaded document is empty.");

            if (file.Length > _options.MaxFileSizeBytes)
                return DocumentUploadResult.Failed($"'{file.FileName}' exceeds the maximum file size.");

            var fileName = CleanFileName(file.FileName);
            if (string.IsNullOrEmpty(fileName))
                return DocumentUploadResult.Failed("The uploaded document name is invalid.");

            var fileStem = Path.GetFileNameWithoutExtension(fileName).Trim('.', ' ');
            if (string.IsNullOrWhiteSpace(fileStem))
                return DocumentUploadResult.Failed("The uploaded document name is invalid.");

            var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!_options.AllowedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
                return DocumentUploadResult.Failed($"'{fileName}' is not an accepted document type.");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileData = memoryStream.ToArray();

            if (!HasValidFileSignature(fileExtension, fileData))
                return DocumentUploadResult.Failed($"'{fileName}' does not match the expected file format.");

            return DocumentUploadResult.Passed(fileName, fileExtension, file.Length, fileData);
        }

        // Saves an uploaded file to the file system with AES encryption.
        public async Task SaveDocumentFileAsync(int documentId, byte[] fileData)
        {
            var encryptedData = EncryptData(fileData);
            var filePath = GetDocumentPath(documentId);
            await File.WriteAllBytesAsync(filePath, encryptedData);
        }

        // Retrieves and decrypts a stored document file from the file system.
        public async Task<byte[]?> GetDocumentFileAsync(int documentId)
        {
            var filePath = GetDocumentPath(documentId);
            if (!File.Exists(filePath)) return null;

            var encryptedData = await File.ReadAllBytesAsync(filePath);
            return DecryptData(encryptedData);
        }

        private string GetDocumentPath(int documentId)
        {
            return Path.Combine(_filesPath, $"{documentId}.enc");
        }

        private static string CleanFileName(string fileName)
        {
            var safeName = Path.GetFileName(fileName);
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            return safeName;
        }

        private static byte[] GetEncryptionKey(string configuredKey)
        {
            if (string.IsNullOrWhiteSpace(configuredKey))
                throw new InvalidOperationException("Document storage encryption key is not configured.");

            byte[] keyBytes;
            try
            {
                keyBytes = Convert.FromBase64String(configuredKey);
            }
            catch (FormatException)
            {
                keyBytes = Encoding.UTF8.GetBytes(configuredKey);
            }

            if (keyBytes.Length != 16 && keyBytes.Length != 24 && keyBytes.Length != 32)
                throw new InvalidOperationException("Document storage encryption key must be 16, 24, or 32 bytes.");

            return keyBytes;
        }

        private static bool HasValidFileSignature(string extension, byte[] fileData)
        {
            return extension switch
            {
                ".pdf" => HasPdfSignature(fileData),
                ".docx" => HasOfficeDocumentSignature(fileData, "word/document.xml"),
                ".xlsx" => HasOfficeDocumentSignature(fileData, "xl/workbook.xml"),
                _ => false
            };
        }

        private static bool HasPdfSignature(byte[] fileData)
        {
            var pdfHeader = Encoding.ASCII.GetBytes("%PDF-");
            return fileData.Length >= pdfHeader.Length && fileData.Take(pdfHeader.Length).SequenceEqual(pdfHeader);
        }

        private static bool HasOfficeDocumentSignature(byte[] fileData, string requiredEntry)
        {
            try
            {
                using var stream = new MemoryStream(fileData);
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                return archive.GetEntry("[Content_Types].xml") != null && archive.GetEntry(requiredEntry) != null;
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        private byte[] EncryptData(byte[] data)
        {
            var nonce = RandomNumberGenerator.GetBytes(GcmNonceSize);
            var tag = new byte[GcmTagSize];
            var cipherText = new byte[data.Length];

            using var aesGcm = new AesGcm(_encryptionKey, GcmTagSize);
            aesGcm.Encrypt(nonce, data, cipherText, tag);

            using var ms = new MemoryStream();

            // Store a small header so new authenticated files can be separated from older files.
            ms.Write(EncryptedFileHeader);
            ms.Write(nonce);
            ms.Write(tag);
            ms.Write(cipherText);

            return ms.ToArray();
        }

        private byte[]? DecryptData(byte[] encryptedData)
        {
            try
            {
                if (HasCurrentEncryptionHeader(encryptedData))
                    return DecryptCurrentData(encryptedData);

                return DecryptLegacyData(encryptedData);
            }
            catch (CryptographicException)
            {
                return null;
            }
            catch (InvalidDataException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private byte[] DecryptCurrentData(byte[] encryptedData)
        {
            var headerLength = EncryptedFileHeader.Length;
            var minimumLength = headerLength + GcmNonceSize + GcmTagSize;
            if (encryptedData.Length < minimumLength)
                throw new InvalidDataException("Stored document data is incomplete.");

            var nonce = encryptedData.AsSpan(headerLength, GcmNonceSize);
            var tag = encryptedData.AsSpan(headerLength + GcmNonceSize, GcmTagSize);
            var cipherText = encryptedData.AsSpan(minimumLength);
            var fileData = new byte[cipherText.Length];

            using var aesGcm = new AesGcm(_encryptionKey, GcmTagSize);
            aesGcm.Decrypt(nonce, cipherText, tag, fileData);

            return fileData;
        }

        private byte[] DecryptLegacyData(byte[] encryptedData)
        {
            if (encryptedData.Length <= LegacyIvSize)
                throw new InvalidDataException("Stored document data is incomplete.");

            using var aes = Aes.Create();
            var iv = new byte[LegacyIvSize];

            Array.Copy(encryptedData, 0, iv, 0, iv.Length);
            aes.Key = _encryptionKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
            {
                cs.Write(encryptedData, iv.Length, encryptedData.Length - iv.Length);
            }

            return ms.ToArray();
        }

        private static bool HasCurrentEncryptionHeader(byte[] encryptedData)
        {
            return encryptedData.Length >= EncryptedFileHeader.Length &&
                   encryptedData.AsSpan(0, EncryptedFileHeader.Length).SequenceEqual(EncryptedFileHeader);
        }
    }
}
