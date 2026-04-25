using Bogus;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Application.FakeData
{
    public static class FakeDbQueryCreditScores
    {
        public static IEnumerable<CreditScore> GenerateCreditScores(IEnumerable<Customer> customers)
        {
            Randomizer.Seed = new Random(222222);

            var creditScores = new List<CreditScore>();

            var faker = new Faker<CreditScore>()
                .StrictMode(true)
                .RuleFor(x => x.Id, _ => Guid.NewGuid())
                .RuleFor(x => x.Score, f => f.Random.Int(300, 1000))
                .RuleFor(x => x.LastCalculatedDate, f => f.Date.Recent())
                .RuleFor(x => x.PaymentHistoryScore, f => f.Random.Decimal(200, 1000))
                .RuleFor(x => x.CreditUtilizationScore, f => f.Random.Decimal(200, 1000))
                .RuleFor(x => x.CreditAgeScore, f => f.Random.Decimal(100, 900))
                .RuleFor(x => x.DefaultHistoryScore, f => f.Random.Decimal(0, 1000))
                .RuleFor(x => x.CreditRating, f => f.PickRandom<CreditRating>())
                .RuleFor(x => x.Customer, _ => null)
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

            foreach (var customer in customers)
            {
                var score = faker.Clone().RuleFor(x => x.CustomerId, _ => customer.Id).Generate();
                creditScores.Add(score);
            }

            return creditScores;
        }
    }
}
