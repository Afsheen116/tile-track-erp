using CeramicERP.Models;
using CeramicERP.Security;
using Microsoft.EntityFrameworkCore;

namespace CeramicERP.Data
{
    public static class RbacSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await SeedPermissionsAsync(context);
            await SeedRolesAsync(context);
            await SeedRolePermissionsAsync(context);
            await SeedDefaultUsersAsync(context);
        }

        private static async Task SeedPermissionsAsync(ApplicationDbContext context)
        {
            var permissions = await context.Permissions.ToListAsync();
            var hasChanges = false;

            foreach (var definition in PermissionNames.AllDefinitions)
            {
                var existing = permissions.FirstOrDefault(p =>
                    string.Equals(p.Name, definition.Name, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    permissions.Add(new Permission
                    {
                        Name = definition.Name,
                        Description = definition.Description
                    });
                    context.Permissions.Add(permissions[^1]);
                    hasChanges = true;
                    continue;
                }

                if (!string.Equals(existing.Description, definition.Description, StringComparison.Ordinal))
                {
                    existing.Description = definition.Description;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedRolesAsync(ApplicationDbContext context)
        {
            var roles = await context.Roles.ToListAsync();

            EnsureRole(context, roles, RoleNames.SuperAdmin, "Admin");
            EnsureRole(context, roles, RoleNames.AdminManager);
            EnsureRole(context, roles, RoleNames.SalesExecutive, "User");
            EnsureRole(context, roles, RoleNames.Accountant);
            EnsureRole(context, roles, RoleNames.InventoryStaff);

            if (context.ChangeTracker.HasChanges())
            {
                await context.SaveChangesAsync();
            }
        }

        private static Role EnsureRole(
            ApplicationDbContext context,
            List<Role> roles,
            string desiredName,
            params string[] legacyNames)
        {
            var role = roles.FirstOrDefault(r =>
                string.Equals(r.Name, desiredName, StringComparison.OrdinalIgnoreCase));
            if (role != null)
            {
                return role;
            }

            role = roles.FirstOrDefault(r =>
                legacyNames.Any(legacy =>
                    string.Equals(r.Name, legacy, StringComparison.OrdinalIgnoreCase)));
            if (role != null)
            {
                role.Name = desiredName;
                return role;
            }

            role = new Role { Name = desiredName };
            context.Roles.Add(role);
            roles.Add(role);
            return role;
        }

        private static async Task SeedRolePermissionsAsync(ApplicationDbContext context)
        {
            var roles = await context.Roles.ToListAsync();
            var permissions = await context.Permissions.ToListAsync();
            var rolePermissions = await context.RolePermissions.ToListAsync();

            var roleMap = roles.ToDictionary(r => r.Name, r => r, StringComparer.OrdinalIgnoreCase);
            var permissionMap = permissions.ToDictionary(
                p => p.Name,
                p => p,
                StringComparer.OrdinalIgnoreCase);

            var superAdminPermissions = PermissionNames.AllDefinitions
                .Select(p => p.Name)
                .ToArray();

            var matrix = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [RoleNames.SuperAdmin] = superAdminPermissions,
                [RoleNames.AdminManager] = new[]
                {
                    PermissionNames.ViewDashboard,
                    PermissionNames.ViewDashboardFinancial,
                    PermissionNames.ViewSales,
                    PermissionNames.CreateSale,
                    PermissionNames.EditSale,
                    PermissionNames.DeleteSale,
                    PermissionNames.ViewPurchases,
                    PermissionNames.CreatePurchase,
                    PermissionNames.EditPurchase,
                    PermissionNames.DeletePurchase,
                    PermissionNames.ViewInventory,
                    PermissionNames.ManageInventory,
                    PermissionNames.ViewReports,
                    PermissionNames.ViewProfit,
                    PermissionNames.ViewLedger,
                    PermissionNames.ExportLedger,
                    PermissionNames.ManagePayments,
                    PermissionNames.ViewCustomersSuppliers
                },
                [RoleNames.SalesExecutive] = new[]
                {
                    PermissionNames.ViewDashboard,
                    PermissionNames.ViewSales,
                    PermissionNames.CreateSale,
                    PermissionNames.ViewInventory,
                    PermissionNames.ViewCustomersSuppliers
                },
                [RoleNames.Accountant] = new[]
                {
                    PermissionNames.ViewDashboard,
                    PermissionNames.ViewDashboardFinancial,
                    PermissionNames.ViewSales,
                    PermissionNames.ViewPurchases,
                    PermissionNames.ViewReports,
                    PermissionNames.ViewProfit,
                    PermissionNames.ViewLedger,
                    PermissionNames.ExportLedger,
                    PermissionNames.ManagePayments,
                    PermissionNames.ViewCustomersSuppliers
                },
                [RoleNames.InventoryStaff] = new[]
                {
                    PermissionNames.ViewDashboard,
                    PermissionNames.ViewInventory,
                    PermissionNames.ViewPurchases,
                    PermissionNames.CreatePurchase
                }
            };

            foreach (var roleEntry in matrix)
            {
                if (!roleMap.TryGetValue(roleEntry.Key, out var role))
                {
                    continue;
                }

                var targetPermissionIds = roleEntry.Value
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(permissionMap.ContainsKey)
                    .Select(name => permissionMap[name].Id)
                    .ToHashSet();

                var currentAssignments = rolePermissions
                    .Where(rp => rp.RoleId == role.Id)
                    .ToList();

                var currentPermissionIds = currentAssignments
                    .Select(rp => rp.PermissionId)
                    .ToHashSet();

                var missingPermissionIds = targetPermissionIds
                    .Except(currentPermissionIds)
                    .ToList();

                foreach (var permissionId in missingPermissionIds)
                {
                    var assignment = new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId
                    };
                    context.RolePermissions.Add(assignment);
                    rolePermissions.Add(assignment);
                }

                var redundantAssignments = currentAssignments
                    .Where(rp => !targetPermissionIds.Contains(rp.PermissionId))
                    .ToList();

                if (redundantAssignments.Count > 0)
                {
                    context.RolePermissions.RemoveRange(redundantAssignments);
                    foreach (var redundant in redundantAssignments)
                    {
                        rolePermissions.Remove(redundant);
                    }
                }
            }

            if (context.ChangeTracker.HasChanges())
            {
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedDefaultUsersAsync(ApplicationDbContext context)
        {
            var roleMap = await context.Roles.ToDictionaryAsync(
                r => r.Name,
                r => r.Id,
                StringComparer.OrdinalIgnoreCase);

            if (roleMap.TryGetValue(RoleNames.SuperAdmin, out var superAdminRoleId))
            {
                await EnsureUserAsync(
                    context,
                    name: "System Owner",
                    email: "admin@erp.com",
                    defaultPassword: "Admin@123",
                    roleId: superAdminRoleId);
            }

            if (roleMap.TryGetValue(RoleNames.SalesExecutive, out var salesRoleId))
            {
                await EnsureUserAsync(
                    context,
                    name: "ERP Sales User",
                    email: "user@erp.com",
                    defaultPassword: "User@123",
                    roleId: salesRoleId);
            }
        }

        private static async Task EnsureUserAsync(
            ApplicationDbContext context,
            string name,
            string email,
            string defaultPassword,
            int roleId)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                context.Users.Add(new User
                {
                    Name = name,
                    Email = normalizedEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                    RoleId = roleId,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
                await context.SaveChangesAsync();
                return;
            }

            var hasChanges = false;
            if (user.RoleId != roleId)
            {
                user.RoleId = roleId;
                hasChanges = true;
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                hasChanges = true;
            }

            if (string.IsNullOrWhiteSpace(user.Name))
            {
                user.Name = name;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync();
            }
        }
    }
}
