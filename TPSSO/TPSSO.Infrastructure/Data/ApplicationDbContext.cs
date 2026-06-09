using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using TPSSO.Domain.Entities;

namespace TPSSO.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>
{
    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();
    public DbSet<ClientApplication> ClientApplications => Set<ClientApplication>();
    public DbSet<ClientRedirectUri> ClientRedirectUris => Set<ClientRedirectUri>();
    public DbSet<ClientScope> ClientScopes => Set<ClientScope>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 让 OpenIddict 构建实体模型
        builder.UseOpenIddict();

        // 自定义 Identity 表名（去掉默认的 AspNet 前缀）
        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.UserName).HasMaxLength(100);
            entity.Property(u => u.NormalizedUserName).HasMaxLength(100);
            entity.Property(u => u.Email).HasMaxLength(100);
            entity.Property(u => u.NormalizedEmail).HasMaxLength(200);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            entity.Property(u => u.AvatarUrl).HasMaxLength(500);
            entity.Property(u => u.NickName).HasMaxLength(50);
        });
        builder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(r => r.Description).HasMaxLength(200);
        });
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

        // 客户端应用表配置
        builder.Entity<ClientApplication>(entity =>
        {
            entity.ToTable("ClientApplications");
            entity.Property(e => e.ClientId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ClientSecretHash).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Logo).HasMaxLength(500);
            entity.Property(e => e.OpenIddictApplicationId).HasMaxLength(100);
            entity.Property(e => e.ReviewRemark).HasMaxLength(500);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.ClientId).IsUnique();
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.Status);
        });

        builder.Entity<ClientRedirectUri>(entity =>
        {
            entity.ToTable("ClientRedirectUris");
            entity.Property(e => e.Uri).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.ClientApplicationId);
        });

        builder.Entity<ClientScope>(entity =>
        {
            entity.ToTable("ClientScopes");
            entity.Property(e => e.Scope).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.ClientApplicationId);
        });
    }
}
