using Platform.Identity.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Platform.Identity.Permissions;

public class PlatformPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var platform = context.AddGroup(PlatformPermissions.GroupName, L("Permission:Platform"));

        var admin = platform.AddPermission(PlatformPermissions.Admin.Default, L("Permission:Platform.Admin"));
        admin.AddChild(PlatformPermissions.Admin.Access, L("Permission:Platform.Admin.Access"));
        admin.AddChild(PlatformPermissions.Admin.Dashboard, L("Permission:Platform.Admin.Dashboard"));

        var identity = platform.AddPermission(PlatformPermissions.Identity.Default, L("Permission:Platform.Identity"));
        identity.AddChild(PlatformPermissions.Identity.UsersView, L("Permission:Identity.Users.View"));
        identity.AddChild(PlatformPermissions.Identity.UsersManage, L("Permission:Identity.Users.Manage"));
        identity.AddChild(PlatformPermissions.Identity.RolesManage, L("Permission:Identity.Roles.Manage"));
        identity.AddChild(PlatformPermissions.Identity.ClientsManage, L("Permission:Identity.Clients.Manage"));

        var docForge = context.AddGroup(DocForgePermissions.GroupName, L("Permission:DocForge"));
        docForge.AddPermission(DocForgePermissions.Convert, L("Permission:DocForge.Convert"));
        docForge.AddPermission(DocForgePermissions.Transcribe, L("Permission:DocForge.Transcribe"));
        docForge.AddPermission(DocForgePermissions.Translate, L("Permission:DocForge.Translate"));
        docForge.AddPermission(DocForgePermissions.History, L("Permission:DocForge.History"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<IdentityResource>(name);
    }
}