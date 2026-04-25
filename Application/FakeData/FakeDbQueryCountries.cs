using Domain.Entities;
using System;
using System.Collections.Generic;

namespace Application.FakeData
{
    public static class FakeDbQueryCountries
    {
        public static readonly IEnumerable<Country> Countries;

        static FakeDbQueryCountries()
        {
            var countries = new List<Country>
            {
                new Country
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Code = "DO",
                    CodeIso2 = "DO",
                    Name = "República Dominicana",
                    Nationality = "Dominicana/o"
                },
                new Country
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Code = "US",
                    CodeIso2 = "US",
                    Name = "United States",
                    Nationality = "American"
                },
                new Country
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Code = "ES",
                    CodeIso2 = "ES",
                    Name = "España",
                    Nationality = "Española/o"
                },
                new Country
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Code = "MX",
                    CodeIso2 = "MX",
                    Name = "México",
                    Nationality = "Mexicana/o"
                },
                new Country
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Code = "CO",
                    CodeIso2 = "CO",
                    Name = "Colombia",
                    Nationality = "Colombiana/o"
                },
                new Country
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Code = "PR",
                    CodeIso2 = "PR",
                    Name = "Puerto Rico",
                    Nationality = "Puertorriqueña/o"
                }
            };

            Countries = countries;
        }
    }
}
