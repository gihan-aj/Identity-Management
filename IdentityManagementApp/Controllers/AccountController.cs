using IdentityManagementApp.Data;
using IdentityManagementApp.DTOs.Account;
using IdentityManagementApp.Models;
using IdentityManagementApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Runtime.InteropServices;
using System.Text;
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
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public AccountController(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            JwtService jwtService,
            EmailService emailService,
            IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _jwtService = jwtService;
            _emailService = emailService;
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

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if(await CheckEmailExistsAsync(model.Email))
            {
                return BadRequest($"An account is already created using {model.Email}. Please try with another email address.");
            }

            var userToAdd = new User
            {
                FirstName = model.FirstName.ToLower(),
                LastName = model.LastName.ToLower(),
                UserName = model.Email.ToLower(),
                Email = model.Email.ToLower(),
            };

            // create user inside AspNetUsers table
            var result = await _userManager.CreateAsync(userToAdd, model.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await _userManager.AddToRoleAsync(userToAdd, SeedData.MemberRole);

            try
            {
                if(await SendConfirmEmailAsync(userToAdd))
                {
                    return CreateJsonResult(201, "Account Created", "Your account has been created. please confirm your email address.");

                    //return Ok(new JsonResult(new 
                    //{ 
                    //    title = "Account Created", 
                    //    message = "Your account has been created, please confirm your email address." 
                    //}));
                }

                return BadRequest("Failed to send email. Please contact administration");
            }
            catch (Exception)
            {
                return BadRequest("Failed to send email. Please contact administration");
            }
        }

        [HttpPut("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized("This email has not been registered yet.");
            }

            if(user.EmailConfirmed == true)
            {
                return BadRequest("Your email has confirmed before. Please login to your account.");
            }

            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(model.Token);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if(result.Succeeded == true)
                {
                    return CreateJsonResult(200, "Email Confirmed", "Your email address is confirmed. You can login now.");
                }

                return BadRequest("Invalid token. Please try again");
            }
            catch (Exception)
            {
                return BadRequest("Invalid token. Please try again");
            }
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

        private async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await _userManager.Users.AnyAsync(x => x.Email == email.ToLower());
        }

        private async Task<bool> SendConfirmEmailAsync(User user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var clientUrl = _configuration["JWT:ClientUrl"];
            var confirmEmailPath = _configuration["Email:ConfirmEmailPath"];

            var url = $"{clientUrl}/{confirmEmailPath}?token={token}&email={user.Email}";

            var firstName = CapitalizeFirstLetter(user.FirstName);
            var appName = _configuration["Email:ApplicationName"];

            var body = $"<p>Hello {firstName},</p>" +
                "<p>Please confirm your email address by clicking on the following link.</p>" +
                $"<p><a href=\"{url}\">Click here.</a></p>" +
                "<p>Thank you,</p>" +
                $"<p>{appName}</p>";

            var emailSend = new EmailSendDto(user.Email, "Confirm your email", body, true);

            return await _emailService.SendEmailAsync(emailSend);
        }

        private string CapitalizeFirstLetter(string name)
        {
            if (string.IsNullOrEmpty(name)) 
            {  
                return name; 
            }

            return char.ToUpper(name[0]) + name.Substring(1);
        }

        private JsonResult CreateJsonResult(int statusCode, string title, string message)
        {
            var response = new JsonResult(new
            {
                title = title,
                message = message
            });

            response.StatusCode = statusCode;

            return response;
        }

        #endregion
    }
}
