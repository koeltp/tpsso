using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Taipi.Core.Linq;
using Taipi.Core.RQRS;
using Taipi.Core.Exceptions;
using TPSSO.Application.Exceptions;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Infrastructure.Services;

/// <summary>
/// 客户端管理服务实现
/// </summary>
public class ClientService : IClientService
{
    private readonly ApplicationDbContext _context;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ILogger<ClientService> _logger;

    public ClientService(
        ApplicationDbContext context,
        IOpenIddictApplicationManager applicationManager,
        ILogger<ClientService> logger)
    {
        _context = context;
        _applicationManager = applicationManager;
        _logger = logger;
    }

    public async Task<ClientCreatedResult> CreateAsync(CreateClientModel model, Guid userId)
    {
        var clientId = $"client_{Guid.NewGuid():N}"[..20];

        var client = new ClientApplication
        {
            ClientId = clientId,
            Name = model.Name,
            Description = model.Description,
            Logo = model.Logo,
            IsPublic = model.IsPublic,
            Status = ClientStatus.Draft,
            CreatedByUserId = userId,
            RedirectUris = ParseRedirectUris(model.RedirectUris),
            AllowedScopes = ParseScopes(model.AllowedScopes)
        };

        _context.ClientApplications.Add(client);
        await _context.SaveChangesAsync();

        return ToCreatedResult(client);
    }

