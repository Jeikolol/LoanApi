namespace Domain.Constants
{
    public static class SystemRoles
    {
        public const string Admin = "ADMIN";
        public const string Manager = "MANAGER";
        public const string User = "USER";

        /// <summary>Roles allowed to manage system settings such as the document storage.</summary>
        public const string AdminOrManager = Admin + "," + Manager;
    }
}
