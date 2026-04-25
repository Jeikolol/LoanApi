using Bogus;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Application.FakeData
{
    public static class FakeDbQueryLoanDocuments
    {
        public static IEnumerable<LoanDocument> GenerateLoanDocuments(IEnumerable<Loan> loans)
        {
            Randomizer.Seed = new Random(444444);

            var loanDocuments = new List<LoanDocument>();

            var faker = new Faker<LoanDocument>()
                .StrictMode(true)
                .RuleFor(x => x.Id, _ => Guid.NewGuid())
                .RuleFor(x => x.DocumentType, f => f.PickRandom<DocumentTypes>())
                .RuleFor(x => x.FileName, f => f.System.FileName("pdf"))
                .RuleFor(x => x.FileUrl, f => $"https://storage.example.com/documents/{f.Random.Guid()}")
                .RuleFor(x => x.UploadedDate, f => f.Date.Recent())
                .RuleFor(x => x.FileSizeBytes, f => f.Random.Long(100_000, 5_000_000))
                .RuleFor(x => x.MimeType, f => f.PickRandom<MimeTypes>())
                .RuleFor(x => x.CreatedOn, f => f.Date.Recent())
                .RuleFor(x => x.Loan, _ => null)
                .RuleFor(x => x.CreatedById, _ => (Guid?)null)
                .RuleFor(x => x.CreatedBy, _ => null)
                .RuleFor(x => x.UpdatedOn, _ => (DateTime?)null)
                .RuleFor(x => x.UpdatedById, _ => (Guid?)null)
                .RuleFor(x => x.UpdatedBy, _ => null)
                .RuleFor(x => x.IsActive, _ => true)
                .RuleFor(x => x.IsDeleted, _ => false)
                .RuleFor(x => x.DeletedOn, _ => (DateTime?)null)
                .RuleFor(x => x.DeletedById, _ => (Guid?)null)
                .RuleFor(x => x.DeletedBy, _ => null);

            foreach (var loan in loans)
            {
                var documentCount = Randomizer.Seed!.Next(1, 5);

                for (int i = 0; i < documentCount; i++)
                {
                    var document = faker.Clone()
                        .RuleFor(x => x.LoanId, _ => loan.Id)
                        .Generate();

                    loanDocuments.Add(document);
                }
            }

            return loanDocuments;
        }
    }
}
