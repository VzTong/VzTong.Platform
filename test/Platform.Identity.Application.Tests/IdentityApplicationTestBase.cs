using Volo.Abp.Modularity;

namespace Platform.Identity;

public abstract class IdentityApplicationTestBase<TStartupModule> : IdentityTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