    public async Task SubmitAsync(Guid id, Guid userId)
    {
        var client = await FindClientAsync(id);
        if (client == null)
            throw new AppException(AppCodes.ClientNotFound, "客户端不存在");
        if (client.CreatedByUserId != userId)
            throw new ForbiddenException(AppCodes.ClientNoPermission, "无权操作此客户端");
        if (client.Status != ClientStatus.Draft)
            throw new BadRequestException(AppCodes.ClientInvalidStatus, "仅草稿状态可提交审核");

        var invalidUris = client.RedirectUris.Where(u => !Uri.TryCreate(u.Uri, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https")).ToList();
        if (invalidUris.Any())
            throw new BadRequestException(AppCodes.ClientInvalidRedirectUri, $"回调地址格式错误：{string.Join("、", invalidUris.Select(u => u.Uri))}");

        client.Status = ClientStatus.Pending;
        client.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task WithdrawAsync(Guid id, Guid userId)
    {
        var client = await FindClientAsync(id);
        if (client == null)
            throw new AppException(AppCodes.ClientNotFound, "客户端不存在");
        if (client.CreatedByUserId != userId)
            throw new ForbiddenException(AppCodes.ClientNoPermission, "无权操作此客户端");
        if (client.Status == ClientStatus.Draft)
            throw new BadRequestException(AppCodes.ClientInvalidStatus, "已是草稿状态");

        client.Status = ClientStatus.Draft;
        client.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ApproveAsync(Guid id, Guid reviewerId)
    {
        var client = await FindClientAsync(id);
        if (client == null)
            throw new AppException(AppCodes.ClientNotFound, "客户端不存在");
        if (client.Status != ClientStatus.Pending)
            throw new BadRequestException(AppCodes.ClientInvalidStatus, "仅待审核状态可审批");

        var invalidUris = client.RedirectUris.Where(u => !Uri.TryCreate(u.Uri, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https")).ToList();
        if (invalidUris.Any())
            throw new BadRequestException(AppCodes.ClientInvalidRedirectUri, $"回调地址格式错误：{string.Join("、", invalidUris.Select(u => u.Uri))}");

        var redirectUris = client.RedirectUris.Select(u => new Uri(u.Uri)).ToList();
        var postLogoutUris = redirectUris.Select(u => new Uri(u.GetLeftPart(System.UriPartial.Authority))).Distinct().ToList();

        var permissions = new List<string>
        {
            Permissions.Endpoints.Authorization,
            Permissions.Endpoints.Token,
            Permissions.Endpoints.EndSession,
            Permissions.ResponseTypes.Code,
            Permissions.GrantTypes.AuthorizationCode
        };

        foreach (var scope in client.AllowedScopes)
        {
            if (scope.Scope == "profile") permissions.Add(Permissions.Scopes.Profile);
            else if (scope.Scope == "email") permissions.Add(Permissions.Scopes.Email);
            else if (scope.Scope == "roles") permissions.Add(Permissions.Scopes.Roles);
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = client.Name,
            ClientSecret = client.IsPublic ? null : GenerateSecret()
        };

        foreach (var uri in redirectUris)
            descriptor.RedirectUris.Add(uri);
        foreach (var uri in postLogoutUris)
            descriptor.PostLogoutRedirectUris.Add(uri);
        foreach (var perm in permissions)
            descriptor.Permissions.Add(perm);

        if (!client.IsPublic)
        {
            var newSecret = GenerateSecret();
            descriptor.ClientSecret = newSecret;
            client.ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(newSecret);
        }

        var openIddictApp = await _applicationManager.CreateAsync(descriptor);
        client.OpenIddictApplicationId = await _applicationManager.GetIdAsync(openIddictApp);
        client.Status = ClientStatus.Approved;
        client.ReviewedByUserId = reviewerId;
        client.ReviewedAt = DateTime.UtcNow;
        client.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task RejectAsync(Guid id, Guid reviewerId, string reason)
    {
        var client = await FindClientAsync(id);
        if (client == null)
            throw new AppException(AppCodes.ClientNotFound, "客户端不存在");
        if (client.Status != ClientStatus.Pending)
            throw new BadRequestException(AppCodes.ClientInvalidStatus, "仅待审核状态可审批");

        client.Status = ClientStatus.Rejected;
        client.ReviewedByUserId = reviewerId;
        client.ReviewedAt = DateTime.UtcNow;
        client.ReviewRemark = reason;
        client.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Guid id, Guid userId, UpdateClientModel model)
    {
        var client = await FindClientAsync(id);
        if (client == null)
            throw new AppException(AppCodes.ClientNotFound, "客户端不存在");
        if (client.CreatedByUserId != userId)
            throw new ForbiddenException(AppCodes.ClientNoPermission, "无权修改此客户端");
        if (client.Status != ClientStatus.Draft)
            throw new BadRequestException(AppCodes.ClientInvalidStatus, "仅草稿状态可编辑，请先撤回");

        if (!string.IsNullOrEmpty(model.RowVersion))
        {
            client.RowVersion = Convert.FromBase64String(model.RowVersion);
        }

        client.Name = model.Name;
        client.Description = model.Description;
        client.Logo = model.Logo;
        client.UpdatedAt = DateTime.UtcNow;

        var oldUris = await _context.ClientRedirectUris.Where(u => u.ClientApplicationId == id).ToListAsync();
        _context.ClientRedirectUris.RemoveRange(oldUris);
        _context.ClientRedirectUris.AddRange(ParseRedirectUris(model.RedirectUris).Select(u =>
        {
            u.ClientApplicationId = client.Id;
            return u;
        }));

        var oldScopes = await _context.ClientScopes.Where(s => s.ClientApplicationId == id).ToListAsync();
        _context.ClientScopes.RemoveRange(oldScopes);
        _context.ClientScopes.AddRange(ParseScopes(model.AllowedScopes).Select(s =>
        {
            s.ClientApplicationId = client.Id;
            return s;
        }));

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BadRequestException(AppCodes.ConcurrencyConflict, "数据已被其他操作修改，请刷新后重试");
        }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var client = await FindClientAsync(id);
        if (client == null)
            throw new AppException(AppCodes.ClientNotFound, "客户端不存在");

        if (client.ClientId == SystemClientIds.AdminClient)
            throw new BadRequestException(AppCodes.ClientBuiltIn, "系统内置客户端，禁止删除");

        if (client.CreatedByUserId != userId)
            throw new ForbiddenException(AppCodes.ClientNoPermission, "无权删除此客户端");

        if (!string.IsNullOrEmpty(client.OpenIddictApplicationId))
        {
            var openIddictApp = await _applicationManager.FindByIdAsync(client.OpenIddictApplicationId);
            if (openIddictApp != null)
                await _applicationManager.DeleteAsync(openIddictApp);
        }

        _context.ClientRedirectUris.RemoveRange(client.RedirectUris);
        _context.ClientScopes.RemoveRange(client.AllowedScopes);
        _context.ClientApplications.Remove(client);
        await _context.SaveChangesAsync();
    }

    public async Task<PagerResponseResult<ClientResult>> SearchAsync(SearchPager<ClientSearchCondition> pager, Guid userId, bool isAdmin)
    {
        var query = _context.ClientApplications
            .Include(c => c.RedirectUris)
            .Include(c => c.AllowedScopes)
            .AsQueryable();

        query = query.WhereIf(!isAdmin, c => c.CreatedByUserId == userId);

        query = query.WhereIf(
            pager.Condition?.Status.HasValue == true,
            c => c.Status == pager.Condition!.Status!.Value);

        var keyword = pager.Condition?.Keyword?.Trim();
        query = query.WhereIf(
            !string.IsNullOrEmpty(keyword),
            c => c.Name.Contains(keyword!) || c.ClientId.Contains(keyword!));

        query = query.OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Page(pager).ToListAsync();

        return new PagerResponseResult<ClientResult>(items.Select(ToResult), pager, totalCount);
    }

    public async Task<ClientResult> GetByIdAsync(Guid id)
    {
        var client = await FindClientAsync(id);
        if (client == null)
            throw new AppException(AppCodes.ClientNotFound, "客户端不存在");

        return ToResult(client);
    }

    public async Task<ClientCreatedResult> RegenerateSecretAsync(Guid id, Guid userId)
    {
        var client = await FindClientAsync(id);
        if (client == null)
            throw new AppException(AppCodes.ClientNotFound, "客户端不存在");

        if (client.IsPublic)
            throw new BadRequestException(AppCodes.ClientNoSecret, "公开客户端没有密钥");

        if (client.Status != ClientStatus.Approved)
            throw new BadRequestException(AppCodes.ClientInvalidStatus, "仅审核通过的客户端可操作密钥");

        var plainSecret = GenerateSecret();
        client.ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(plainSecret);
        client.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(client.OpenIddictApplicationId))
        {
            var openIddictApp = await _applicationManager.FindByIdAsync(client.OpenIddictApplicationId);
            if (openIddictApp != null)
            {
                await _applicationManager.UpdateAsync(openIddictApp, plainSecret);
            }
        }

        await _context.SaveChangesAsync();

        var result = ToCreatedResult(client);
        result.PlainSecret = plainSecret;
        return result;
    }

    // ──────── 私有方法 ────────

    private async Task<ClientApplication?> FindClientAsync(Guid id)
    {
        return await _context.ClientApplications
            .Include(c => c.RedirectUris)
            .Include(c => c.AllowedScopes)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    private static List<ClientRedirectUri> ParseRedirectUris(string raw)
    {
        return raw.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim())
            .Where(u => u.Length > 0)
            .Select(u => new ClientRedirectUri { Uri = u })
            .ToList();
    }

    private static List<ClientScope> ParseScopes(string raw)
    {
        return raw.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Select(s => new ClientScope { Scope = s })
            .ToList();
    }

    private static string GenerateSecret()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');
    }

    private static ClientResult ToResult(ClientApplication client)
    {
        return new ClientResult
        {
            Id = client.Id,
            ClientId = client.ClientId,
            Name = client.Name,
            Description = client.Description,
            Logo = client.Logo,
            RedirectUris = string.Join("\n", client.RedirectUris.Select(u => u.Uri)),
            AllowedScopes = string.Join(" ", client.AllowedScopes.Select(s => s.Scope)),
            IsPublic = client.IsPublic,
            Status = client.Status.ToString(),
            ReviewRemark = client.ReviewRemark,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt,
            RowVersion = client.RowVersion != null ? Convert.ToBase64String(client.RowVersion) : null
        };
    }

    private static ClientCreatedResult ToCreatedResult(ClientApplication client)
    {
        var baseResult = ToResult(client);
        return new ClientCreatedResult
        {
            Id = baseResult.Id,
            ClientId = baseResult.ClientId,
            Name = baseResult.Name,
            Description = baseResult.Description,
            Logo = baseResult.Logo,
            RedirectUris = baseResult.RedirectUris,
            AllowedScopes = baseResult.AllowedScopes,
            IsPublic = baseResult.IsPublic,
            Status = baseResult.Status,
            ReviewRemark = baseResult.ReviewRemark,
            CreatedAt = baseResult.CreatedAt,
            UpdatedAt = baseResult.UpdatedAt,
            RowVersion = baseResult.RowVersion
        };
    }
}
