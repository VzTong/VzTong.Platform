using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Platform.Identity.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace Platform.Identity.Data;

public class PlatformRoleDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IdentityRoleManager _roleManager;
    private readonly IPermissionDataSeeder _permissionDataSeeder;
    private readonly IGuidGenerator _guidGenerator;

    public PlatformRoleDataSeedContributor(
        IIdentityRoleRepository roleRepository,
        IdentityRoleManager roleManager,
        IPermissionDataSeeder permissionDataSeeder,
        IGuidGenerator guidGenerator)
    {
        _roleRepository = roleRepository;
        _roleManager = roleManager;
        _permissionDataSeeder = permissionDataSeeder;
        _guidGenerator = guidGenerator;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await EnsureRoleAsync("User");

        // Gán thêm (idempotent) cho role admin có sẵn
        // admin: toàn bộ Platform + DocForge
        await _permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            "admin",
            new[]
            {
                PlatformPermissions.Admin.Access,
                PlatformPermissions.Admin.Dashboard,
                PlatformPermissions.Identity.UsersView,
                PlatformPermissions.Identity.UsersManage,
                PlatformPermissions.Identity.RolesManage,
                PlatformPermissions.Identity.ClientsManage,
                DocForgePermissions.Convert,
                DocForgePermissions.Transcribe,
                DocForgePermissions.Translate,
                DocForgePermissions.History,
            },
            context?.TenantId
        );

        // User: DocForge cơ bản
        await _permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            "User",
            new[]
            {
                DocForgePermissions.Convert,
                DocForgePermissions.Transcribe,
                DocForgePermissions.Translate,
                DocForgePermissions.History,
            },
            context?.TenantId
        );
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        var role = await _roleRepository.FindByNormalizedNameAsync(roleName.ToUpperInvariant());
        if (role == null)
        {
            role = new IdentityRole(_guidGenerator.Create(), roleName)
            {
                IsPublic = true
            };
            (await _roleManager.CreateAsync(role)).CheckErrors();
        }
    }
}