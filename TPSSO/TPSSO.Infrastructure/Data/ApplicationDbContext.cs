using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using TPSSO.Domain.Entities;

namespace TPSSO.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 让 OpenIddict 构建实体模型
        builder.UseOpenIddict();

        builder.Entity<IdentityUser>(o =>
        {
            o.Property(u => u.UserName).HasMaxLength(100);
            o.Property(u => u.NormalizedUserName).HasMaxLength(100);
            o.Property(u => u.Email).HasMaxLength(100);
            o.Property(u => u.NormalizedEmail).HasMaxLength(200);
            o.Property(u => u.PhoneNumber).HasMaxLength(20);
        });

        // 限制索引相关列的长度，避免 MySQL 索引键过长
        builder.Entity<OpenIddictEntityFrameworkCoreToken>(entity =>
        {
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(100);
        });

        builder.Entity<OpenIddictEntityFrameworkCoreAuthorization>(entity =>
        {
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
        });

        builder.Entity<OpenIddictEntityFrameworkCoreScope>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        builder.Entity<OpenIddictEntityFrameworkCoreApplication>(entity =>
        {
            entity.Property(e => e.ClientId).HasMaxLength(100);
        });

        // 验证码表配置
        builder.Entity<VerificationCode>(entity =>
        {
            entity.ToTable("VerificationCodes");
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(10).IsRequired();
            entity.HasIndex(e => new { e.Email, e.Purpose, e.IsUsed });
        });
    }
}
