using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Persistence;
using System.Data;

namespace Infrastructure.DataAccess
{
    public class ApplicationDbSeeder
    {
        //En caso de querer insertar muchos datos; usar Dapper para mejor performance
        private readonly IDbConnection _connection;

        public ApplicationDbSeeder(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task SeedDatabaseAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            await SeedRoleAsync(dbContext, cancellationToken);
            await SeedBranchAsync(dbContext, cancellationToken);
            await SeedUserAsync(dbContext, cancellationToken);
        }

        public async Task SeedRoleAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            if (!dbContext.Roles.Any())
            {
                var roles = new List<Role>
                {
                    new Role
                    {
                        Id = SystemConstants.DefaultRoleId,
                        Code = "ADMIN",
                        Name = "Administrator",
                        Description = "System Administrator with full access",
                    },
                    new Role
                    {
                        Code = "USER",
                        Name = "Standard User",
                        Description = "Standard user with limited access",
                    }
                };

                await dbContext.Roles.AddRangeAsync(roles);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task SeedBranchAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            if (!dbContext.Branches.Any())
            {
                var branches = new List<Branch>
                {
                    new Branch
                    {
                        Id = SystemConstants.DefaultBranchId,
                        Code = "01",
                        Name = "Main Branch",
                        Address = "123 Main St, Cityville",
                    },
                    new Branch
                    {

                        Code = "02",
                        Name = "Secondary Branch",
                        Address = "456 Side St, Townsville",
                    }
                };

                await dbContext.Branches.AddRangeAsync(branches);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task SeedUserAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            if (!dbContext.Employees.Any())
            {
                var hasher = new PasswordHasher<Employee>();

                var employee = new Employee
                {
                    Id = SystemConstants.DefaultAdminId,
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@admin.com",
                    UserName = "admin",
                    RoleId = SystemConstants.DefaultRoleId,
                    BranchId = SystemConstants.DefaultBranchId,
                    CreatedOn = DateTime.UtcNow,
                    
                };

                employee.PasswordHash = hasher.HashPassword(employee, SystemConstants.DefaultAdminPassword);

                await dbContext.Employees.AddAsync(employee);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
