using Volo.Abp.Modularity;

namespace Platform.Identity;

[DependsOn(
    typeof(IdentityDomainModule),
    typeof(IdentityTestBaseModule)
)]
public class IdentityDomainTestModule : AbpModule
{

}
