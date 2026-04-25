using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public class CountryConfig : AuditableConfiguration<Country, Guid>
    {
        public CountryConfig() : base(SchemaNames.Reference, nameof(Country))
        {
        }

        public override void Configure(EntityTypeBuilder<Country> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Code)
                   .IsRequired(false)
                   .HasMaxLength(10);

            builder.Property(x => x.CodeIso2)
                   .IsRequired()
                   .HasMaxLength(2);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Nationality)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.HasMany(x => x.Customers)
                   .WithOne(x => x.Country)
                   .HasForeignKey(x => x.CountryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.CustomersNationality)
                   .WithOne(x => x.NationalityCountry)
                   .HasForeignKey(x => x.NationalityCountryId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
