using Contract_Monthly_Claim_System.Models;

namespace Contract_Monthly_Claim_System.Services
{
    public interface IDocumentAccessService
    {
        Task<DocumentDownloadResult> GetAuthorizedDownloadAsync(int documentId, int currentUserId, string currentUserRole);
    }

    public class DocumentAccessService : IDocumentAccessService
    {
        private readonly IDataService _dataService;
        private readonly IDocumentStorageService _documentStorageService;

        public DocumentAccessService(IDataService dataService, IDocumentStorageService documentStorageService)
        {
            _dataService = dataService;
            _documentStorageService = documentStorageService;
        }

        public async Task<DocumentDownloadResult> GetAuthorizedDownloadAsync(int documentId, int currentUserId, string currentUserRole)
        {
            var document = await _dataService.GetDocumentAsync(documentId);
            if (document == null)
                return DocumentDownloadResult.Missing();

            if (!CanDownloadDocument(document, currentUserId, currentUserRole))
                return DocumentDownloadResult.Denied();

            var fileData = await _documentStorageService.GetDocumentFileAsync(documentId);
            if (fileData == null)
                return DocumentDownloadResult.Missing();

            return DocumentDownloadResult.Ready(fileData, document.fileName, GetContentType(document.fileType));
        }

        private static bool CanDownloadDocument(Document document, int currentUserId, string currentUserRole)
        {
            if (document.Claim == null)
                return false;

            return currentUserRole switch
            {
                nameof(UserRoleType.Lecturer) => document.Claim.userId == currentUserId,
                nameof(UserRoleType.Coordinator) => CanCoordinatorDownload(document.Claim.statusId),
                nameof(UserRoleType.Manager) => CanManagerDownload(document.Claim.statusId),
                _ => false
            };
        }

        private static bool CanCoordinatorDownload(int statusId)
        {
            return statusId == (int)ClaimStatusType.Submitted ||
                   statusId == (int)ClaimStatusType.ApprovedByCoordinator ||
                   statusId == (int)ClaimStatusType.RejectedByCoordinator;
        }

        private static bool CanManagerDownload(int statusId)
        {
            return statusId == (int)ClaimStatusType.ApprovedByCoordinator ||
                   statusId == (int)ClaimStatusType.RejectedByCoordinator ||
                   statusId == (int)ClaimStatusType.ApprovedByManager ||
                   statusId == (int)ClaimStatusType.RejectedByManager ||
                   statusId == (int)ClaimStatusType.Paid;
        }

        private static string GetContentType(string fileType)
        {
            return fileType.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}
