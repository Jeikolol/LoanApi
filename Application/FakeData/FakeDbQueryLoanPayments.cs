using Bogus;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Application.FakeData
{
    public static class FakeDbQueryLoanPayments
    {
        public static IEnumerable<LoanPayment> GenerateLoanPayments(IEnumerable<Loan> loans)
        {
            Randomizer.Seed = new Random(333333);

            var loanPayments = new List<LoanPayment>();

            var faker = new Faker<LoanPayment>()
                .StrictMode(true)
                .RuleFor(x => x.Id, _ => Guid.NewGuid())
                .RuleFor(x => x.PaymentDate, f => DateOnly.FromDateTime(f.Date.Recent(30)))
                .RuleFor(x => x.PrincipalAmount, f => f.Random.Decimal(100, 5000))
                .RuleFor(x => x.InterestAmount, f => f.Random.Decimal(10, 1000))
                .RuleFor(x => x.PenaltyAmount, f => f.Random.Decimal(0, 500))
                .RuleFor(x => x.PaymentMethod, f => f.PickRandom<PaymentMethod>())
                .RuleFor(x => x.ReferenceNumber, f => f.Random.Replace("REF-#########"))
                .RuleFor(x => x.Status, f => f.PickRandom<PaymentStatus>())
                .RuleFor(x => x.CreatedOn, f => f.Date.Recent())
                .RuleFor(x => x.Loan, _ => null)
                .RuleFor(x => x.Currency, _ => null)
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
                var paymentCount = Randomizer.Seed!.Next(2, 6);

                for (int i = 0; i < paymentCount; i++)
                {
                    var payment = faker.Clone()
                        .RuleFor(x => x.LoanId, _ => loan.Id)
                        .RuleFor(x => x.CurrencyId, _ => loan.CurrencyId)
                        .RuleFor(x => x.TotalPaymentAmount, (f, p) => p.PrincipalAmount + p.InterestAmount + p.PenaltyAmount)
                        .Generate();

                    loanPayments.Add(payment);
                }
            }

            return loanPayments;
        }
    }
}
