using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using vws.web.Domain.dbo;
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

            var userExistsWithUserName = await userManager.FindByNameAsync(model.Username);

            var userExistsWithEmail = await userManager.FindByEmailAsync(model.Email);

            if(userExistsWithEmail != null)
            {
                if(userExistsWithEmail.EmailConfirmed)
                {
                    errors.Add(localizer["There is a user with this email."]);
                    if(userExistsWithUserName != null)
                        errors.Add(localizer["There is a user with this username."]);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ResponseModel { Status = "Error", HasError = true, Message = "User already exists!", Errors = errors });
                }
                else
                {
                    if(userExistsWithUserName != null)
                    {
                        errors.Add(localizer["There is a user with this username."]);
                        return StatusCode(StatusCodes.Status500InternalServerError,
                            new ResponseModel { Status = "Error", HasError = true, Message = "User already exists!", Errors = errors });
                    }
                    else
                    {
                        await userManager.DeleteAsync(userExistsWithEmail);
                    }
                }
            }
            else
            {
                if(userExistsWithUserName != null)
                {
                    errors.Add(localizer["There is a user with this username."]);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ResponseModel { Status = "Error", HasError = true, Message = "User already exists!", Errors = errors });
                }
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
                if(user.EmailConfirmed == false)
                {
                    var _response = new ResponseModel
                    {
                        HasError = true,
                        Message = "User login failed.",
                        Status = "Error"
                    };
                    _response.AddError(localizer["Email is not confirmed yet."]);
                    return StatusCode(StatusCodes.Status401Unauthorized, _response);
                }

                var authClaims = new List<Claim>
                {
                    new Claim("UserEmail", user.Email)
                };

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));

                var token = new JwtSecurityToken(
                    issuer: configuration["JWT:Issuer"],
                    audience: configuration["JWT:Audience"],
                    expires: DateTime.Now.AddMinutes(10),
                    claims: authClaims,
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

            if (user.EmailVerificationCode == model.ValidationCode &&
                timeDiff.TotalMinutes <= Int16.Parse(configuration["EmailCode:ValidDurationTimeInMinutes"]))
            {
                user.EmailConfirmed = true;
                var result = await userManager.UpdateAsync(user);
                if(result.Succeeded)
                    return Ok(new ResponseModel { Status = "Success", Message = "Email confirmed successfully!", HasError = false });
            }

            errors.Add(localizer["Emil confirmation failed."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Email confirmation failed!", Errors = errors, HasError = true });
        }

        [HttpPost]
        [Route("sendConfirmEmail")]
        public async Task<IActionResult> SendConfirmEmail([FromBody] EmailModel model)
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
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User does not exist!", HasError = true, Errors = errors });
            }

            if (user.EmailConfirmed)
            {
                errors.Add(localizer["Email already confirmed."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Email already confirmed!", Errors = errors, HasError = true });
            }

            var randomCode = new string(Enumerable.Repeat(configuration["EmailCode:CodeCharSet"], Int16.Parse(configuration["EmailCode:SizeOfCode"])).Select(s => s[random.Next(s.Length)]).ToArray());

            user.EmailVerificationSendTime = DateTime.Now;
            user.EmailVerificationCode = randomCode;
            var result = await userManager.UpdateAsync(user);
            if(result.Succeeded)
            {
                string emailErrorMessage;
                await emailSender.SendEmailAsync(user.Email, "EmailConfirmation", randomCode, configuration, out emailErrorMessage);
                if(string.IsNullOrEmpty(emailErrorMessage))
                    return Ok(new ResponseModel { Status = "Success", Message = "Email sent successfully!", HasError = false });
                errors.Add(localizer[emailErrorMessage]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Sending email failed!", Errors = errors, HasError = true });
            }
            errors.Add(localizer["Problem happened in sending email."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Sending email failed!", Errors = errors, HasError = true });
        }

        [HttpPost]
        [Route("sendResetPassEmail")]
        public async Task<IActionResult> SendResetPassEmail([FromBody] EmailModel model)
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
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User does not exist!", HasError = true, Errors = errors });
            }

            var randomCode = new string(Enumerable.Repeat(configuration["EmailCode:CodeCharSet"], Int16.Parse(configuration["EmailCode:SizeOfCode"])).Select(s => s[random.Next(s.Length)]).ToArray());

            user.ResetPasswordSendTime = DateTime.Now;
            user.ResetPasswordCode = randomCode;
            user.ResetPasswordCodeIsValid = true;
            var result = await userManager.UpdateAsync(user);

            if(result.Succeeded)
            {
                string emailErrorMessage;
                await emailSender.SendEmailAsync(user.Email, "EmailConfirmation", randomCode, configuration, out emailErrorMessage);
                if (string.IsNullOrEmpty(emailErrorMessage))
                    return Ok(new ResponseModel { Status = "Success", Message = "Email sent successfully!", HasError = false });
                errors.Add(localizer[emailErrorMessage]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Sending email failed!", Errors = errors, HasError = true });
            }
            errors.Add(localizer["Problem happened in sending email."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Sending email failed!", Errors = errors, HasError = true });
        }

        [HttpPost]
        [Route("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
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
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User does not exist!", HasError = true, Errors = errors });
            }

            var timeDiff = user.ResetPasswordSendTime - DateTime.Now;

            if (!user.ResetPasswordCodeIsValid)
            {
                errors.Add(localizer["Request for reset password is not valid. Request for reset password again."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Reset password is not valid!", HasError = true, Errors = errors });
            }

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
                        return Ok(new ResponseModel { Status = "Success", Message = "Password changed successfully!", HasError = false });
                    errors.Add("Reseting the password was unsuccessful.");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Password changing failed!", HasError = true, Errors = errors });
                }
                foreach (var error in result.Errors)
                {
                    errors.Add(localizer[error.Description]);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "New password is not valid!", Errors = errors, HasError = true });
            }
            errors.Add(localizer["Reset password failed. Code is invalid or code validation time is paased."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Password changing failed!", Errors = errors, HasError = true });
        }

        [HttpPost]
        [Route("isUserNameInUse")]
        public async Task<bool> IsUserNameInUse(UsernameModel model)
        {
            var user = await userManager.FindByNameAsync(model.Username);
            if (user == null) return false;
            return true;
        }

        [HttpPost]
        [Route("isEmailInUse")]
        public async Task<bool> IsEmailInUse(EmailModel model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null) return false;
            if (user.EmailConfirmed)
                return true;
            return false;
        }

        [HttpGet]
        [Authorize]
        [Route("getUserEmail")]
        public string GetUserEmail()
        {
            return User.Claims.First(claim=>claim.Type == "UserEmail").Value;
        }
    }
}
