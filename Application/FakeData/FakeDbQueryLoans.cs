using Bogus;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Application.FakeData
{
    public static class FakeDbQueryLoans
    {
        public static IEnumerable<Loan> GenerateLoans(IEnumerable<Customer> customers, IEnumerable<Branch> branches)
        {
            Randomizer.Seed = new Random(654321);

            var loans = new List<Loan>();

            var faker = new Faker<Loan>()
                .StrictMode(true)
                .RuleFor(x => x.Id, _ => Guid.NewGuid())
                .RuleFor(x => x.LoanNumber, f => f.Random.Replace("LN-#########"))
                .RuleFor(x => x.PrincipalAmount, f => f.Random.Decimal(10_000, 500_000))
                .RuleFor(x => x.InterestRate, f => f.Random.Decimal(5m, 18m))
                .RuleFor(x => x.TermMonths, f => f.Random.Int(6, 60))
                .RuleFor(x => x.InstallmentAmount, f => f.Random.Decimal(500, 5_000))
                .RuleFor(x => x.LoanStatus, f => f.PickRandom<LoanStatus>())
                .RuleFor(x => x.DisbursementDate, f => DateOnly.FromDateTime(f.Date.Past(2)))
                .RuleFor(x => x.MaturityDate, f => DateOnly.FromDateTime(f.Date.Future(5)))
                .RuleFor(x => x.NextPaymentDueDate, f => DateOnly.FromDateTime(f.Date.Future(1)))
                .RuleFor(x => x.TotalInterestAmount, f => f.Random.Decimal(1_000, 50_000))
                .RuleFor(x => x.PaidInterest, f => f.Random.Decimal(0, 20_000))
                .RuleFor(x => x.AmortizationBalance, f => f.Random.Decimal(0, 300_000))
                .RuleFor(x => x.PrincipalRemaining, f => f.Random.Decimal(0, 300_000))
                .RuleFor(x => x.LoanPurpose, f => f.PickRandom<LoanPurpose>())
                .RuleFor(x => x.MaxCredit, f => f.Random.Decimal(100_000, 1_000_000))
                .RuleFor(x => x.RequiresGuarantor, f => f.Random.Bool(0.3f))
                .RuleFor(x => x.CustomerId, f => f.PickRandom(customers).Id)
                .RuleFor(x => x.BranchId, f => f.PickRandom(branches).Id)
                .RuleFor(x => x.CurrencyId, f => f.Random.Bool(0.9f) ? new Guid("77777777-7777-7777-7777-777777777777") : new Guid("88888888-8888-8888-8888-888888888888"))
                .RuleFor(x => x.RemainingInterest, f => f.Random.Decimal(0, 50_000))
                .RuleFor(x => x.PrincipalPaid, f => f.Random.Decimal(0, 300_000))
                .RuleFor(x => x.DaysOverdue, _ => 0)
                .RuleFor(x => x.DelinquencyPercentage, _ => 0m)
                .RuleFor(x => x.LastPaymentDate, _ => (DateTime?)null)
                .RuleFor(x => x.GuarantorId, _ => (Guid?)null)
                .RuleFor(x => x.Payments, _ => new List<LoanPayment>())
                .RuleFor(x => x.Documents, _ => new List<LoanDocument>())
                .RuleFor(x => x.Customer, _ => null)
                .RuleFor(x => x.Branch, _ => null)
                .RuleFor(x => x.Currency, _ => null)
                .RuleFor(x => x.CreatedOn, _ => DateTime.UtcNow)
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

            var generatedLoans = faker.Generate(500);
            return generatedLoans;
        }
    }
}
