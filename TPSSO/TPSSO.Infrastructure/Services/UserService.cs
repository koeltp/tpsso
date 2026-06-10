using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taipi.Core.Linq;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;

namespace TPSSO.Infrastructure.Services;

/// <summary>
/// 用户管理服务实现（管理员）
/// </summary>
public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ApplicationDbContext context,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILogger<UserService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<PagerResponseResult<UserListResult>> SearchAsync(SearchPager<UserSearchCondition> pager)
    {
        var query = _context.Users.AsQueryable();

        var condition = pager.Condition;

        // 按关键字模糊搜索（用户名、邮箱、昵称）
        var keyword = condition?.Keyword?.Trim();
        query = query.WhereIf(
            !string.IsNullOrEmpty(keyword),
            u => u.UserName!.Contains(keyword!) || u.Email!.Contains(keyword!) || (u.NickName != null && u.NickName.Contains(keyword!)));

        // 按状态筛选
        query = query.WhereIf(
            condition?.Status == UserStatus.Locked,
            u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);

        query = query.WhereIf(
            condition?.Status == UserStatus.Active,
            u => u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow);

        // 按角色筛选（需要子查询）
        if (!string.IsNullOrEmpty(condition?.Role))
        {
            var role = condition.Role;
            query = query.Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)));
        }

        // 默认按创建时间降序
        query = query.OrderByDescending(u => u.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Page(pager).ToListAsync();

        // 批量获取角色，避免 N+1 查询
        var userIds = items.Select(u => u.Id).ToList();
        var userRoles = await (
            from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, RoleName = r.Name! }
        ).ToListAsync();

        var roleDict = userRoles.GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());

        var results = items.Select(u => new UserListResult
        {
            Id = u.Id,
            Username = u.UserName!,
            Email = u.Email!,
            NickName = u.NickName,
            AvatarUrl = u.AvatarUrl,
            Roles = roleDict.GetValueOrDefault(u.Id, []),
            IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow,
            CreatedAt = u.CreatedAt
        });

        return new PagerResponseResult<UserListResult>(results, pager, totalCount);
    }

    public async Task<ResponseResult<UserListResult>> GetByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return ResponseResult<UserListResult>.NotFound("用户不存在");

        var roles = await _userManager.GetRolesAsync(user);
        return new ResponseResult<UserListResult>(new UserListResult
        {
            Id = user.Id,
            Username = user.UserName!,
            Email = user.Email!,
            NickName = user.NickName,
            AvatarUrl = user.AvatarUrl,
            Roles = roles.ToList(),
            IsLockedOut = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow,
            CreatedAt = user.CreatedAt
        }) { Code = 200 };
    }

    public async Task<ResponseResult<List<RoleResult>>> GetRolesAsync()
    {
        var roles = await _roleManager.Roles
            .Select(r => new RoleResult { Name = r.Name!, Description = r.Description })
            .ToListAsync();

        return new ResponseResult<List<RoleResult>>(roles) { Code = 200 };
    }

    public async Task<ResponseResult<bool>> UpdateRolesAsync(Guid id, Guid operatorId, UpdateUserRolesModel model)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return ResponseResult<bool>.NotFound("用户不存在");

        // 不允许清空所有角色
        if (model.Roles.Count == 0)
            return ResponseResult<bool>.BadRequest("至少需要保留一个角色");

        // 验证角色是否存在
        foreach (var roleName in model.Roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                return ResponseResult<bool>.BadRequest($"角色 {roleName} 不存在");
        }

        // 不允许管理员移除自己的 Admin 角色（防止把自己锁出管理后台）
        if (id == operatorId && !model.Roles.Contains(RoleConstants.Admin))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains(RoleConstants.Admin))
                return ResponseResult<bool>.BadRequest("不能移除自己的管理员角色");
        }

        // 增量操作：计算差异，分别添加和移除
        var existingRoles = (await _userManager.GetRolesAsync(user)).ToList();
        var rolesToAdd = model.Roles.Except(existingRoles).ToList();
        var rolesToRemove = existingRoles.Except(model.Roles).ToList();

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
                return ResponseResult<bool>.BadRequest("移除角色失败");
        }

        if (rolesToAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
                return ResponseResult<bool>.BadRequest("添加角色失败");
        }

        return ResponseResult<bool>.Success("角色更新成功");
    }

    public async Task<ResponseResult<bool>> LockAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return ResponseResult<bool>.NotFound("用户不存在");

        // 锁定 100 年，相当于永久禁用
        var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        if (!result.Succeeded)
            return ResponseResult<bool>.BadRequest("禁用用户失败");

        return ResponseResult<bool>.Success("已禁用");
    }

    public async Task<ResponseResult<bool>> UnlockAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return ResponseResult<bool>.NotFound("用户不存在");

        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded)
            return ResponseResult<bool>.BadRequest("启用用户失败");

        // 重置失败计数
        await _userManager.ResetAccessFailedCountAsync(user);

        return ResponseResult<bool>.Success("已启用");
    }

    public async Task<ResponseResult<bool>> ResetPasswordAsync(Guid id, ResetPasswordModel model)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return ResponseResult<bool>.NotFound("用户不存在");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return ResponseResult<bool>.BadRequest($"重置密码失败：{errors}");
        }

        return ResponseResult<bool>.Success("密码已重置");
    }
}
