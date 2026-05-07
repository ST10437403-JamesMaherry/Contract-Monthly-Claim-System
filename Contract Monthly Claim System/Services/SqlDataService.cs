using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Contract_Monthly_Claim_System.Services
{
    public interface IDataService
    {
        // Claim-related operations
        Task<List<Contract_Monthly_Claim_System.Models.Claim>> GetClaimsAsync();
        Task<Contract_Monthly_Claim_System.Models.Claim?> GetClaimAsync(int claimId);
        Task AddClaimAsync(Contract_Monthly_Claim_System.Models.Claim claim);
        Task UpdateClaimAsync(Contract_Monthly_Claim_System.Models.Claim claim);
        Task<List<ClaimReview>> GetClaimReviewsAsync();

        // Document metadata and file operations
        Task<List<Document>> GetDocumentsAsync();
        Task<Document?> GetDocumentAsync(int documentId);
        Task AddDocumentAsync(Document document);

        // User and claim status reference data
        Task<List<User>> GetUsersAsync();
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<List<ClaimStatus>> GetClaimStatusesAsync();
    }

    public class SqlDataService : IDataService
    {
        private readonly ApplicationDbContext _context;

        // Constructor that initializes the database context
        public SqlDataService(ApplicationDbContext context)
        {
            _context = context;
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
                .Include(c => c.ClaimReviews)
                    .ThenInclude(cr => cr.Reviewer)
                .ToListAsync();
        }

        // Retrieves a specific claim by ID with related data
        public async Task<Contract_Monthly_Claim_System.Models.Claim?> GetClaimAsync(int claimId)
        {
            return await _context.Claims
                .Include(c => c.User)
                .Include(c => c.Status)
                .Include(c => c.Documents)
                .Include(c => c.ClaimReviews)
                    .ThenInclude(cr => cr.Reviewer)
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

        // Retrieves review history for all claims
        public async Task<List<ClaimReview>> GetClaimReviewsAsync()
        {
            return await _context.ClaimReviews
                .Include(cr => cr.Claim)
                .Include(cr => cr.Reviewer)
                .OrderByDescending(cr => cr.reviewedAt)
                .ToListAsync();
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
        public async Task<Document?> GetDocumentAsync(int documentId)
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

        #region Reference Data

        // Retrieves all available claim statuses from the database
        public async Task<List<ClaimStatus>> GetClaimStatusesAsync()
        {
            return await _context.ClaimStatuses.ToListAsync();
        }

        #endregion
    }
}
