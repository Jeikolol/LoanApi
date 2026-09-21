namespace LoanApi.Controllers.Modules.Loans.Requests
{
    public class UploadDocumentForm
    {
        public IFormFile? File { get; set; }
        public string? DocumentType { get; set; }
    }
}
