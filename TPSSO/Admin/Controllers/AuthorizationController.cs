using System.Security.Claims;
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
            select new MyAuthorizationResult
            {
                Id = auth.Id!,
                ClientName = app.DisplayName ?? app.ClientId!,
                Scopes = auth.Scopes ?? "openid",
                CreatedAt = auth.CreationDate!.Value
            }
        ).ToListAsync();

        return new ResponseResult<List<MyAuthorizationResult>>(authorizations);
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
