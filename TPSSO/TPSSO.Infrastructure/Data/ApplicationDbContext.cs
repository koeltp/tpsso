using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using TPSSO.Domain.Entities;

namespace TPSSO.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 让 OpenIddict 构建实体模型
        builder.UseOpenIddict();

        // 自定义 Identity 表名（去掉默认的 AspNet 前缀）
        builder.Entity<IdentityUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.UserName).HasMaxLength(100);
            entity.Property(u => u.NormalizedUserName).HasMaxLength(100);
            entity.Property(u => u.Email).HasMaxLength(100);
            entity.Property(u => u.NormalizedEmail).HasMaxLength(200);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
        });
        builder.Entity<IdentityRole<Guid>>(entity => entity.ToTable("Roles"));
        builder.Entity<IdentityUserRole<Guid>>(entity => entity.ToTable("UserRoles"));
        builder.Entity<IdentityUserClaim<Guid>>(entity => entity.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<Guid>>(entity => entity.ToTable("UserLogins"));
        builder.Entity<IdentityRoleClaim<Guid>>(entity => entity.ToTable("RoleClaims"));
        builder.Entity<IdentityUserToken<Guid>>(entity => entity.ToTable("UserTokens"));

        // 自定义 OpenIddict 表名
        builder.Entity<OpenIddictEntityFrameworkCoreApplication>(entity =>
        {
            entity.ToTable("Applications");
            entity.Property(e => e.ClientId).HasMaxLength(100);
        });
        builder.Entity<OpenIddictEntityFrameworkCoreAuthorization>(entity =>
        {
            entity.ToTable("Authorizations");
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
        });
        builder.Entity<OpenIddictEntityFrameworkCoreScope>(entity =>
        {
            entity.ToTable("Scopes");
            entity.Property(e => e.Name).HasMaxLength(200);
        });
        builder.Entity<OpenIddictEntityFrameworkCoreToken>(entity =>
        {
            entity.ToTable("Tokens");
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(100);
        });

        // 验证码表配置
        builder.Entity<VerificationCode>(entity =>
        {
            entity.ToTable("VerificationCodes");
            entity.Property(e => e.Id).HasColumnName("VerificationCodeId");
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(10).IsRequired();
            entity.HasIndex(e => new { e.Email, e.Purpose, e.IsUsed });
        });
    }
}
