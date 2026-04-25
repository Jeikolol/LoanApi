using Bogus;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Application.FakeData
{
    public static class FakeDbQueryCustomers
    {
        public static readonly IEnumerable<Customer> Customers;

        static FakeDbQueryCustomers()
        {
            Randomizer.Seed = new Random(123456);
            var identificationTypeIds = new[] { SystemConstants.CedulaIdentificationTypeId, SystemConstants.PassportIdentificationTypeId };

            // Default to Dominican Republic
            var dominicanCountryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var countryIds = new[] { dominicanCountryId, Guid.Parse("22222222-2222-2222-2222-222222222222"), Guid.Parse("33333333-3333-3333-3333-333333333333") };

            var faker = new Faker<Customer>()
                .StrictMode(true)
                .RuleFor(x => x.Id, _ => Guid.NewGuid())
                .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                .RuleFor(x => x.LastName, f => f.Name.LastName())
                .RuleFor(x => x.CompanyName, f => f.Company.CompanyName())
                .RuleFor(x => x.Identification, f => f.Random.Replace("###-#######-#"))
                .RuleFor(x => x.Phone, (f, c) => $"829-{f.Random.Number(100, 999):D3}-{f.Random.Number(1000, 9999):D4}")
                .RuleFor(x => x.Email, f => f.Internet.Email())
                .RuleFor(x => x.Address, f => f.Address.StreetAddress())
                .RuleFor(x => x.Province, f => f.Address.State())
                .RuleFor(x => x.CountryId, _ => dominicanCountryId)
                .RuleFor(x => x.NationalityCountryId, f => f.PickRandom(countryIds))
                .RuleFor(x => x.Occupation, f => f.Name.JobTitle())
                .RuleFor(x => x.AnnualIncome, f => f.Random.Decimal(50_000m, 500_000m))
                .RuleFor(x => x.IncomeSource, f => f.PickRandom<IncomeSource>())
                .RuleFor(x => x.CreditLimit, f => f.Random.Decimal(100_000m, 1_000_000m))
                .RuleFor(x => x.RiskLevel, f => f.PickRandom<RiskLevel>())
                .RuleFor(x => x.AlternativePhone, (f, c) => $"849-{f.Random.Number(100, 999):D3}-{f.Random.Number(1000, 9999):D4}")
                .RuleFor(x => x.EmergencyContactName, f => f.Name.FullName())
                .RuleFor(x => x.EmergencyContactPhone, (f, c) => $"809-{f.Random.Number(100, 999):D3}-{f.Random.Number(1000, 9999):D4}")
                .RuleFor(x => x.TaxId, f => f.Random.Replace("###-#####-#"))
                .RuleFor(x => x.CustomerType, f => f.PickRandom<CustomerType>())
                .RuleFor(x => x.Status, f => f.PickRandom<CustomerStatus>())
                .RuleFor(x => x.IdentificationTypeId, f => f.PickRandom(identificationTypeIds))
                .RuleFor(x => x.IsBlacklisted, f => f.Random.Bool(0.05f))
                .RuleFor(x => x.BlacklistReason, (f, c) => c.IsBlacklisted ? f.Lorem.Sentence() : null)
                .RuleFor(x => x.Loans, _ => new List<Loan>())
                .RuleFor(x => x.CreditScore, _ => null)
                .RuleFor(x => x.IdentificationType, _ => null)
                .RuleFor(x => x.Country, _ => null)
                .RuleFor(x => x.NationalityCountry, _ => null)
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

            Customers = faker.Generate(150);
        }
    }
}
