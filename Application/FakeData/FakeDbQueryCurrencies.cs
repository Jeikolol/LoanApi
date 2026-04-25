using Domain.Entities;
using System;
using System.Collections.Generic;

namespace Application.FakeData
{
    public static class FakeDbQueryCurrencies
    {
        public static IEnumerable<Currency> Currencies => new List<Currency>
        {
            new Currency
            {
                Id = new Guid("77777777-7777-7777-7777-777777777777"),
                CodeIso2 = "DO",
                CodeIso3 = "DOP",
                Name = "Dominican Peso",
                Symbol = "RD$",
                IsDefault = true,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = null
            },
            new Currency
            {
                Id = new Guid("88888888-8888-8888-8888-888888888888"),
                CodeIso2 = "US",
                CodeIso3 = "USD",
                Name = "US Dollar",
                Symbol = "$",
                IsDefault = false,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = null
            }
        };
    }
}
