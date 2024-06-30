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
                return BadRequest("Your email was confirmed before. Please login to your account.");
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

        [HttpPost("resend-email-confirmation-link/{email}")]
        public async Task<IActionResult> ResendEmailConfirmationLink(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Invalid email");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Unauthorized("This email address has not been registered yet.");
            }

            if (user.EmailConfirmed == true)
            {
                return BadRequest("Your email was confirmed before. Please login to your account.");
            }

            try
            {
                if(await SendConfirmEmailAsync(user))
                {
                    return CreateJsonResult(200, "Confirmation link sent", "Please confirm your email address.");
                }

                return BadRequest("Failed to send email. Please contact administration");
            }
            catch (Exception)
            {
                return BadRequest("Failed to send email. Please contact administration");
            }
        }

        [HttpPost("forgot-username-or-password/{email}")]
        public async Task<IActionResult> ForgotUsernameOrPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Invalid email");
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return Unauthorized("This email address has not been registerd yet.");
            }

            if (!user.EmailConfirmed)
            {
                return BadRequest("Please confirm your email address first.");
            }

            try
            {
                if (await SendForgotUsernameOrPasswordEmailAsync(user))
                {
                    return CreateJsonResult(200, "Forgot username or password email sent", "Please check your email");
                }

                return BadRequest("Failed to send email. Please contact administration");
            }
            catch (Exception)
            {
                return BadRequest("Failed to send email. Please contact administration");
            }
        }

        [HttpPut("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized("This email address has not been registered yet.");
            }
            if (!user.EmailConfirmed)
            {
                return BadRequest("Please confirm your email first.");
            }

            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(model.Token);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

                var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
                if (result.Succeeded)
                {
                    return CreateJsonResult(200, "Password reset success", "Your password has been reset.");
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
                FirstName = user.FirstName,
                LastName = user.LastName,
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
            int year = DateTime.Now.Year;

            var containerStyle = "font-family: Arial, sans-serif; width: 100%; max-width: 576px; margin: 0 auto; background-color: #ffffff; padding: 20px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);";
            var headerStyle = "text-align: center; padding: 20px 0; border-bottom: 1px solid #dddddd;";
            var h1Style = "margin: 0; color: #333333;";
            var contentStyle = "padding: 20px;";
            var pStyle = "line-height: 1.5; color: #666666;";
            var aStyle = "display: inline-block; padding: 10px 20px; background-color: #007bff; color: #ffffff; text-decoration: none; border-radius: 8px;";
            var footerStyle = "text-align: center; padding: 20px; border-top: 1px solid #dddddd; color: #999999; font-size: 12px;";

            //var body = $"<p>Hello {firstName},</p>" +
            //    "<p>Please confirm your email address by clicking on the following link.</p>" +
            //    $"<p><a href=\"{url}\">Click here.</a></p>" +
            //    $"<p>Thank you,<br>The {appName} Team</p>" +
            //    $"<div style=\"{footerStyle}\">" +
            //    $"<p>&copy; {year} {appName}. All rights reserved.</p>" +
            //    "</div>";

            var body = $"<div style=\"{containerStyle}\">" +
                $"<div style=\"{headerStyle}\">" +
                $"<h1 style=\"{h1Style}\">Email Confirmation</h1>" +
                "</div>" +
                $"<div style=\"{contentStyle}\">" +
                $"<p style=\"{pStyle}\">Hi {firstName},</p>" +
                $"<p style=\"{pStyle}\">Please confirm your email address by clicking the button below:</p>" +
                $"<a style=\"{aStyle}\" href=\"{url}\">Confirm Email</a>" +
                $"<p style=\"{pStyle}\">If you did not sign up for this account, you can ignore this email.</p>" +
                $"<p style=\"{pStyle}\">Thanks,<br>The {appName} Team</p>" +
                "</div>" +
                $"<div style=\"{footerStyle}\">" +
                $"<p>&copy; {year} {appName}. All rights reserved.</p>" +
                "</div>";

            var emailSend = new EmailSendDto(user.Email, "Confirm your email", body, true);

            return await _emailService.SendEmailAsync(emailSend);
        }

        private async Task<bool> SendForgotUsernameOrPasswordEmailAsync(User user)
        {
            var token =  await _userManager.GeneratePasswordResetTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var clientUrl = _configuration["JWT:ClientUrl"];
            var resetPasswordPath = _configuration["Email:ResetPasswordPath"];

            var url = $"{clientUrl}/{resetPasswordPath}?token={token}&email={user.Email}";

            var firstName = CapitalizeFirstLetter(user.FirstName);
            var appName = _configuration["Email:ApplicationName"];
            int year = DateTime.Now.Year;

            var containerStyle = "font-family: Arial, sans-serif; width: 100%; max-width: 576px; margin: 0 auto; background-color: #ffffff; padding: 20px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);";
            var headerStyle = "text-align: center; padding: 20px 0; border-bottom: 1px solid #dddddd;";
            var h1Style = "margin: 0; color: #333333;";
            var contentStyle = "padding: 20px;";
            var pStyle = " line-height: 1.5; color: #666666;";
            var aStyle = "display: inline-block; padding: 10px 20px; background-color: #007bff; color: #ffffff; text-decoration: none; border-radius: 8px;";
            var footerStyle = "text-align: center; padding: 20px; border-top: 1px solid #dddddd; color: #999999; font-size: 12px;";        

            var body = $"<div style=\"{containerStyle}\">" +
                $"<div style=\"{headerStyle}\">" +
                $"<h1 style=\"{h1Style}\">Forgot Username or Password</h1>" +
                "</div>" +
                $"<div style=\"{contentStyle}\">" +
                $"<p style=\"{pStyle}\">Hi {firstName},</p>" +
                $"<p style=\"{pStyle}\">Username: {user.UserName},</p>" +
                $"<p style=\"{pStyle}\">In order to reset your password, please click on the following button:</p>" +
                $"<a style=\"{aStyle}\" href=\"{url}\">Reset Password</a>" +
                $"<p style=\"{pStyle}\">If you did not sign up for this account, you can ignore this email.</p>" +
                $"<p style=\"{pStyle}\">Thanks,<br>The {appName} Team</p>" +
                "</div>" +
                $"<div style=\"{footerStyle}\">" +
                $"<p>&copy; {year} {appName}. All rights reserved.</p>" +
                "</div>";

            var emailSend = new EmailSendDto(user.Email, "Forgot username or password", body, true);

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

            response.ContentType = "application/json";
            response.StatusCode = statusCode;

            return response;
        }

        #endregion
    }
}
