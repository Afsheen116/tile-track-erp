using CeramicERP.Data;
using CeramicERP.Models;
using CeramicERP.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace CeramicERP.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private static readonly string[] SelfRegistrationRoles =
        {
            RoleNames.AdminManager,
            RoleNames.SalesExecutive,
            RoleNames.Accountant,
            RoleNames.InventoryStaff
        };

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User?.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            var normalizedEmail = email?.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null ||
                !user.IsActive ||
                !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ViewBag.Error = "Invalid credentials";
                return View();
            }

            var appRole = user.Role?.Name ?? RoleNames.SalesExecutive;
            var permissions = user.Role?.RolePermissions?
                .Select(rp => rp.Permission?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, appRole)
            };

            foreach (var permission in permissions)
            {
                claims.Add(new Claim(CustomClaimTypes.Permission, permission!));
            }

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            if (User?.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new RegisterViewModel
            {
                RoleName = RoleNames.SalesExecutive
            };

            await LoadRegisterRolesAsync(model.RoleName);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            await LoadRegisterRolesAsync(model.RoleName);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = model.Email.Trim().ToLowerInvariant();
            var normalizedName = string.Join(" ", model.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                ModelState.AddModelError(nameof(model.Name), "Name is required.");
                return View(model);
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == normalizedEmail);

            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
                return View(model);
            }

            if (!SelfRegistrationRoles.Contains(model.RoleName, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.RoleName), "Selected role is not allowed.");
                return View(model);
            }

            var userRole = await _context.Roles.FirstOrDefaultAsync(
                r => r.Name == model.RoleName);

            if (userRole == null)
            {
                ModelState.AddModelError(nameof(model.RoleName), "Selected role is not available.");
                return View(model);
            }

            var user = new User
            {
                Name = normalizedName,
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                RoleId = userRole.Id,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Account created successfully. You can now login.";
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Login));
            }

            var model = new UserProfileViewModel
            {
                Name = user.Name,
                Email = user.Email,
                Role = user.Role?.Name ?? RoleNames.SalesExecutive,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private async Task LoadRegisterRolesAsync(string? selectedRoleName)
        {
            var allowedRoles = await _context.Roles
                .Where(r => SelfRegistrationRoles.Contains(r.Name))
                .OrderBy(r => r.Name)
                .Select(r => r.Name)
                .ToListAsync();

            ViewBag.RegisterRoles = new SelectList(
                allowedRoles,
                selectedRoleName ?? RoleNames.SalesExecutive);
        }

    }
}
