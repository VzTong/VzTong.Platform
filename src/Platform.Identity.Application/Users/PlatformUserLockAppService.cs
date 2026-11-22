using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Volo.Abp;
using Volo.Abp.Identity;

namespace Platform.Identity.Users;

/// <summary>
/// API khóa user linh hoạt (ABP Community — không dùng /lock Pro).
///
/// - Khóa tạm: LockoutEnd = UtcNow + hours → hết hạn Identity tự cho login lại.
/// - Khóa vĩnh viễn: IsActive = false → chỉ admin mở lại.
/// - Mở khóa: IsActive = true + xóa LockoutEnd.
///
/// Route conventional (Swagger /api/app/...):
///   POST .../platform-user-lock/lock-temporary?userId={guid}&amp;hours=24
///   POST .../platform-user-lock/lock-permanent?userId={guid}
///   POST .../platform-user-lock/unlock?userId={guid}
/// </summary>
[Authorize(IdentityPermissions.Users.Update)]
public class PlatformUserLockAppService : IdentityAppService
{
    private readonly IdentityUserManager _userManager;

    public PlatformUserLockAppService(IdentityUserManager userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Khóa tạm theo số giờ (UTC). hours &lt;= 0 → chỉ clear lockout thời gian.
    /// </summary>
    public virtual async Task LockTemporaryAsync(Guid userId, int hours)
    {
        var user = await GetNonAdminUserAsync(userId);

        (await _userManager.SetLockoutEnabledAsync(user, true)).CheckErrors();

        if (hours <= 0)
        {
            (await _userManager.SetLockoutEndDateAsync(user, null)).CheckErrors();
            return;
        }

        var until = DateTimeOffset.UtcNow.AddHours(hours);
        (await _userManager.SetLockoutEndDateAsync(user, until)).CheckErrors();
    }

    /// <summary>
    /// Khóa vĩnh viễn: tắt IsActive + clear LockoutEnd (tránh lẫn trạng thái).
    /// </summary>
    public virtual async Task LockPermanentAsync(Guid userId)
    {
        var user = await GetNonAdminUserAsync(userId);

        user.SetIsActive(false);
        (await _userManager.SetLockoutEndDateAsync(user, null)).CheckErrors();
        (await _userManager.UpdateAsync(user)).CheckErrors();
    }

    /// <summary>
    /// Mở hết: bật IsActive + bỏ LockoutEnd.
    /// </summary>
    public virtual async Task UnlockAsync(Guid userId)
    {
        var user = await _userManager.GetByIdAsync(userId);

        user.SetIsActive(true);
        (await _userManager.SetLockoutEndDateAsync(user, null)).CheckErrors();
        (await _userManager.UpdateAsync(user)).CheckErrors();
    }

    /// <summary>Không cho khóa tài khoản admin hệ thống.</summary>
    private async Task<IdentityUser> GetNonAdminUserAsync(Guid userId)
    {
        var user = await _userManager.GetByIdAsync(userId);

        if (string.Equals(user.UserName, "admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("Identity:CannotLockAdmin")
                .WithData("UserName", user.UserName);
        }

        return user;
    }
}