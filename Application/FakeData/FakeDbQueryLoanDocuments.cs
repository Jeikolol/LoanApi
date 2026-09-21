using Bogus;
using Domain.Entities;
using Domain.Enums;

namespace Application.FakeData
{
    public static class FakeDbQueryLoanDocuments
    {
        /// <summary>Spanish file-name stem and extension for each kind of document.</summary>
        private static readonly Dictionary<DocumentTypes, (string Stem, string Extension, MimeTypes Mime)> Catalog = new()
        {
            [DocumentTypes.Contract] = ("contrato-prestamo", "pdf", MimeTypes.Pdf),
            [DocumentTypes.PromissoryNote] = ("pagare", "pdf", MimeTypes.Pdf),
            [DocumentTypes.IDCopy] = ("copia-cedula", "jpg", MimeTypes.Jpeg),
            [DocumentTypes.IncomeCertificate] = ("carta-de-ingresos", "pdf", MimeTypes.Pdf),
            [DocumentTypes.BankStatement] = ("estado-de-cuenta", "pdf", MimeTypes.Pdf),
            [DocumentTypes.PropertyDeed] = ("titulo-de-propiedad", "pdf", MimeTypes.Pdf),
        };

        // Every loan has its contract and promissory note; the rest are the supporting papers.
        private static readonly DocumentTypes[] Supporting =
        {
            DocumentTypes.IDCopy, DocumentTypes.IncomeCertificate, DocumentTypes.BankStatement, DocumentTypes.PropertyDeed,
        };

        public static IEnumerable<LoanDocument> GenerateLoanDocuments(IEnumerable<Loan> loans)
        {
            var random = new Randomizer(444444);
            var faker = new Faker { Random = random };
            var documents = new List<LoanDocument>();

            foreach (var loan in loans)
            {
                var types = new List<DocumentTypes> { DocumentTypes.Contract, DocumentTypes.PromissoryNote };
                types.AddRange(faker.PickRandom(Supporting, random.Int(1, 2)));

                // Documents are filed around the disbursement date, never in the future.
                var disbursed = loan.DisbursementDate.ToDateTime(TimeOnly.MinValue);
                var uploaded = disbursed > DateTime.UtcNow ? DateTime.UtcNow : disbursed;

                foreach (var type in types)
                {
                    var (stem, extension, mime) = Catalog[type];
                    var uploadedDate = uploaded.AddHours(random.Int(8, 17));
                    if (uploadedDate > DateTime.UtcNow) uploadedDate = DateTime.UtcNow;
                    var fileName = $"{stem}-{loan.LoanNumber}.{extension}";

                    documents.Add(new LoanDocument
                    {
                        Id = Guid.NewGuid(),
                        LoanId = loan.Id,
                        DocumentType = type,
                        FileName = fileName,
                        FileUrl = $"https://storage.example.com/prestamos/{loan.Id}/{fileName}",
                        UploadedDate = uploadedDate,
                        FileSizeBytes = random.Long(120_000, 2_500_000),
                        MimeType = mime,
                        CreatedOn = uploadedDate,
                        IsActive = true,
                        IsDeleted = false,
                    });
                }
            }

            return documents;
        }
    }
}
