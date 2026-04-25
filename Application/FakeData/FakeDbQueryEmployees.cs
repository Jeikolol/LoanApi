using Bogus;
using Domain.Constants;
using Domain.Entities;
using System;
using System.Collections.Generic;

namespace Application.FakeData
{
    public static class FakeDbQueryEmployees
    {
        public static readonly IEnumerable<Employee> Employees;

        static FakeDbQueryEmployees()
        {
            Randomizer.Seed = new Random(789456);

            var faker = new Faker<Employee>()
                .StrictMode(true)
                .RuleFor(x => x.Id, _ => Guid.NewGuid())
                .RuleFor(x => x.UserName, f => f.Internet.UserName())
                .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                .RuleFor(x => x.LastName, f => f.Name.LastName())
                .RuleFor(x => x.Email, f => f.Internet.Email())
                .RuleFor(x => x.RoleId, _ => SystemConstants.DefaultRoleId)
                .RuleFor(x => x.BranchId, _ => SystemConstants.DefaultBranchId)
                .RuleFor(x => x.PasswordHash, f => HashPassword(f.Internet.Password(12)))
                .RuleFor(x => x.PasswordSalt, _ => Guid.NewGuid().ToString())
                .RuleFor(x => x.LastLoginOn, f => f.Date.RecentOffset().DateTime);

            Employees = faker.Generate(50);
        }

        private static string HashPassword(string password)
        {
            // Using ASP.NET Core PasswordHasher for simplicity
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Employee>();
            return hasher.HashPassword(new Employee(), password);
        }
    }
}
