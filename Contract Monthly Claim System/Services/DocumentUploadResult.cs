namespace Contract_Monthly_Claim_System.Services
{
    public class DocumentUploadResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public byte[] FileData { get; set; } = Array.Empty<byte>();

        public static DocumentUploadResult Passed(string fileName, string fileExtension, long fileSize, byte[] fileData)
        {
            return new DocumentUploadResult
            {
                Success = true,
                FileName = fileName,
                FileExtension = fileExtension,
                FileSize = fileSize,
                FileData = fileData
            };
        }

        public static DocumentUploadResult Failed(string errorMessage)
        {
            return new DocumentUploadResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
