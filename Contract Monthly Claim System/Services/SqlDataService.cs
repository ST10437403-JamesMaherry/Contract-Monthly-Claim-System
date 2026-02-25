using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Contract_Monthly_Claim_System.Services
{
    public interface IDataService
    {
        // Claim-related operations
        Task<List<Contract_Monthly_Claim_System.Models.Claim>> GetClaimsAsync();
        Task<Contract_Monthly_Claim_System.Models.Claim> GetClaimAsync(int claimId);
        Task AddClaimAsync(Contract_Monthly_Claim_System.Models.Claim claim);
        Task UpdateClaimAsync(Contract_Monthly_Claim_System.Models.Claim claim);

        // Document metadata and file operations
        Task<List<Document>> GetDocumentsAsync();
        Task<Document> GetDocumentAsync(int documentId);
        Task AddDocumentAsync(Document document);
        Task<byte[]> GetDocumentFileAsync(int documentId);
        Task SaveDocumentFileAsync(int documentId, byte[] fileData);

        // User and claim status reference data
        Task<List<User>> GetUsersAsync();
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<List<ClaimStatus>> GetClaimStatusesAsync();
    }

    public class SqlDataService : IDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _filesPath;
        private readonly byte[] _encryptionKey;

        // Constructor that initializes the database context and file storage paths
        public SqlDataService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;

            // Set up file storage path for encrypted documents
            _filesPath = Path.Combine(env.ContentRootPath, "App_Data", "Uploads");
            _encryptionKey = Encoding.UTF8.GetBytes("16bytekey123456!");

            // Ensure upload directory exists
            Directory.CreateDirectory(_filesPath);
        }

        #region User Operations
        // Returns list of all users in the database
        public async Task<List<User>> GetUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        // Adds a new user to the database
        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // Updates an existing user in the database
        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Claim Operations

        // Retrieves all claims with related data
        public async Task<List<Contract_Monthly_Claim_System.Models.Claim>> GetClaimsAsync()
        {
            return await _context.Claims
                .Include(c => c.User)          // Load related user data
                .Include(c => c.Status)        // Load related status data
                .Include(c => c.Documents)     // Load related documents
                .ToListAsync();
        }

        // Retrieves a specific claim by ID with related data
        public async Task<Contract_Monthly_Claim_System.Models.Claim> GetClaimAsync(int claimId)
        {
            return await _context.Claims
                .Include(c => c.User)
                .Include(c => c.Status)
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.claimId == claimId);
        }

        // Adds a new claim to the database
        public async Task AddClaimAsync(Contract_Monthly_Claim_System.Models.Claim claim)
        {
            // Calculate total amount before saving
            claim.totalAmount = claim.hoursWorked * claim.hourlyRate;
            claim.submissionDate = DateTime.Now;

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();
        }

        /// Updates an existing claim in the database
        public async Task UpdateClaimAsync(Contract_Monthly_Claim_System.Models.Claim claim)
        {
            // Recalculate total amount before updating
            claim.totalAmount = claim.hoursWorked * claim.hourlyRate;

            _context.Claims.Update(claim);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Document Metadata Operations

        // Retrieves all document records with related claim data
        public async Task<List<Document>> GetDocumentsAsync()
        {
            return await _context.Documents
                .Include(d => d.Claim)  // Load related claim data
                .ToListAsync();
        }

        // Retrieves a specific document by ID with related claim data
        public async Task<Document> GetDocumentAsync(int documentId)
        {
            return await _context.Documents
                .Include(d => d.Claim)
                .FirstOrDefaultAsync(d => d.documentId == documentId);
        }

        // Adds a new document record to the database
        public async Task AddDocumentAsync(Document document)
        {
            document.uploadDate = DateTime.Now;
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Encrypted File Storage

        // Saves an uploaded file to the file system with AES encryption
        public async Task SaveDocumentFileAsync(int documentId, byte[] fileData)
        {
            var encryptedData = EncryptData(fileData);
            var filePath = Path.Combine(_filesPath, $"{documentId}.enc");
            await File.WriteAllBytesAsync(filePath, encryptedData);
        }

        // Retrieves and decrypts a stored document file from the file system
        public async Task<byte[]> GetDocumentFileAsync(int documentId)
        {
            var filePath = Path.Combine(_filesPath, $"{documentId}.enc");
            if (!File.Exists(filePath)) return null;

            var encryptedData = await File.ReadAllBytesAsync(filePath);
            return DecryptData(encryptedData);
        }

        // Encrypts file data using AES encryption with a random IV
        // The IV is prepended to the encrypted data for decryption
        private byte[] EncryptData(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();

            // Prepend IV to the encrypted data
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
            }

            return ms.ToArray();
        }

        // Decrypts file data by extracting the IV and decrypting the content
        private byte[] DecryptData(byte[] encryptedData)
        {
            using var aes = Aes.Create();
            var iv = new byte[16];

            // Extract IV from the beginning of the encrypted data
            Array.Copy(encryptedData, 0, iv, 0, iv.Length);
            aes.Key = _encryptionKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
            {
                // Decrypt everything after the IV
                cs.Write(encryptedData, iv.Length, encryptedData.Length - iv.Length);
            }

            return ms.ToArray();
        }

        #endregion

        #region Reference Data

        // Retrieves all available claim statuses from the database
        public async Task<List<ClaimStatus>> GetClaimStatusesAsync()
        {
            return await _context.ClaimStatuses.ToListAsync();
        }

        #endregion
    }
}
