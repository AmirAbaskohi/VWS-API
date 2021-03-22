using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._file;
using vws.web.EmailTemplates;
using vws.web.Enums;
using vws.web.Models;
using vws.web.Models._account;
using vws.web.Repositories;
using static vws.web.EmailTemplates.EmailTemplateTypes;

namespace vws.web.Controllers._account
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class AccountController : BaseController
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
        private readonly IVWS_DbContext vwsDbContext;
        private readonly IFileManager fileManager;

        public AccountController(UserManager<ApplicationUser> _userManager, RoleManager<IdentityRole> _roleManager,
            SignInManager<ApplicationUser> _signInManager, IConfiguration _configuration, IEmailSender _emailSender,
            IPasswordHasher<ApplicationUser> _passwordHasher, IStringLocalizer<AccountController> _localizer,
            IVWS_DbContext _vwsDbContext, IFileManager _fileManager)
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
            vwsDbContext = _vwsDbContext;
            fileManager = _fileManager;
        }

        #region Private Methods

        private JwtSecurityToken GenerateToken(IEnumerable<Claim> claims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: configuration["JWT:Issuer"],
                audience: configuration["JWT:Audience"],
                expires: DateTime.Now.AddMinutes(Int16.Parse(configuration["JWT:ValidTimeInMinutes"])),
                claims: claims.ToList(),
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private async Task<JwtTokenModel> GenerateJWT(IdentityUser user, string nickName)
        {
            var authClaims = new List<Claim>
                {
                    new Claim("UserEmail", user.Email),
                    new Claim("NickName", nickName),
                    new Claim("UserId", user.Id),
                };

            var token = GenerateToken(authClaims);

            var refreshToken = new RefreshToken()
            {
                IsValid = true,
                Token = GenerateRefreshToken(),
                UserId = new Guid(user.Id)
            };
            await vwsDbContext.AddRefreshTokenAsync(refreshToken);
            vwsDbContext.Save();

            return new JwtTokenModel
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken.Token,
                ValidTo = token.ValidTo
            };
        }

        //private async Task<LoginResponseModel> GenerateJWT(IdentityUser user)
        //{
        //    var authClaims = new List<Claim>
        //        {
        //            new Claim("UserEmail", user.Email),
        //            new Claim("UserName", user.UserName),
        //            new Claim("UserId", user.Id),
        //        };

        //    var token = GenerateToken(authClaims);

        //    var refreshToken = new RefreshToken()
        //    {
        //        IsValid = true,
        //        Token = GenerateRefreshToken(),
        //        UserId = new Guid(user.Id)
        //    };
        //    await vwsDbContext.AddRefreshTokenAsync(refreshToken);
        //    vwsDbContext.Save();

        //    return new LoginResponseModel
        //    {
        //        Token = new JwtSecurityTokenHandler().WriteToken(token),
        //        RefreshToken = refreshToken.Token,
        //        ValidTo = token.ValidTo
        //    };
        //}

        private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleToken(string googleTokenId)
        {
            GoogleJsonWebSignature.ValidationSettings settings = new GoogleJsonWebSignature.ValidationSettings();
            settings.Audience = new List<string>() { configuration["Google:ClientId"] };
            var g = await GoogleJsonWebSignature.ValidateAsync(googleTokenId, settings);
            GoogleJsonWebSignature.Payload payload = g;
            return payload;
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"])),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        private async Task CreateUserProfile(Guid userId)
        {
            UserProfile userProfile = new UserProfile()
            {
                UserId = userId,
                ThemeColorCode = ""
            };
            await vwsDbContext.AddUserProfileAsync(userProfile);
            vwsDbContext.Save();
        }

        private void CreateUserTaskStatuses(Guid userId)
        {
            vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 2, ProjectId = null, UserProfileId = userId, TeamId = null, Title = "To Do" });
            vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 4, ProjectId = null, UserProfileId = userId, TeamId = null, Title = "Doing" });
            vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 6, ProjectId = null, UserProfileId = userId, TeamId = null, Title = "Done" });

            vwsDbContext.Save();
        }

        #endregion

        [HttpPost]
        [Route("loginRegister")]
        public async Task<IActionResult> LoginRegister([FromBody] LoginRegisterModel model)
        {
            ResponseModel<LoginRegisterResponseModel> responseModel = new ResponseModel<LoginRegisterResponseModel>();

            if (!emailChecker.IsValid(model.Email))
            {
                responseModel.AddError(localizer["Email is invalid."]);
                responseModel.Message = "Invalid Email.";
                return StatusCode(StatusCodes.Status400BadRequest, responseModel);
            }

            var user = await userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                responseModel.Value.EmailConfirmed = user.EmailConfirmed;

                if (await userManager.CheckPasswordAsync(user, model.Password))
                {
                    if (responseModel.Value.EmailConfirmed)
                    {
                        var userProfile = await vwsDbContext.GetUserProfileAsync(new Guid(user.Id));
                        if (string.IsNullOrWhiteSpace(userProfile.NickName))
                        {
                            responseModel.Value.HasNickName = false;
                            responseModel.Message = "User has not nick-name.";
                            return Ok(responseModel);
                        }
                        else
                        {
                            responseModel.Value.HasNickName = true;
                            responseModel.Value.JwtToken = await GenerateJWT(user, userProfile.NickName);
                            responseModel.Message = "Logged in successfully.";
                            return Ok(responseModel);
                        }
                    }
                    else
                    {
                        responseModel.Value.HasNickName = false;
                        responseModel.Message = "Email is not confirmed";
                        return Ok(responseModel);
                    }
                }
                else
                {
                    responseModel.Message = "User login failed.";
                    responseModel.AddError(localizer["Email or password is wrong."]);
                    return StatusCode(StatusCodes.Status401Unauthorized, responseModel);
                }
            }
            else
            {
                responseModel.Value.EmailConfirmed = false;
                responseModel.Value.HasNickName = false;
                #region Create AspNetUser
                ApplicationUser applicationUser = new ApplicationUser()
                {
                    Email = model.Email,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                var result = await userManager.CreateAsync(applicationUser, model.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        responseModel.AddError(localizer[error.Description]);
                    responseModel.Message = "User creation failed!";
                    return StatusCode(StatusCodes.Status500InternalServerError, responseModel);
                }
                #endregion
                try
                {
                    await CreateUserProfile(Guid.Parse(applicationUser.Id));
                }
                catch
                {
                    responseModel.AddError(localizer["User creation failed."]);
                    responseModel.Message = "User creation failed!";
                    return StatusCode(StatusCodes.Status500InternalServerError, responseModel);
                }
                responseModel.Message = "User created successfully!";
                return Ok(responseModel);
            }
        }

        //[HttpPost]
        //[Route("register")]
        //public async Task<IActionResult> Register([FromBody] RegisterModel model)
        //{
        //    List<string> errors = new List<string>();

        //    if (model.Username.Contains("@"))
        //    {
        //        errors.Add(localizer["Username should not contain @ character."]);
        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new ResponseModel { Message = "Username has @ character.", Errors = errors });
        //    }

        //    if (!emailChecker.IsValid(model.Email))
        //    {
        //        errors.Add(localizer["Email is invalid."]);
        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new ResponseModel { Message = "Invalid Email.", Errors = errors });
        //    }

        //    var userExistsWithUserName = await userManager.FindByNameAsync(model.Username);

        //    var userExistsWithEmail = await userManager.FindByEmailAsync(model.Email);

        //    if (userExistsWithEmail != null)
        //    {
        //        if (userExistsWithEmail.EmailConfirmed)
        //        {
        //            errors.Add(localizer["There is a user with this email."]);
        //            if (userExistsWithUserName != null)
        //                errors.Add(localizer["There is a user with this username."]);
        //            return StatusCode(StatusCodes.Status500InternalServerError,
        //                new ResponseModel { Message = "User already exists!", Errors = errors });
        //        }
        //        else
        //        {
        //            if (userExistsWithUserName != null)
        //            {
        //                errors.Add(localizer["There is a user with this username."]);
        //                return StatusCode(StatusCodes.Status500InternalServerError,
        //                    new ResponseModel { Message = "User already exists!", Errors = errors });
        //            }
        //            else
        //            {
        //                Guid userId = new Guid(userExistsWithEmail.Id);
        //                await userManager.DeleteAsync(userExistsWithEmail);
        //                vwsDbContext.DeleteUserProfile(await vwsDbContext.GetUserProfileAsync(userId));
        //                vwsDbContext.Save();
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (userExistsWithUserName != null)
        //        {
        //            errors.Add(localizer["There is a user with this username."]);
        //            return StatusCode(StatusCodes.Status500InternalServerError,
        //                new ResponseModel { Message = "User already exists!", Errors = errors });
        //        }
        //    }

        //    ApplicationUser user = new ApplicationUser()
        //    {
        //        Email = model.Email,
        //        SecurityStamp = Guid.NewGuid().ToString(),
        //        UserName = model.Username
        //    };

        //    var result = await userManager.CreateAsync(user, model.Password);
        //    if (!result.Succeeded)
        //    {
        //        foreach (var error in result.Errors)
        //        {
        //            errors.Add(localizer[error.Description]);
        //        }
        //        return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "User creation failed!", Errors = errors });
        //    }

        //    await CreateUserProfile(Guid.Parse(user.Id));

        //    return Ok(new ResponseModel { Message = "User created successfully!" });
        //}

        //[HttpPost]
        //[Route("sso")]
        //public async Task<IActionResult> SSO([FromBody] ExternalLoginModel model)
        //{
        //    var _response = new ResponseModel();
        //    switch (model.ProviderName)
        //    {
        //        case "Google":
        //            var payload = await ValidateGoogleToken(model.Token);
        //            if (!payload.EmailVerified)
        //            {
        //                _response.AddError(localizer["Email not verified."]);
        //                return BadRequest(_response);
        //            }
        //            var existedUser = await userManager.FindByEmailAsync(payload.Email);
        //            if (existedUser == null)
        //            {
        //                ApplicationUser user = new ApplicationUser()
        //                {
        //                    Email = payload.Email,
        //                    SecurityStamp = Guid.NewGuid().ToString(),
        //                    //UserName = payload.Email
        //                };
        //                IdentityResult identityResult = await userManager.CreateAsync(user);
        //                user.EmailConfirmed = true;
        //                vwsDbContext.Save();
        //                CreateUserTaskStatuses(Guid.Parse(user.Id));
        //                if (identityResult.Succeeded)
        //                {
        //                    identityResult = await userManager.AddLoginAsync(user, new UserLoginInfo(model.ProviderName, payload.Subject, model.ProviderName));
        //                    if (identityResult.Succeeded)
        //                    {
        //                        await signInManager.SignInAsync(user, false);
        //                        await CreateUserProfile(Guid.Parse(user.Id));
        //                        return Ok(new ResponseModel<JwtTokenModel>(GenerateJWT(user).Result));
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                return Ok(new ResponseModel<JwtTokenModel>(GenerateJWT(existedUser).Result));
        //            }
        //            break;
        //        default:
        //            return BadRequest();
        //    }
        //    return Forbid();
        //}

        //[HttpPost]
        //[Route("login")]
        //public async Task<IActionResult> Login([FromBody] LoginModel model)
        //{
        //    bool rememberMe = true;
        //    if (model.RememberMe.HasValue)
        //        rememberMe = model.RememberMe.Value;

        //    bool isEmail = false;
        //    if (model.UsernameOrEmail.Contains("@"))
        //        isEmail = true;

        //    var user = isEmail ? await userManager.FindByEmailAsync(model.UsernameOrEmail) :
        //        await userManager.FindByNameAsync(model.UsernameOrEmail);

        //    if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
        //    {
        //        if (user.EmailConfirmed == false)
        //        {
        //            var _response = new ResponseModel<string>
        //            {
        //                Message = "User login failed."
        //            };
        //            _response.AddError(localizer["Email is not confirmed yet."]);
        //            _response.Value = user.Email;
        //            return StatusCode(StatusCodes.Status401Unauthorized, _response);
        //        }

        //        return Ok(new ResponseModel<LoginResponseModel>(GenerateJWT(user).Result));
        //    }
        //    var response = new ResponseModel
        //    {
        //        Message = "User login failed."
        //    };
        //    response.AddError(localizer["Password or Username is wrong."]);
        //    return StatusCode(StatusCodes.Status401Unauthorized, response);
        //}

        [HttpPost]
        [Route("confirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ValidationModel model)
        {
            List<string> errors = new List<string>();

            if (!emailChecker.IsValid(model.Email))
            {
                errors.Add(localizer["Email is invalid."]);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseModel { Message = "Invalid Email.", Errors = errors });
            }

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                errors.Add(localizer["User does not exist."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "User does not exist!", Errors = errors });
            }

            var timeDiff = user.EmailVerificationSendTime - DateTime.Now;

            if (user.EmailConfirmed)
            {
                errors.Add(localizer["Email already confirmed."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Email already confirmed!", Errors = errors });
            }

            if (user.EmailVerificationCode == model.ValidationCode &&
                timeDiff.TotalMinutes <= Int16.Parse(configuration["EmailCode:ValidDurationTimeInMinutes"]))
            {
                user.EmailConfirmed = true;
                var result = await userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    CreateUserTaskStatuses(Guid.Parse(user.Id));
                    return Ok(new ResponseModel { Message = "Email confirmed successfully!" });
                }
            }

            errors.Add(localizer["Emil confirmation failed."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Email confirmation failed!", Errors = errors });
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
                    new ResponseModel { Message = "Invalid Email.", Errors = errors });
            }

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                errors.Add(localizer["User does not exist."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "User does not exist!", Errors = errors });
            }

            if (user.EmailConfirmed)
            {
                errors.Add(localizer["Email already confirmed."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Email already confirmed!", Errors = errors });
            }

            if (user.EmailVerificationSendTime != null)
            {
                var timeDiff = DateTime.Now - user.EmailVerificationSendTime;
                if (timeDiff.TotalDays < 365 && timeDiff.TotalMinutes < Int16.Parse(configuration["EmailCode:EmailTimeDifferenceInMinutes"]))
                {
                    errors.Add(localizer["Too many requests."]);
                    return StatusCode(StatusCodes.Status400BadRequest, new ResponseModel { Message = "Too Many Requests!", Errors = errors });
                }
            }

            var randomCode = new string(Enumerable.Repeat(configuration["EmailCode:CodeCharSet"], Int16.Parse(configuration["EmailCode:SizeOfCode"])).Select(s => s[random.Next(s.Length)]).ToArray());

            user.EmailVerificationSendTime = DateTime.Now;
            user.EmailVerificationCode = randomCode;
            var result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                string emailErrorMessage;
                SendEmailModel emailModel = new SendEmailModel
                {
                    FromEmail = configuration["EmailSender:RegistrationEmail:EmailAddress"],
                    ToEmail = user.Email,
                    Subject = "Email Confirmation",
                    Body = EmailTemplateUtility.GetEmailTemplate((int)EmailTemplateEnum.EmailVerificationCode).Replace("{0}", randomCode),
                    Credential = new NetworkCredential
                    {
                        UserName = configuration["EmailSender:RegistrationEmail:UserName"],
                        Password = configuration["EmailSender:RegistrationEmail:Password"]
                    },
                    IsBodyHtml = true
                };
                await emailSender.SendEmailAsync(emailModel, out emailErrorMessage);
                if (string.IsNullOrEmpty(emailErrorMessage))
                    return Ok(new ResponseModel { Message = "Email sent successfully!" });
                errors.Add(localizer[emailErrorMessage]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Sending email failed!", Errors = errors });
            }
            errors.Add(localizer["Problem happened in sending email."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Sending email failed!", Errors = errors });
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
                    new ResponseModel { Message = "Invalid Email.", Errors = errors });
            }

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                errors.Add(localizer["User does not exist."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "User does not exist!", Errors = errors });
            }

            if (user.ResetPasswordSendTime != null)
            {
                var timeDiff = DateTime.Now - user.ResetPasswordSendTime;
                if (timeDiff.TotalDays < 365 && timeDiff.TotalMinutes < Int16.Parse(configuration["EmailCode:EmailTimeDifferenceInMinutes"]))
                {
                    errors.Add(localizer["Too many requests."]);
                    return StatusCode(StatusCodes.Status400BadRequest, new ResponseModel { Message = "Too Many Requests!", Errors = errors });
                }
            }

            var randomCode = new string(Enumerable.Repeat(configuration["EmailCode:CodeCharSet"], Int16.Parse(configuration["EmailCode:SizeOfCode"])).Select(s => s[random.Next(s.Length)]).ToArray());

            user.ResetPasswordSendTime = DateTime.Now;
            user.ResetPasswordCode = randomCode;
            user.ResetPasswordCodeIsValid = true;
            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                string emailErrorMessage;
                SendEmailModel emailModel = new SendEmailModel
                {
                    FromEmail = configuration["EmailSender:RegistrationEmail:EmailAddress"],
                    ToEmail = user.Email,
                    Subject = "Reset Password",
                    Body = randomCode,
                    Credential = new NetworkCredential { UserName = configuration["EmailSender:RegistrationEmail:UserName"], Password = configuration["EmailSender:RegistrationEmail:Password"] },
                    IsBodyHtml = true
                };
                await emailSender.SendEmailAsync(emailModel, out emailErrorMessage);
                if (string.IsNullOrEmpty(emailErrorMessage))
                    return Ok(new ResponseModel { Message = "Email sent successfully!" });
                errors.Add(localizer[emailErrorMessage]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Sending email failed!", Errors = errors });
            }
            errors.Add(localizer["Problem happened in sending email."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Sending email failed!", Errors = errors });
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
                    new ResponseModel { Message = "Invalid Email.", Errors = errors });
            }

            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                errors.Add(localizer["User does not exist."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "User does not exist!", Errors = errors });
            }

            var timeDiff = user.ResetPasswordSendTime - DateTime.Now;

            if (!user.ResetPasswordCodeIsValid)
            {
                errors.Add(localizer["Request for reset password is not valid. Request for reset password again."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Reset password is not valid!", Errors = errors });
            }

            if (user.ResetPasswordCode == model.ValidationCode &&
                timeDiff.TotalMinutes <= Int16.Parse(configuration["EmailCode:ValidDurationTimeInMinutes"]))
            {
                var passwordValidator = new PasswordValidator<ApplicationUser>();
                var result = await passwordValidator.ValidateAsync(userManager, user, model.NewPassword);
                if (result.Succeeded)
                {
                    user.PasswordHash = passwordHasher.HashPassword(user, model.NewPassword);
                    user.ResetPasswordCodeIsValid = false;
                    var res = await userManager.UpdateAsync(user);
                    if (res.Succeeded == true)
                        return Ok(new ResponseModel { Message = "Password changed successfully!" });
                    errors.Add("Reseting the password was unsuccessful.");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Password changing failed!", Errors = errors });
                }
                foreach (var error in result.Errors)
                {
                    errors.Add(localizer[error.Description]);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "New password is not valid!", Errors = errors });
            }
            errors.Add(localizer["Reset password failed. Code is invalid or code validation time is paased."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Password changing failed!", Errors = errors });
        }

        //[HttpPost]
        //[Route("isUserNameInUse")]
        //public async Task<bool> IsUserNameInUse(string username)
        //{
        //    var user = await userManager.FindByNameAsync(username);
        //    if (user == null) return false;
        //    return true;
        //}

        //[HttpPost]
        //[Route("isEmailInUse")]
        //public async Task<bool> IsEmailInUse(string email)
        //{
        //    var user = await userManager.FindByEmailAsync(email);
        //    if (user == null) return false;
        //    if (user.EmailConfirmed)
        //        return true;
        //    return false;
        //}

        //[HttpGet]
        //[Authorize]
        //[Route("getUserEmail")]
        //public string GetUserEmail()
        //{
        //    return User.Claims.First(claim => claim.Type == "UserEmail").Value;
        //}

        [HttpGet]
        [Route("getEmailTimeWait")]
        public int GetEmailTimeWait()
        {
            return Int16.Parse(configuration["EmailCode:EmailTimeDifferenceInMinutes"]);
        }

        [HttpPost]
        [Route("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenModel model)
        {
            var response = new ResponseModel<LoginResponseModel>();
            var principal = GetPrincipalFromExpiredToken(model.Token);
            Guid userId = new Guid(principal.Claims.First(claim => claim.Type == "UserId").Value);
            var varRefreshToken = await vwsDbContext.GetRefreshTokenAsync(userId, model.RefreshToken);

            if (varRefreshToken == null || varRefreshToken.IsValid == false)
            {
                response.Message = "Invalid refresh token";
                response.AddError(localizer["Refresh token is invalid."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            vwsDbContext.MakeRefreshTokenInvalid(model.RefreshToken);
            var newRefreshToken = new RefreshToken
            {
                IsValid = true,
                Token = GenerateRefreshToken(),
                UserId = userId
            };
            await vwsDbContext.AddRefreshTokenAsync(newRefreshToken);
            vwsDbContext.Save();

            var newJWToken = GenerateToken(principal.Claims);

            return Ok(new ResponseModel<LoginResponseModel>(new LoginResponseModel
            {
                Token = new JwtSecurityTokenHandler().WriteToken(newJWToken),
                RefreshToken = newRefreshToken.Token,
                ValidTo = newJWToken.ValidTo
            }));
        }

        [HttpPost]
        [Authorize]
        [Route("uploadProfileImage")]
        public async Task<IActionResult> UploadProfileImage(IFormFile image)
        {
            var response = new ResponseModel<Guid>();

            string[] types = { "png", "jpg", "jpeg" };

            var files = Request.Form.Files.ToList();

            Guid userId = LoggedInUserId.Value;
            var userProfile = await vwsDbContext.GetUserProfileAsync(userId);

            if (files.Count > 1)
            {
                response.AddError(localizer["There is more than one file."]);
                response.Message = "Too many files passed";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (files.Count == 0 && image == null)
            {
                response.AddError(localizer["You did not upload an image."]);
                response.Message = "There is no image";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            var uploadedImage = files.Count == 0 ? image : files[0];

            ResponseModel<File> fileResponse;

            if (userProfile.ProfileImage != null)
            {
                fileResponse = await fileManager.WriteFile(uploadedImage, userId, "profileImages", (int)userProfile.ProfileImageId, types.ToList());
                if (fileResponse.HasError)
                {
                    foreach (var error in fileResponse.Errors)
                        response.AddError(localizer[error]);
                    response.Message = "Error in writing file";
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
                userProfile.ProfileImage.RecentFileId = fileResponse.Value.Id;
                vwsDbContext.Save();
            }
            else
            {
                var time = DateTime.Now;
                var newFileContainer = new FileContainer
                {
                    ModifiedOn = time,
                    CreatedOn = time,
                    CreatedBy = userId,
                    ModifiedBy = userId,
                    Guid = Guid.NewGuid()
                };
                await vwsDbContext.AddFileContainerAsync(newFileContainer);
                vwsDbContext.Save();
                fileResponse = await fileManager.WriteFile(uploadedImage, userId, "profileImages", newFileContainer.Id, types.ToList());
                if (fileResponse.HasError)
                {
                    foreach (var error in fileResponse.Errors)
                        response.AddError(localizer[error]);
                    response.Message = "Error in writing file";
                    vwsDbContext.DeleteFileContainer(newFileContainer);
                    vwsDbContext.Save();
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
                newFileContainer.RecentFileId = fileResponse.Value.Id;
                userProfile.ProfileImageId = newFileContainer.Id;
                userProfile.ProfileImageGuid = newFileContainer.Guid;
                vwsDbContext.Save();
            }

            response.Value = fileResponse.Value.FileContainerGuid;
            response.Message = "User image added successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("changePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var errors = new List<string>();
            string userId = LoggedInUserId.Value.ToString();
            var user = await userManager.FindByIdAsync(userId);

            if (await userManager.CheckPasswordAsync(user, model.LastPassword))
            {
                var passwordValidator = new PasswordValidator<ApplicationUser>();
                var result = await passwordValidator.ValidateAsync(userManager, user, model.NewPassword);
                if (result.Succeeded)
                {
                    user.PasswordHash = passwordHasher.HashPassword(user, model.NewPassword);
                    var res = await userManager.UpdateAsync(user);
                    if (res.Succeeded == true)
                    {
                        string emailError;
                        SendEmailModel emailModel = new SendEmailModel
                        {
                            FromEmail = configuration["EmailSender:RegistrationEmail:EmailAddress"],
                            ToEmail = user.Email,
                            Subject = "Change Password Alert",
                            Body = "Email Changed",
                            Credential = new NetworkCredential { UserName = configuration["EmailSender:RegistrationEmail:UserName"], Password = configuration["EmailSender:RegistrationEmail:Password"] },
                            IsBodyHtml = true
                        };
                        await emailSender.SendEmailAsync(emailModel, out emailError);
                        return Ok(new ResponseModel { Message = "Password changed successfully!" });
                    }
                    errors.Add("Changing the password was unsuccessful.");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Password changing failed!", Errors = errors });
                }
                foreach (var error in result.Errors)
                {
                    errors.Add(localizer[error.Description]);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "New password is not valid!", Errors = errors });
            }

            errors.Add(localizer["Last password is not true."]);
            return StatusCode(StatusCodes.Status401Unauthorized, new ResponseModel { Message = "Unauthorized", Errors = errors });
        }

        [HttpPost]
        [Authorize]
        [Route("logOut")]
        public async Task<IActionResult> LogOut([FromBody] TokenModel model)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();

            var refreshToken = await vwsDbContext.GetRefreshTokenAsync(userId, model.Token);

            if (refreshToken == null || refreshToken.IsValid == false)
            {
                response.Message = "Invalid refresh token";
                response.AddError(localizer["Refresh token is invalid."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            refreshToken.IsValid = false;
            vwsDbContext.Save();

            response.Message = "Logged out successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getCulture")]
        public Object GetCulture()
        {
            var userId = LoggedInUserId.Value;

            var userProfile = vwsDbContext.UserProfiles.Include(profile => profile.Culture).FirstOrDefault(profile => profile.UserId == userId);

            return userProfile.CultureId == null ? new { CultureAbbriviation = SeedDataEnum.Cultures.en_US.ToString().Replace('_', '-') } : new { CultureAbbriviation = userProfile.Culture.CultureAbbreviation };
        }

        [HttpPost]
        [Authorize]
        [Route("setCulture")]
        public IActionResult SetCulture(byte cultureId)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();

            if (cultureId <= 0 || cultureId > 10)
            {
                response.AddError(localizer["There is no culture with given Id."]);
                response.Message = "Culture not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var userProfile = vwsDbContext.UserProfiles.FirstOrDefault(profile => profile.UserId == userId);
            userProfile.CultureId = cultureId;
            vwsDbContext.Save();

            response.Message = "Culture set successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getUserProfileImage")]
        public async Task<Guid?> GetUserProfileImage()
        {
            var userId = LoggedInUserId.Value;

            var selectedProfile = await vwsDbContext.GetUserProfileAsync(userId);
            return selectedProfile.ProfileImageGuid;
        }
    }
}
