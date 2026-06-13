using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using Taipi.Core.RQRS;
using Taipi.Core.Exceptions;
using TPSSO.Application.Exceptions;
using TPSSO.Application.Models;
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

        var authorizations = await (
            from auth in _context.Set<OpenIddictEntityFrameworkCoreAuthorization>()
            where auth.Subject == userIdStr
                  && auth.Status == OpenIddictConstants.Statuses.Valid
                  && auth.Type == OpenIddictConstants.AuthorizationTypes.Permanent
            join app in _context.Set<OpenIddictEntityFrameworkCoreApplication>()
                on auth.Application!.Id! equals app.Id!
            select new
            {
                auth.Id,
                ClientName = app.DisplayName ?? app.ClientId!,
                ScopesRaw = auth.Scopes ?? "",
                auth.CreationDate
            }
        ).ToListAsync();

        var results = authorizations.Select(a => new MyAuthorizationResult
        {
            Id = a.Id!,
            ClientName = a.ClientName,
            Scopes = ParseScopes(a.ScopesRaw),
            CreatedAt = a.CreationDate
        }).ToList();

        return new ResponseResult<List<MyAuthorizationResult>>(results);
    }

    /// <summary>
    /// 解析 OpenIddict 存储的 Scopes（JSON 数组格式如 ["openid","profile"]）
    /// </summary>
    private static string ParseScopes(string scopesRaw)
    {
        if (string.IsNullOrWhiteSpace(scopesRaw))
            return "openid";

        // OpenIddict 以 JSON 数组格式存储 Scopes
        if (scopesRaw.StartsWith('['))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<string>>(scopesRaw);
                return items != null ? string.Join(" ", items) : scopesRaw;
            }
            catch
            {
                return scopesRaw;
            }
        }

        return scopesRaw;
    }

    /// <summary>
    /// 撤销授权
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<StatusResponseResult> RevokeAuthorization(string id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(OpenIddictConstants.Claims.Subject);

        var auth = await _context.Set<OpenIddictEntityFrameworkCoreAuthorization>()
            .FirstOrDefaultAsync(a => a.Id == id && a.Subject == userIdStr);

        if (auth is null)
            throw new AppException(5001, "授权记录不存在");

        auth.Status = OpenIddictConstants.Statuses.Revoked;

        var tokens = await _context.Set<OpenIddictEntityFrameworkCoreToken>()
            .Where(t => t.Authorization != null && t.Authorization.Id == id
                && t.Status == OpenIddictConstants.Statuses.Valid)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Status = OpenIddictConstants.Statuses.Revoked;
        }

        await _context.SaveChangesAsync();
        return StatusResponseResult.Success("已撤销授权");
    }
}
