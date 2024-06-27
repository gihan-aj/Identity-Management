using IdentityManagementApp.Data;
using IdentityManagementApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityManagementApp.Services
{
    public class ContextSeedService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ContextSeedService(AppDbContext db , UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task InitializeContextAsync()
        {
            if(_db.Database.GetPendingMigrationsAsync().GetAwaiter().GetResult().Count() > 0)
            {
                // applying any pending migrations
                await _db.Database.MigrateAsync();
            }

            if (!_roleManager.Roles.Any())
            {
                await _roleManager.CreateAsync(new IdentityRole { Name = SeedData.AdminRole });
                await _roleManager.CreateAsync(new IdentityRole { Name = SeedData.ManagerRole });
                await _roleManager.CreateAsync(new IdentityRole { Name = SeedData.SupervisorRole });
                await _roleManager.CreateAsync(new IdentityRole { Name = SeedData.MemberRole });
            }

            if (!_userManager.Users.AnyAsync().GetAwaiter().GetResult())
            {
                var admin = new User
                {
                    FirstName = "Admin",
                    LastName = "User",
                    UserName = SeedData.AdminUserName,
                    Email = SeedData.AdminUserName,
                    EmailConfirmed = true,
                };

                await _userManager.CreateAsync(admin, "123456");

                await _userManager.AddToRolesAsync(admin , new[] 
                { 
                    SeedData.AdminRole, 
                    SeedData.ManagerRole, 
                    SeedData.SupervisorRole, 
                    SeedData.MemberRole 
                });

                await _userManager.AddClaimsAsync(admin, new Claim[]
                {
                    new Claim(ClaimTypes.Email, admin.Email),
                    new Claim(ClaimTypes.Surname, admin.LastName)
                });

                var manager = new User
                {
                    FirstName = "Manager",
                    LastName = "User",
                    UserName = "manager@example.com",
                    Email = "manager@example.com",
                    EmailConfirmed = true,
                };

                await _userManager.CreateAsync(manager, "123456");

                await _userManager.AddToRolesAsync(manager, new[]
                {              
                    SeedData.ManagerRole,                   
                });

                await _userManager.AddClaimsAsync(manager, new Claim[]
                {
                    new Claim(ClaimTypes.Email, manager.Email),
                    new Claim(ClaimTypes.Surname, manager.LastName)
                });

                var supervisor = new User
                {
                    FirstName = "Supervisor",
                    LastName = "User",
                    UserName = "supervisor@example.com",
                    Email = "supervisor@example.com",
                    EmailConfirmed = true,
                };

                await _userManager.CreateAsync(supervisor, "123456");

                await _userManager.AddToRolesAsync(supervisor, new[]
                {
                    SeedData.SupervisorRole,
                });

                await _userManager.AddClaimsAsync(supervisor, new Claim[]
                {
                    new Claim(ClaimTypes.Email, supervisor.Email),
                    new Claim(ClaimTypes.Surname, supervisor.LastName)
                });

                var member = new User
                {
                    FirstName = "Member",
                    LastName = "User",
                    UserName = "member@example.com",
                    Email = "member@example.com",
                    EmailConfirmed = true,
                };

                await _userManager.CreateAsync(member, "123456");

                await _userManager.AddToRolesAsync(member, new[]
                {
                    SeedData.MemberRole
                });

                await _userManager.AddClaimsAsync(member, new Claim[]
                {
                    new Claim(ClaimTypes.Email, member.Email),
                    new Claim(ClaimTypes.Surname, member.LastName)
                });
            }
        }
    }
}
