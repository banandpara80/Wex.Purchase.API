using Microsoft.EntityFrameworkCore;
using Wex.Purchase.Repository.Entity;

namespace Wex.Purchase.Repository;

/// <summary>
/// Entity Framework Core database context for the purchase database.
/// Manages the database connection and entity mappings for purchase data.
/// </summary>
public class PurchaseDbContext : DbContext
{
    public PurchaseDbContext(DbContextOptions<PurchaseDbContext> options) : base(options)
    {
    }

    public DbSet<PurchaseBO> Purchases { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseBO>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Description).IsRequired();
            b.Property(e => e.PurchaseAmount).HasColumnType("numeric(18,2)").IsRequired();
            b.Property(e => e.TransactionDate).IsRequired();
        });
    }
}
