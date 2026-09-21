using Bogus;
using Bogus.DataSets;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using System.Globalization;
using System.Text;

namespace Application.FakeData
{
    /// <summary>
    /// Demo customers with plausible Dominican profiles: mostly individuals with Spanish names, cédula numbers,
    /// modest incomes and credit limits that are a fraction of what they earn in a year.
    /// </summary>
    public static class FakeDbQueryCustomers
    {
        private const string DominicanRepublicIsoCode = "DO";

        private static readonly Country DominicanRepublic = FakeDbQueryCountries.Countries.Single(c => c.CodeIso2 == DominicanRepublicIsoCode);

        private static readonly IReadOnlyList<Country> ForeignCountries =
            FakeDbQueryCountries.Countries.Where(c => c.CodeIso2 != DominicanRepublicIsoCode).ToList();

        private static readonly string[] Provinces =
        [
            "Distrito Nacional", "Distrito Nacional", "Santo Domingo", "Santo Domingo", "Santo Domingo", "Santiago", "Santiago",
            "La Vega", "San Cristóbal", "Puerto Plata", "La Romana", "San Pedro de Macorís", "Duarte", "Espaillat", "La Altagracia",
        ];

        private static readonly string[] Sectors =
        [
            "Los Prados", "Naco", "Villa Mella", "Herrera", "Los Alcarrizos", "Bella Vista", "Ensanche Ozama", "Gazcue",
            "Mirador Norte", "Cristo Rey", "Villa Consuelo", "Arroyo Hondo", "Ensanche Quisqueya", "Los Ríos",
        ];

        private static readonly string[] Occupations =
        [
            "Contador/a", "Maestro/a", "Enfermero/a", "Comerciante", "Técnico en refrigeración", "Chofer", "Ingeniero/a civil",
            "Vendedor/a", "Administrador/a", "Cajero/a", "Electricista", "Abogado/a", "Agricultor/a", "Cocinero/a",
            "Secretario/a", "Mecánico/a", "Diseñador/a gráfico", "Policía", "Barbero/a", "Recepcionista",
        ];

        private static readonly string[] EmailDomains = ["gmail.com", "hotmail.com", "outlook.com", "yahoo.com"];
        private static readonly string[] PhonePrefixes = ["809", "829", "849"];

        /// <summary>A Dominican number in the stored form: +1, an area code and seven digits.</summary>
        private static string DominicanPhone(Random random) =>
            $"+1{PhonePrefixes[random.Next(PhonePrefixes.Length)]}{random.Next(200, 999)}{random.Next(1000, 9999)}";
        private static readonly string[] BlacklistReasons =
        [
            "Incumplimiento reiterado de pagos", "Documentación falsificada", "Reportado por fraude en otra entidad",
        ];

