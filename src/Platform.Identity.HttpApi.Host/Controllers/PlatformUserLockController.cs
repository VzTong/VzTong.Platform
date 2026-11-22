using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Identity;
using AbpUser = Volo.Abp.Identity.IdentityUser;

namespace Platform.Identity.Controllers;

/// <summary>
/// Khóa user: tạm (đến mốc thời gian) hoặc vĩnh viễn (IsActive=false).
/// URL cố định — tránh 405 của conventional AppService.
///
/// POST /api/platform/users/{id}/lock
/// POST /api/platform/users/{id}/unlock
/// </summary>
[Route("api/platform/users")]
[Authorize(IdentityPermissions.Users.Update)]
public class PlatformUserLockController : AbpController
{
    private readonly IdentityUserManager _userManager;

    public PlatformUserLockController(IdentityUserManager userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// mode = temporary → bắt buộc lockoutEnd (UTC) từ lịch UI.
    /// mode = permanent → IsActive = false.
    /// </summary>
    [HttpPost("{id:guid}/lock")]
    public virtual async Task LockAsync(Guid id, [FromBody] LockUserInput input)
    {
        Check.NotNull(input, nameof(input));

        var user = await _userManager.GetByIdAsync(id);
        EnsureNotAdmin(user);

        if (string.Equals(input.Mode, "permanent", StringComparison.OrdinalIgnoreCase))
        {
            user.SetIsActive(false);
            (await _userManager.SetLockoutEndDateAsync(user, null)).CheckErrors();
            (await _userManager.UpdateAsync(user)).CheckErrors();
            return;
        }

        // temporary
        if (input.LockoutEnd is null)
        {
            throw new BusinessException("Identity:LockoutEndRequired");
        }

        var end = input.LockoutEnd.Value;
        if (end.ToUniversalTime() <= DateTimeOffset.UtcNow)
        {
            throw new BusinessException("Identity:LockoutEndMustBeFuture");
        }

        // Tạm: vẫn active, chỉ chặn login đến LockoutEnd
        user.SetIsActive(true);
        (await _userManager.SetLockoutEnabledAsync(user, true)).CheckErrors();
        (await _userManager.SetLockoutEndDateAsync(user, end.ToUniversalTime())).CheckErrors();
        (await _userManager.UpdateAsync(user)).CheckErrors();
    }

    [HttpPost("{id:guid}/unlock")]
    public virtual async Task UnlockAsync(Guid id)
    {
        var user = await _userManager.GetByIdAsync(id);

        user.SetIsActive(true);
        (await _userManager.SetLockoutEndDateAsync(user, null)).CheckErrors();
        (await _userManager.UpdateAsync(user)).CheckErrors();
    }

    private static void EnsureNotAdmin(AbpUser user)
    {
        if (string.Equals(user.UserName, "admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("Identity:CannotLockAdmin");
        }
    }
}

/// <summary>Body JSON cho POST .../lock</summary>
public class LockUserInput
{
    /// <summary>"temporary" | "permanent"</summary>
    public string Mode { get; set; } = "temporary";

    /// <summary>Bắt buộc khi mode=temporary. Nên gửi ISO UTC, ví dụ 2026-12-01T17:00:00.000Z</summary>
    public DateTimeOffset? LockoutEnd { get; set; }
}