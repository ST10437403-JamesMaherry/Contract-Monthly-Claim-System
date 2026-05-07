namespace Contract_Monthly_Claim_System.Services
{
    public class DocumentStorageOptions
    {
        public string UploadPath { get; set; } = "App_Data/Uploads";
        public string EncryptionKey { get; set; } = string.Empty;
        public string[] AllowedExtensions { get; set; } = { ".pdf", ".docx", ".xlsx" };
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
        public int MaxFilesPerClaim { get; set; } = 5;
    }
}
