using Platform.Identity.Samples;
using Xunit;

namespace Platform.Identity.EntityFrameworkCore.Domains;

[Collection(IdentityTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<IdentityEntityFrameworkCoreTestModule>
{

}
