using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Quantity).IsRequired();

        builder.OwnsOne(i => i.UnitPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("UnitPriceAmount")
                .IsRequired()
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("UnitPriceCurrency")
                .IsRequired()
                .HasMaxLength(3)
                .IsFixedLength();
        });

        builder.Ignore(i => i.LineTotal);
        builder.Ignore(i => i.DomainEvents);
    }
}
