using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Volo.Abp.Account;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Users;

namespace Platform.Identity.Account;

[Dependency(ReplaceServices = true)]
[ExposeServices(
    typeof(IProfileAppService),
    typeof(ProfileAppService),
    typeof(PlatformProfileAppService))]
public class PlatformProfileAppService : ProfileAppService
{
    private readonly IOpenIddictTokenRepository _tokenRepository;

    public PlatformProfileAppService(
        IdentityUserManager userManager,
        IOptions<IdentityOptions> identityOptions,
        IOpenIddictTokenRepository tokenRepository)
        : base(userManager, identityOptions)
    {
        _tokenRepository = tokenRepository;
    }

    public override async Task ChangePasswordAsync(ChangePasswordInput input)
    {
        await base.ChangePasswordAsync(input);
        await RevokeAllTokensAsync(CurrentUser.GetId());
    }

    private async Task RevokeAllTokensAsync(Guid userId)
    {
        var tokens = await _tokenRepository.FindBySubjectAsync(userId.ToString());
        foreach (var token in tokens)
        {
            await _tokenRepository.DeleteAsync(token);
        }
    }
}