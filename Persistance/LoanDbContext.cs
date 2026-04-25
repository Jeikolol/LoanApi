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
        public DbSet<Country> Countries => Set<Country>();
        public DbSet<Currency> Currencies => Set<Currency>();
        public DbSet<IdentificationType> IdentificationTypes => Set<IdentificationType>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Loan> Loans => Set<Loan>();
        public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
        public DbSet<LoanDocument> LoanDocuments => Set<LoanDocument>();
        public DbSet<DelinquencyPolicy> DelinquencyPolicies => Set<DelinquencyPolicy>();
        public DbSet<CreditScore> CreditScores => Set<CreditScore>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(LoanDbContext).Assembly
            );
        }
    }
}
