using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence
{
    public class LoanDbContext : DbContext
    {
        public LoanDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Loan> Loans => Set<Loan>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(LoanDbContext).Assembly
            );
        }
    }
}