        public static IReadOnlyList<Customer> Generate(int count = 150, int seed = 123456)
        {
            var random = new Random(seed);
            Randomizer.Seed = new Random(seed);
            var faker = new Faker("es");

            var identifications = new HashSet<string>();
            var phones = new HashSet<string>();
            var customers = new List<Customer>(count);

            for (int i = 0; i < count; i++)
            {
                bool legal = random.NextDouble() < 0.06;
                bool foreign = !legal && random.NextDouble() < 0.06;
                bool blacklisted = random.NextDouble() < 0.02;

                Name.Gender gender = random.Next(2) == 0 ? Name.Gender.Male : Name.Gender.Female;
                string firstName = faker.Name.FirstName(gender);
                string lastName = $"{faker.Name.LastName()} {faker.Name.LastName()}";

                decimal annualIncome = legal ? RoundTo(900_000m + (decimal)random.NextDouble() * 2_600_000m, 10_000m) : IndividualIncome(random);
                decimal creditLimit = legal
                    ? Math.Max(100_000m, RoundTo(annualIncome * (0.15m + (decimal)random.NextDouble() * 0.15m), 25_000m))
                    : Math.Max(20_000m, RoundTo(annualIncome * (0.30m + (decimal)random.NextDouble() * 0.30m), 5_000m));

                string province = Provinces[random.Next(Provinces.Length)];
                bool capital = province is "Distrito Nacional" or "Santo Domingo";
                Country nationality = foreign ? ForeignCountries[random.Next(ForeignCountries.Count)] : DominicanRepublic;

                var customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    CustomerType = legal ? CustomerType.LegalPerson : CustomerType.PhysicalPerson,
                    FirstName = firstName,
                    LastName = lastName,
                    CompanyName = legal ? $"{faker.PickRandom("Comercial", "Inversiones", "Ferretería", "Servicios", "Distribuidora")} {lastName.Split(' ')[0]} SRL" : null,
                    IdentificationTypeId = legal
                        ? SystemConstants.RncIdentificationTypeId
                        : foreign ? SystemConstants.PassportIdentificationTypeId : SystemConstants.CedulaIdentificationTypeId,
                    Identification = UniqueValue(identifications, () => legal
                        ? $"{random.Next(101, 132):D3}-{random.Next(0, 99_999):D5}-{random.Next(0, 10)}"
                        : foreign
                            ? $"{nationality.CodeIso2}{random.Next(10_000_000, 99_999_999)}"
                            : $"{random.Next(1, 402):D3}-{random.Next(0, 9_999_999):D7}-{random.Next(0, 10)}"),
                    Phone = UniqueValue(phones, () => DominicanPhone(random)),
                    AlternativePhone = random.NextDouble() < 0.5 ? DominicanPhone(random) : null,
                    Email = random.NextDouble() < 0.8 ? $"{Slug(firstName)}.{Slug(lastName.Split(' ')[0])}{random.Next(1, 99)}@{EmailDomains[random.Next(EmailDomains.Length)]}" : null,
                    Address = $"Calle {faker.Name.LastName()} #{random.Next(1, 150)}, {(capital ? Sectors[random.Next(Sectors.Length)] : "Zona Centro")}",
                    Province = province,
                    CountryId = DominicanRepublic.Id,
                    NationalityCountryId = nationality.Id,
                    Status = blacklisted ? CustomerStatus.Blocked : PickStatus(random),
                    Occupation = legal ? "Representante legal" : Occupations[random.Next(Occupations.Length)],
                    AnnualIncome = annualIncome,
                    IncomeSource = legal ? IncomeSource.Business : PickIncomeSource(random),
                    CreditLimit = creditLimit,
                    RiskLevel = null,
                    IsBlacklisted = blacklisted,
                    BlacklistReason = blacklisted ? BlacklistReasons[random.Next(BlacklistReasons.Length)] : null,
                    EmergencyContactName = $"{faker.Name.FirstName()} {faker.Name.LastName()}",
                    EmergencyContactPhone = DominicanPhone(random),
                    TaxId = null,
                    CreatedOn = DateTime.UtcNow.AddDays(-random.Next(30, 1000)),
                    IsActive = true,
                    IsDeleted = false,
                };

                customers.Add(customer);
            }

            return customers;
        }

        /// <summary>Log-normal-like monthly wages between roughly RD$ 12,500 and RD$ 130,000 (median around RD$ 32,000).</summary>
        private static decimal IndividualIncome(Random random)
        {
            double gaussian = Math.Sqrt(-2.0 * Math.Log(1.0 - random.NextDouble())) * Math.Cos(2.0 * Math.PI * random.NextDouble());
            double monthly = Math.Exp(Math.Log(32_000) + 0.45 * gaussian);
            return RoundTo((decimal)Math.Clamp(monthly, 12_500, 130_000) * 12m, 6_000m);
        }

        private static CustomerStatus PickStatus(Random random)
        {
            double roll = random.NextDouble();
            if (roll < 0.94) return CustomerStatus.Active;
            if (roll < 0.98) return CustomerStatus.Inactive;
            return CustomerStatus.Suspended;
        }

        private static IncomeSource PickIncomeSource(Random random)
        {
            double roll = random.NextDouble();
            if (roll < 0.65) return IncomeSource.Employment;
            if (roll < 0.85) return IncomeSource.Business;
            if (roll < 0.91) return IncomeSource.Pension;
            if (roll < 0.94) return IncomeSource.Investment;
            return IncomeSource.Other;
        }

        private static decimal RoundTo(decimal value, decimal step) => Math.Round(value / step, 0) * step;

        private static string UniqueValue(HashSet<string> used, Func<string> create)
        {
            string value;
            do
            {
                value = create();
            }
            while (!used.Add(value));

            return value;
        }

        private static string Slug(string text)
        {
            string decomposed = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetter(c))
                {
                    builder.Append(char.ToLowerInvariant(c));
                }
            }

            return builder.ToString();
        }
    }
}
