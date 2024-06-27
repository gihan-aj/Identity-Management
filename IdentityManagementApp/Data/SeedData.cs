
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace IdentityManagementApp.Data
{
    public static class SeedData
    {
        // Roles
        public const string AdminRole = "Admin";
        public const string ManagerRole = "Manager";
        public const string SupervisorRole = "Supervisor";
        public const string MemberRole = "Member";

        public const string AdminUserName = "admin@example.com";
        public const string SuperAdminChangeNotAllowed = "Super Admin change is not allowed!";
        public const int MaximumLoginAttempts = 3;

        public static bool VIPPolicy(AuthorizationHandlerContext ctx)
        {
            if(ctx.User.IsInRole(MemberRole) 
                && ctx.User.HasClaim(c => c.Type == ClaimTypes.Email 
                && c.Value.Contains("vip")))
            {
                return true;
            }

            return false;
        }
    }
}
