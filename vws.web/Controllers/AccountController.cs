﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using vws.web.Models;
using vws.web.Repositories;

namespace vws.web.Controllers
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IEmailSender emailSender;
        private readonly IConfiguration configuration;
        private readonly IPasswordHasher<ApplicationUser> passwordHasher;
        private readonly IStringLocalizer<AccountController> localizer;
        private readonly EmailAddressAttribute emailChecker;
        private readonly Random random;

        public AccountController(UserManager<ApplicationUser> _userManager, RoleManager<IdentityRole> _roleManager,
            SignInManager<ApplicationUser> _signInManager, IConfiguration _configuration, IEmailSender _emailSender,
            IPasswordHasher<ApplicationUser> _passwordHasher, IStringLocalizer<AccountController> _localizer)
        {
            userManager = _userManager;
            roleManager = _roleManager;
            signInManager = _signInManager;
            configuration = _configuration;
            emailSender = _emailSender;
            passwordHasher = _passwordHasher;
            localizer = _localizer;
            emailChecker = new EmailAddressAttribute();
            random = new Random();
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            List<string> errors = new List<string>();

            if(model.Username.Contains("@"))
            {
                errors.Add(localizer["Username should not contain @ character."]);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseModel { Status = "Error", HasError = true, Message = "Username has @ character.", Errors = errors });
            }

            if(!emailChecker.IsValid(model.Email))
            {
                errors.Add(localizer["Email is invalid."]);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseModel { Status = "Error", HasError = true, Message = "Invalid Email.", Errors = errors });
            }

            var userExists = await userManager.FindByNameAsync(model.Username);
            if (userExists != null)
            {
                errors.Add(localizer["User already exists!"]);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseModel { Status = "Error", HasError = true, Message = "User already exists!", Errors = errors });
            }

            ApplicationUser user = new ApplicationUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };

            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    errors.Add(localizer[error.Description]);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User creation failed!", HasError = true, Errors = errors });
            }

            return Ok(new ResponseModel { Status = "Success", Message = "User created successfully!", HasError = false });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            bool rememberMe = true;
            if (model.RememberMe.HasValue)
                rememberMe = model.RememberMe.Value;

            bool isEmail = false;
            if (model.UsernameOrEmail.Contains("@"))
                isEmail = true;

            var user = isEmail ? await userManager.FindByEmailAsync(model.UsernameOrEmail) :
                await userManager.FindByNameAsync(model.UsernameOrEmail);

            if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
            {
                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));

                var token = new JwtSecurityToken(
                    issuer: configuration["JWT:Issuer"],
                    audience: configuration["JWT:Audience"],
                    expires: DateTime.Now.AddMinutes(2),
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                    );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }
            var response = new ResponseModel
            {
                HasError = true,
                Message = "User login failed.",
                Status = "Error"
            };
            response.AddError(localizer["Password or Usernamed is wrong."]);
            return StatusCode(StatusCodes.Status401Unauthorized, response);
        }

        [HttpPost]
        [Route("confirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ValidationModel model)
        {
            List<string> errors = new List<string>();

            if (!emailChecker.IsValid(model.Email))
            {
                errors.Add(localizer["Email is invalid."]);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseModel { Status = "Error", HasError = true, Message = "Invalid Email.", Errors = errors });
            }

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                errors.Add(localizer["User does not exist."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User does not exist!", Errors = errors, HasError = true });
            }

            var timeDiff = user.EmailVerificationSendTime - DateTime.Now;

            if (user.EmailConfirmed)
            {
                errors.Add(localizer["Email already confirmed."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Email already confirmed!", Errors = errors, HasError = true });
            }

            if(user.EmailVerificationCode == model.ValidationCode &&
                timeDiff.TotalMinutes <= Int16.Parse(configuration["EmailCode:ValidDurationTimeInMinutes"]))
            {
                user.EmailConfirmed = true;
                var result = await userManager.UpdateAsync(user);
                if(result.Succeeded)
                    return Ok(new ResponseModel { Status = "Success", Message = "Email confirmed successfully!" });
            }

            errors.Add(localizer["Emil confirmation failed."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Email confirmation failed!", Errors = errors, HasError = true });
        }

        [HttpPost]
        [Route("sendConfirmEmail")]
        public async Task<IActionResult> SendConfirmEmail([FromBody] UserModel model)
        {

            var user = await userManager.FindByEmailAsync(model.Email);
            if(user == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User does not exist!" });

            var randomCode = new string(Enumerable.Repeat(configuration["EmailCode:CodeCharSet"], Int16.Parse(configuration["EmailCode:SizeOfCode"])).Select(s => s[random.Next(s.Length)]).ToArray());

            user.EmailVerificationSendTime = DateTime.Now;
            user.EmailVerificationCode = randomCode;
            var result = await userManager.UpdateAsync(user);
            if(result.Succeeded)
            {
                await emailSender.SendEmailAsync(user.Email, "EmailConfirmation", randomCode);
                return Ok(new ResponseModel { Status = "Success", Message = "Email sent successfully!" });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Sending email failed!" });
        }

        [HttpPost]
        [Route("sendResetPassEmail")]
        public async Task<IActionResult> SendResetPassEmail([FromBody] UserModel model)
        {

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User does not exist!" });

            var randomCode = new string(Enumerable.Repeat(configuration["EmailCode:CodeCharSet"], Int16.Parse(configuration["EmailCode:SizeOfCode"])).Select(s => s[random.Next(s.Length)]).ToArray());

            user.ResetPasswordSendTime = DateTime.Now;
            user.ResetPasswordCode = randomCode;
            user.ResetPasswordCodeIsValid = true;
            var result = await userManager.UpdateAsync(user);

            if(result.Succeeded)
            {
                await emailSender.SendEmailAsync(user.Email, "EmailConfirmation", randomCode);
                return Ok(new ResponseModel { Status = "Success", Message = "Email sent successfully!" });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Sending email failed!" });
        }

        [HttpPost]
        [Route("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);

            if(user == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User does not exist!" });

            var timeDiff = user.ResetPasswordSendTime - DateTime.Now;

            if (!user.ResetPasswordCodeIsValid)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Reset password is not valid!" });

            if (user.ResetPasswordCode == model.ValidationCode &&
                timeDiff.TotalMinutes <= Int16.Parse(configuration["EmailCode:ValidDurationTimeInMinutes"]))
            {
                var passwordValidator = new PasswordValidator<ApplicationUser>();
                var result = await passwordValidator.ValidateAsync(userManager, user, model.NewPassword);
                if(result.Succeeded)
                {
                    user.PasswordHash = passwordHasher.HashPassword(user, model.NewPassword);
                    user.ResetPasswordCodeIsValid = false;
                    var res = await userManager.UpdateAsync(user);
                    if(res.Succeeded == true)
                        return Ok(new ResponseModel { Status = "Success", Message = "Password changed successfully!" });
                    return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Password changing failed!" });
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "New password is not valid!" });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Password changing failed!" });
        }
    }
}
