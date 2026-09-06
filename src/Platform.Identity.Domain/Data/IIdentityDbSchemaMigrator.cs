using System.Threading.Tasks;

namespace Platform.Identity.Data;

public interface IIdentityDbSchemaMigrator
{
    Task MigrateAsync();
}
