using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using Taipi.Core.RQRS;
using TPSSO.Application.Models;
using TPSSO.Infrastructure.Data;

namespace TPSSO.Admin.Controllers;

[ApiController]
[Route("api/client")]
public class ClientUserController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ClientUserController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取客户端的授权用户列表（仅客户端创建者或管理员可查看）
    /// </summary>
    [HttpPost("{id}/authorized-users")]
    [Authorize]
    public async Task<PagerResponseResult<AuthorizedUserResult>> GetAuthorizedUsers(
        Guid id, [FromBody] SearchPager<AuthorizedUserSearchCondition> pager)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");

        // 验证客户端存在且当前用户有权查看
        var client = await _context.ClientApplications.FindAsync(id);
        if (client == null)
            return new PagerResponseResult<AuthorizedUserResult>([], pager, 0);

        if (!isAdmin && client.CreatedByUserId != userId)
            return new PagerResponseResult<AuthorizedUserResult>([], pager, 0);

        // 从 OpenIddict Authorizations 表查询授权用户
        var openIddictAppId = client.OpenIddictApplicationId;
        if (string.IsNullOrEmpty(openIddictAppId))
            return new PagerResponseResult<AuthorizedUserResult>([], pager, 0);

        var query = from auth in _context.Set<OpenIddictEntityFrameworkCoreAuthorization>()
                    where auth.Application != null && auth.Application.Id == openIddictAppId
                          && auth.Status == OpenIddictConstants.Statuses.Valid
                          && auth.Type == OpenIddictConstants.AuthorizationTypes.Permanent
                    join user in _context.Users on auth.Subject equals user.Id.ToString()
                    select new { user, auth.CreationDate };

        // 关键字搜索
        if (!string.IsNullOrWhiteSpace(pager.Condition?.Keyword))
        {
            var keyword = pager.Condition.Keyword;
            query = query.Where(x => x.user.UserName.Contains(keyword) || x.user.NickName.Contains(keyword));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreationDate)
            .Skip((pager.PageIndex - 1) * pager.PageSize)
            .Take(pager.PageSize)
            .Select(x => new AuthorizedUserResult
            {
                Username = x.user.UserName,
                NickName = x.user.NickName,
                AuthorizedAt = x.CreationDate!.Value
            })
            .ToListAsync();

        return new PagerResponseResult<AuthorizedUserResult>(items, pager, totalCount);
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(OpenIddictConstants.Claims.Subject);
        return Guid.Parse(userIdStr!);
    }
}

/// <summary>
/// 授权用户搜索条件
/// </summary>
public class AuthorizedUserSearchCondition
{
    public string? Keyword { get; set; }
}

/// <summary>
/// 授权用户信息
/// </summary>
public class AuthorizedUserResult
{
    public string Username { get; set; } = string.Empty;
    public string? NickName { get; set; }
    public DateTimeOffset AuthorizedAt { get; set; }
}
