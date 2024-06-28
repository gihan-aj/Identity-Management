using IdentityManagementApp.Data;
using IdentityManagementApp.DTOs.Account;
using IdentityManagementApp.Models;
using IdentityManagementApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace IdentityManagementApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly JwtService _jwtService;
        private readonly IConfiguration _configuration;

        public AccountController(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            JwtService jwtService,
            IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            if(user.EmailConfirmed == false)
            {
                return Unauthorized("Please confirm your email.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if(result.IsLockedOut)
            {
                return Unauthorized(string.Format("Your account has been locked. You should wait until {0} (UTC time) to be able to login.", user.LockoutEnd));
            }

            if(!result.Succeeded)
            {
                // invalid password
                if(!user.UserName.Equals(SeedData.AdminUserName))
                {
                    // incrementing AccessFailedCount of the AspNetUser by 1
                    await _userManager.AccessFailedAsync(user);
                }

                if(user.AccessFailedCount > SeedData.MaximumLoginAttempts)
                {
                    // lock the user for one day
                    await _userManager.SetLockoutEndDateAsync(user, DateTime.UtcNow.AddDays(1));
                    return Unauthorized(string.Format("Your account has been locked. You should wait until {0} (UTC time) to be able to login.", user.LockoutEnd));
                }

                return Unauthorized("Invalid username or password.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, null);

            return await CreateApplicationUserDto(user);
        }

        #region Private Helper Methods

        private async Task<ActionResult<UserDto>> CreateApplicationUserDto(User user)
        {
            return new UserDto
            {
                FirstName = user.UserName,
                LastName = user.UserName,
                JWT = await _jwtService.CreateJWT(user),
            };
        }

        #endregion
    }
}
