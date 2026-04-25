namespace LoanApi.Controllers.Modules.Loans.Requests
{
    public class UploadDocumentRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
    }
}
