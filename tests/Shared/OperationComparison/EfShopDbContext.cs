using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FunnySharp.TestSupport.OperationComparison;

internal sealed class ShopDbContext(SqliteConnection connection) : DbContext
{
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();

    public DbSet<CatalogEntity> Catalog => Set<CatalogEntity>();

    public DbSet<InventoryEntity> Inventory => Set<InventoryEntity>();

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

    public DbSet<OutboxEntity> Outbox => Set<OutboxEntity>();

    public DbSet<ReviewEntity> Reviews => Set<ReviewEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlite(connection);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerEntity>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Id).HasColumnName("id");
            entity.Property(customer => customer.Name).HasColumnName("name");
            entity.Property(customer => customer.CreditLimitCents).HasColumnName("credit_limit_cents");
            entity.Property(customer => customer.Active).HasColumnName("active");
        });

        modelBuilder.Entity<CatalogEntity>(entity =>
        {
            entity.ToTable("catalog");
            entity.HasKey(item => item.Sku);
            entity.Property(item => item.Sku).HasColumnName("sku");
            entity.Property(item => item.UnitPriceCents).HasColumnName("unit_price_cents");
            entity.Property(item => item.Active).HasColumnName("active");
        });

        modelBuilder.Entity<InventoryEntity>(entity =>
        {
            entity.ToTable("inventory");
            entity.HasKey(item => item.Sku);
            entity.Property(item => item.Sku).HasColumnName("sku");
            entity.Property(item => item.Available).HasColumnName("available");
            entity.Property(item => item.Reserved).HasColumnName("reserved");
        });

        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(order => order.CustomerId).HasColumnName("customer_id");
            entity.Property(order => order.TotalCents).HasColumnName("total_cents");
            entity.Property(order => order.Status).HasColumnName("status");
            entity
                .HasMany(order => order.Lines)
                .WithOne()
                .HasForeignKey(line => line.OrderId)
                .IsRequired();
        });

        modelBuilder.Entity<OrderLineEntity>(entity =>
        {
            entity.ToTable("order_lines");
            entity.HasKey(line => line.Id);
            entity.Property(line => line.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(line => line.OrderId).HasColumnName("order_id");
            entity.Property(line => line.Sku).HasColumnName("sku");
            entity.Property(line => line.Quantity).HasColumnName("quantity");
            entity.Property(line => line.UnitPriceCents).HasColumnName("unit_price_cents");
        });

        modelBuilder.Entity<PaymentEntity>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(payment => payment.OrderId).HasColumnName("order_id");
            entity.Property(payment => payment.AmountCents).HasColumnName("amount_cents");
            entity.Property(payment => payment.Reference).HasColumnName("reference");
            entity.HasIndex(payment => payment.Reference).IsUnique();
        });

        modelBuilder.Entity<OutboxEntity>(entity =>
        {
            entity.ToTable("outbox");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(message => message.OrderId).HasColumnName("order_id");
            entity.Property(message => message.Topic).HasColumnName("topic");
            entity.Property(message => message.Payload).HasColumnName("payload");
        });

        modelBuilder.Entity<ReviewEntity>(entity =>
        {
            entity.ToTable("reviews");
            entity.HasKey(review => review.Id);
            entity.Property(review => review.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(review => review.OrderId).HasColumnName("order_id");
            entity.Property(review => review.Reason).HasColumnName("reason");
        });
    }
}

internal sealed class CustomerEntity
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int CreditLimitCents { get; set; }

    public bool Active { get; set; }
}

internal sealed class CatalogEntity
{
    public string Sku { get; set; } = string.Empty;

    public int UnitPriceCents { get; set; }

    public bool Active { get; set; }
}

internal sealed class InventoryEntity
{
    public string Sku { get; set; } = string.Empty;

    public int Available { get; set; }

    public int Reserved { get; set; }
}

internal sealed class OrderEntity
{
    public long Id { get; set; }

    public string CustomerId { get; set; } = string.Empty;

    public int TotalCents { get; set; }

    public string Status { get; set; } = string.Empty;

    public List<OrderLineEntity> Lines { get; set; } = [];
}

internal sealed class OrderLineEntity
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int UnitPriceCents { get; set; }
}

internal sealed class PaymentEntity
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public int AmountCents { get; set; }

    public string Reference { get; set; } = string.Empty;
}

internal sealed class OutboxEntity
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public string Topic { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;
}

internal sealed class ReviewEntity
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public string Reason { get; set; } = string.Empty;
}
