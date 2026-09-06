using Platform.Identity.Samples;
using Xunit;

namespace Platform.Identity.EntityFrameworkCore.Applications;

[Collection(IdentityTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<IdentityEntityFrameworkCoreTestModule>
{

}
