using Microsoft.EntityFrameworkCore;
using SoftielRemote.Backend.Models;

namespace SoftielRemote.Backend.Data;

/// <summary>
/// PostgreSQL veritabanı için DbContext.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Agent'lar tablosu.
    /// </summary>
    public DbSet<AgentEntity> Agents { get; set; } = null!;

    /// <summary>
    /// Bekleyen bağlantı istekleri tablosu.
    /// </summary>
    public DbSet<ConnectionRequestEntity> ConnectionRequests { get; set; } = null!;

    /// <summary>
    /// Backend kayıt tablosu (farklı network'lerdeki Backend'lerin keşfi için).
    /// </summary>
    public DbSet<BackendRegistryEntity> BackendRegistry { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AgentEntity yapılandırması
        modelBuilder.Entity<AgentEntity>(entity =>
        {
            entity.ToTable("Agents");
            entity.HasKey(e => e.DeviceId);
            entity.Property(e => e.DeviceId)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(e => e.MachineName)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.OperatingSystem)
                .HasMaxLength(100);
            entity.Property(e => e.ConnectionId)
                .HasMaxLength(100);
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45); // IPv6 için yeterli
            entity.HasIndex(e => e.ConnectionId);
            entity.HasIndex(e => e.LastSeen);
        });

        // ConnectionRequestEntity yapılandırması
        modelBuilder.Entity<ConnectionRequestEntity>(entity =>
        {
            entity.ToTable("ConnectionRequests");
            entity.HasKey(e => e.ConnectionId);
            entity.Property(e => e.ConnectionId)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.TargetDeviceId)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(e => e.RequesterId)
                .HasMaxLength(50);
            entity.Property(e => e.RequesterName)
                .HasMaxLength(255);
            entity.Property(e => e.RequesterIp)
                .HasMaxLength(45);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsRequired();
            entity.HasIndex(e => e.TargetDeviceId);
            entity.HasIndex(e => e.RequestedAt);
        });

        // BackendRegistryEntity yapılandırması
        modelBuilder.Entity<BackendRegistryEntity>(entity =>
        {
            entity.ToTable("BackendRegistry");
            entity.HasKey(e => e.BackendId);
            entity.Property(e => e.BackendId)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.PublicUrl)
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(e => e.LocalIp)
                .HasMaxLength(45);
            entity.Property(e => e.Description)
                .HasMaxLength(255);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.LastSeen);
        });
    }
}

