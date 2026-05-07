namespace Contract_Monthly_Claim_System.Services
{
    public class DocumentDownloadResult
    {
        public bool Found { get; set; }
        public bool Authorized { get; set; }
        public byte[]? FileData { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";

        public static DocumentDownloadResult Missing()
        {
            return new DocumentDownloadResult { Found = false, Authorized = false };
        }

        public static DocumentDownloadResult Denied()
        {
            return new DocumentDownloadResult { Found = true, Authorized = false };
        }

        public static DocumentDownloadResult Ready(byte[] fileData, string fileName, string contentType)
        {
            return new DocumentDownloadResult
            {
                Found = true,
                Authorized = true,
                FileData = fileData,
                FileName = fileName,
                ContentType = contentType
            };
        }
    }
}
