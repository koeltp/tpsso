using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using Taipi.Core.RQRS;
using TPSSO.Infrastructure.Data;

namespace TPSSO.Admin.Controllers;

[ApiController]
[Route("api/authorization")]
public class AuthorizationController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuthorizationController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取当前用户的授权列表
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<ResponseResult<List<MyAuthorizationResult>>> GetMyAuthorizations()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(OpenIddictConstants.Claims.Subject);

        // 查询当前用户的有效授权
        var authorizations = await (
            from auth in _context.Set<OpenIddictEntityFrameworkCoreAuthorization>()
            where auth.Subject == userIdStr
                  && auth.Status == OpenIddictConstants.Statuses.Valid
                  && auth.Type == OpenIddictConstants.AuthorizationTypes.Permanent
            join app in _context.Set<OpenIddictEntityFrameworkCoreApplication>()
                on auth.Application.Id equals app.Id
            select new MyAuthorizationResult
            {
                Id = auth.Id,
                ClientName = app.DisplayName ?? app.ClientId!,
                Scopes = auth.Scopes ?? "openid",
                CreatedAt = auth.CreationDate!.Value
            }
        ).ToListAsync();

        return new ResponseResult<List<MyAuthorizationResult>>(authorizations) { Code = 200 };
    }

    /// <summary>
    /// 撤销授权
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ResponseResult<bool>> RevokeAuthorization(string id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(OpenIddictConstants.Claims.Subject);

        var auth = await _context.Set<OpenIddictEntityFrameworkCoreAuthorization>()
            .FirstOrDefaultAsync(a => a.Id == id && a.Subject == userIdStr);

        if (auth == null)
            return ResponseResult<bool>.NotFound("授权记录不存在");

        // 标记授权为无效
        auth.Status = OpenIddictConstants.Statuses.Revoked;

        // 同时撤销关联的 Token
        var tokens = await _context.Set<OpenIddictEntityFrameworkCoreToken>()
            .Where(t => t.Authorization != null && t.Authorization.Id == id
                && t.Status == OpenIddictConstants.Statuses.Valid)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Status = OpenIddictConstants.Statuses.Revoked;
        }

        await _context.SaveChangesAsync();
        return ResponseResult<bool>.Success("已撤销授权");
    }
}

/// <summary>
/// 我的授权记录
/// </summary>
public class MyAuthorizationResult
{
    public string Id { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
