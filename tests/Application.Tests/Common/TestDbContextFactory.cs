using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests.Common;

internal sealed class TestDbContext : DbContext, IApplicationDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasMany(o => o.Items).WithOne().HasForeignKey("OrderId");

            e.Ignore(o => o.Total);
            e.Ignore(o => o.DomainEvents);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.OwnsOne(i => i.UnitPrice, m =>
            {
                m.Property(p => p.Amount);
                m.Property(p => p.Currency);
            });
            e.Ignore(i => i.LineTotal);
            e.Ignore(i => i.DomainEvents);
        });
    }
}

internal static class TestDbContextFactory
{
    public static TestDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }
}
