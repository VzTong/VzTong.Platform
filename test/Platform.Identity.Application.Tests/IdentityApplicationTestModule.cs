using Volo.Abp.Modularity;

namespace Platform.Identity;

[DependsOn(
    typeof(IdentityApplicationModule),
    typeof(IdentityDomainTestModule)
)]
public class IdentityApplicationTestModule : AbpModule
{

}
