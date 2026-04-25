using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public class CustomerConfig : AuditableConfiguration<Customer, Guid>
    {
        public CustomerConfig() : base(SchemaNames.Customers, nameof(Customer))
        {
        }

        public override void Configure(EntityTypeBuilder<Customer> builder)
        {
            base.Configure(builder);

            // =========================
            // Basic info
            // =========================
            builder.Property(x => x.FirstName)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.LastName)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.CompanyName)
                   .HasMaxLength(200)
                   .IsRequired(false);

            // =========================
            // Identification
            // =========================
            builder.Property(x => x.Identification)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.HasIndex(x => x.Identification)
                   .IsUnique();

            builder.HasOne(x => x.IdentificationType)
                   .WithMany()
                   .HasForeignKey(x => x.IdentificationTypeId)
                   .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // Contact
            // =========================
            builder.Property(x => x.Email)
                   .HasMaxLength(100)
                   .IsRequired(false);

            builder.HasIndex(x => x.Email)
                   .IsUnique(false);

            builder.Property(x => x.Phone)
                   .HasMaxLength(15)
                   .IsRequired();

            builder.HasIndex(x => x.Phone)
                   .IsUnique();

            // =========================
            // Address
            // =========================
            builder.Property(x => x.Address)
                   .HasMaxLength(250)
                   .IsRequired();

            builder.Property(x => x.Province)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.HasOne(x => x.Country)
                   .WithMany(x => x.Customers)
                   .HasForeignKey(x => x.CountryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.NationalityCountry)
                   .WithMany(x => x.CustomersNationality)
                   .HasForeignKey(x => x.NationalityCountryId)
                   .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // Dominican compliance
            // =========================
            builder.Property(x => x.Occupation)
                   .HasMaxLength(100)
                   .IsRequired(false);

            builder.Property(x => x.AnnualIncome)
                   .HasPrecision(18, 2)
                   .IsRequired(false);

            builder.Property(x => x.IncomeSource)
                   .IsRequired(false);

            builder.Property(x => x.CreditLimit)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.RiskLevel)
                   .IsRequired(false);

            builder.Property(x => x.BlacklistReason)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(x => x.IsBlacklisted)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(x => x.AlternativePhone)
                   .HasMaxLength(15)
                   .IsRequired(false);

            builder.Property(x => x.EmergencyContactName)
                   .HasMaxLength(100)
                   .IsRequired(false);

            builder.Property(x => x.EmergencyContactPhone)
                   .HasMaxLength(15)
                   .IsRequired(false);

            builder.Property(x => x.TaxId)
                   .HasMaxLength(50)
                   .IsRequired(false);

            // =========================
            // Status & enums
            // =========================
            builder.Property(x => x.CustomerType)
                   .IsRequired();

            builder.Property(x => x.Status)
                   .IsRequired();

            // =========================
            // Relationships
            // =========================
            builder.HasMany(x => x.Loans)
                   .WithOne(x => x.Customer)
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreditScore)
                   .WithOne(x => x.Customer)
                   .HasForeignKey<CreditScore>(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
