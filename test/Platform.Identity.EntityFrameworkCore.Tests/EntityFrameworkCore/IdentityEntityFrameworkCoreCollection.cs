using Xunit;

namespace Platform.Identity.EntityFrameworkCore;

[CollectionDefinition(IdentityTestConsts.CollectionDefinitionName)]
public class IdentityEntityFrameworkCoreCollection : ICollectionFixture<IdentityEntityFrameworkCoreFixture>
{

}
