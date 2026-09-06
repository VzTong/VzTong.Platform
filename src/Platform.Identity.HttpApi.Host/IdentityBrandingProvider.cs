using Microsoft.Extensions.Localization;
using Platform.Identity.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace Platform.Identity;

[Dependency(ReplaceServices = true)]
public class IdentityBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<IdentityResource> _localizer;

    public IdentityBrandingProvider(IStringLocalizer<IdentityResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
