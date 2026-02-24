using System.Security.Claims;

namespace CeramicERP.Security
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool HasPermission(this ClaimsPrincipal? user, string permission)
        {
            return user?.HasClaim(CustomClaimTypes.Permission, permission) ?? false;
        }

        public static string GetRoleName(this ClaimsPrincipal? user)
        {
            return user?.FindFirstValue(ClaimTypes.Role) ?? "User";
        }
    }
}
