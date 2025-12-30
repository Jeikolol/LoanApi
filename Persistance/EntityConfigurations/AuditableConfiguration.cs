using Domain;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public abstract class AuditableConfiguration<TEntity, T> : IEntityTypeConfiguration<TEntity>
        where TEntity : AuditableEntity<T>
    {
        private readonly string? _schema;
        private readonly string _tableName;

        protected AuditableConfiguration(string? schema = "dbo", string? tableName = null)
        {
            _schema = schema;
            _tableName = string.IsNullOrEmpty(tableName) ? typeof(TEntity).Name.Pluralize() : tableName;
        }

        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            _ = _schema ?? throw new ArgumentNullException($"schema is null for entity [{typeof(TEntity).Name}]");

            builder
                .ToTable(_tableName, _schema);

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CreatedOn)
                  .IsRequired();

            builder.Property(x => x.CreatedById)
                   .IsRequired(false);

            builder.Property(x => x.UpdatedOn)
                   .IsRequired(false);

            builder.Property(x => x.UpdatedById)
                   .IsRequired(false);

            builder.Property(x => x.DeletedOn)
                   .IsRequired(false);

            builder.Property(x => x.DeletedById)
                   .IsRequired(false);

            builder.Property(x => x.IsDeleted)
                   .HasDefaultValue(false)
                   .IsRequired();

            builder.HasIndex(x => x.IsDeleted);
            builder.HasIndex(x => x.CreatedOn);

            builder.HasQueryFilter(x => !x.IsDeleted);
            
            builder.HasOne(x => x.CreatedBy)
                   .WithMany()
                   .HasForeignKey(x => x.CreatedById)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.UpdatedBy)
                   .WithMany()
                   .HasForeignKey(x => x.UpdatedById)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.DeletedBy)
                   .WithMany()
                   .HasForeignKey(x => x.DeletedById)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
