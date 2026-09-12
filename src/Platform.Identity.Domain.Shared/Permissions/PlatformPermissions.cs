namespace Platform.Identity.Permissions;

public static class PlatformPermissions
{
    public const string GroupName = "Platform";

    public static class Admin
    {
        public const string Default = GroupName + ".Admin";
        public const string Access = Default + ".Access";
        public const string Dashboard = Default + ".Dashboard";
    }

    public static class Identity
    {
        public const string Default = GroupName + ".Identity";
        public const string UsersView = Default + ".Users.View";
        public const string UsersManage = Default + ".Users.Manage";
        public const string RolesManage = Default + ".Roles.Manage";
        public const string ClientsManage = Default + ".Clients.Manage";
    }
}

public static class DocForgePermissions
{
    public const string GroupName = "DocForge";

    public const string Convert = GroupName + ".Convert";
    public const string Transcribe = GroupName + ".Transcribe";
    public const string Translate = GroupName + ".Translate";
    public const string History = GroupName + ".History";
}