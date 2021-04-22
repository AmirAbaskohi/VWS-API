using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
        #region Feilds
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        private readonly IStringLocalizer<AccountController> _localizer;
        private readonly EmailAddressAttribute _emailChecker;
        private readonly Random _random;
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IFileManager _fileManager;
        #endregion

        #region Ctor
        public AccountController(UserManager<ApplicationUser> userManager, IConfiguration configuration, IEmailSender emailSender,
            IPasswordHasher<ApplicationUser> passwordHasher, IStringLocalizer<AccountController> localizer,
            IVWS_DbContext vwsDbContext, IFileManager fileManager)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _passwordHasher = passwordHasher;
            _localizer = localizer;
            _emailChecker = new EmailAddressAttribute();
            _random = new Random();
            _vwsDbContext = vwsDbContext;
            _fileManager = fileManager;
        }
        #endregion

        #region Private Methods

        private JwtSecurityToken GenerateToken(IEnumerable<Claim> claims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                expires: DateTime.Now.AddMinutes(Int16.Parse(_configuration["JWT:ValidTimeInMinutes"])),
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

        private async Task<JwtTokenModel> GenerateJWT(IdentityUser user, string nickName, Guid? userProfileImageId)
        {
            var authClaims = new List<Claim>
                {
                    new Claim("UserEmail", user.Email),
                    new Claim("NickName", nickName),
                    new Claim("UserId", user.Id),
                    new Claim("UserProfileImageId", userProfileImageId.HasValue ? userProfileImageId.Value.ToString(): string.Empty),
                };

            var token = GenerateToken(authClaims);

            var refreshToken = new RefreshToken()
            {
                IsValid = true,
                Token = GenerateRefreshToken(),
                UserId = new Guid(user.Id)
            };
            await _vwsDbContext.AddRefreshTokenAsync(refreshToken);
            _vwsDbContext.Save();

            return new JwtTokenModel
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken.Token,
                ValidTo = token.ValidTo
            };
        }

        private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleToken(string googleTokenId)
        {
            GoogleJsonWebSignature.ValidationSettings settings = new GoogleJsonWebSignature.ValidationSettings();
            settings.Audience = new List<string>() { _configuration["Google:ClientId"] };
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"])),
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

        private async Task<UserProfile> CreateUserProfile(Guid userId)
        {
            UserProfile userProfile = new UserProfile()
            {
                UserId = userId,
                ThemeColorCode = "",
                NickNameSecurityStamp = Guid.NewGuid(),
                ProfileImageSecurityStamp = Guid.NewGuid()
            };
            var createdUserProfile = await _vwsDbContext.AddUserProfileAsync(userProfile);
            _vwsDbContext.Save();
            return createdUserProfile;
        }

        private void CreateUserTaskStatuses(Guid userId)
        {
            _vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 2, ProjectId = null, UserProfileId = userId, TeamId = null, Title = "To Do" });
            _vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 4, ProjectId = null, UserProfileId = userId, TeamId = null, Title = "Doing" });
            _vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 6, ProjectId = null, UserProfileId = userId, TeamId = null, Title = "Done" });

            _vwsDbContext.Save();
        }

        private async Task<ResponseModel<Guid>> UploadProfileImage(Guid userId, IFormFile image, IFormFileCollection formFiles)
        {
            ResponseModel<Guid> response = new ResponseModel<Guid>();
            string[] types = { "png", "jpg", "jpeg" };
            if (formFiles.Count > 1)
            {
                response.AddError(_localizer["There is more than one file."]);
                response.Message = "Too many files passed";
                return response;
            }
            if (formFiles.Count == 0 && image == null)
            {
                response.AddError(_localizer["You did not upload an image."]);
                response.Message = "There is no image";
                return response;
            }
            var uploadedImage = formFiles.Count == 0 ? image : formFiles[0];

            ResponseModel<File> fileResponse;
            UserProfile userProfile = await _vwsDbContext.GetUserProfileAsync(userId);
            if (userProfile.ProfileImage != null)
            {
                fileResponse = await _fileManager.WriteFile(uploadedImage, userId, "profileImages", (int)userProfile.ProfileImageId, types.ToList());
                if (fileResponse.HasError)
                {
                    foreach (var error in fileResponse.Errors)
                        response.AddError(_localizer[error]);
                    response.Message = "Error in writing file";
                    return response;
                }
                userProfile.ProfileImage.RecentFileId = fileResponse.Value.Id;
                userProfile.ProfileImageSecurityStamp = Guid.NewGuid();
                _vwsDbContext.Save();
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
                await _vwsDbContext.AddFileContainerAsync(newFileContainer);
                _vwsDbContext.Save();
                fileResponse = await _fileManager.WriteFile(uploadedImage, userId, "profileImages", newFileContainer.Id, types.ToList());
                if (fileResponse.HasError)
                {
                    foreach (var error in fileResponse.Errors)
                        response.AddError(_localizer[error]);
                    response.Message = "Error in writing file";
                    _vwsDbContext.DeleteFileContainer(newFileContainer);
                    _vwsDbContext.Save();
                    return response;
                }
                newFileContainer.RecentFileId = fileResponse.Value.Id;
                userProfile.ProfileImageId = newFileContainer.Id;
                userProfile.ProfileImageGuid = newFileContainer.Guid;
                userProfile.ProfileImageSecurityStamp = Guid.NewGuid();
                _vwsDbContext.Save();
            }
            response.Value = fileResponse.Value.FileContainerGuid;
            response.Message = "User image added successfully!";
            return response;
        }

        #endregion

        #region LoginRegisterAPIS
        [HttpPost]
        [Route("loginRegister")]
        public async Task<IActionResult> LoginRegister([FromBody] LoginRegisterModel model)
        {
            ResponseModel<LoginRegisterResponseModel> responseModel = new ResponseModel<LoginRegisterResponseModel>();
            responseModel.Value = new LoginRegisterResponseModel();

            if (!_emailChecker.IsValid(model.Email))
            {
                responseModel.AddError(_localizer["Email is invalid."]);
                responseModel.Message = "Invalid Email.";
                return StatusCode(StatusCodes.Status400BadRequest, responseModel);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                responseModel.Value.EmailConfirmed = user.EmailConfirmed;

                if (await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    var userProfile = await _vwsDbContext.GetUserProfileAsync(new Guid(user.Id));
                    if (responseModel.Value.EmailConfirmed)
                    {
                        if (string.IsNullOrWhiteSpace(userProfile.NickName))
                        {
                            responseModel.Value.HasNickName = false;
                            responseModel.Value.NickNameSecurityStamp = userProfile.NickNameSecurityStamp;
                            responseModel.Value.ProfileImageSecurityStamp = userProfile.ProfileImageSecurityStamp;
                            responseModel.Message = "User has not nick-name.";
                            return Ok(responseModel);
                        }
                        else
                        {
                            responseModel.Value.HasNickName = true;
                            responseModel.Value.JwtToken = await GenerateJWT(user, userProfile.NickName, userProfile.ProfileImageGuid);
                            responseModel.Message = "Logged in successfully!";
                            return Ok(responseModel);
                        }
                    }
                    else
                    {
                        responseModel.Value.HasNickName = false;
                        responseModel.Value.NickNameSecurityStamp = userProfile.NickNameSecurityStamp;
                        responseModel.Value.ProfileImageSecurityStamp = userProfile.ProfileImageSecurityStamp;
                        responseModel.Message = "Email is not confirmed";
                        return Ok(responseModel);
                    }
                }
                else
                {
                    responseModel.Message = "User login failed.";
                    responseModel.AddError(_localizer["Email or password is wrong."]);
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
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = model.Email
                };
                var result = await _userManager.CreateAsync(applicationUser, model.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        responseModel.AddError(_localizer[error.Description]);
                    responseModel.Message = "User creation failed!";
                    return StatusCode(StatusCodes.Status500InternalServerError, responseModel);
                }
                #endregion
                try
                {
                    var createdUserProfile = await CreateUserProfile(Guid.Parse(applicationUser.Id));
                    responseModel.Value.NickNameSecurityStamp = createdUserProfile.NickNameSecurityStamp;
                    responseModel.Value.ProfileImageSecurityStamp = createdUserProfile.ProfileImageSecurityStamp;
                }
                catch
                {
                    await _userManager.DeleteAsync(applicationUser);
                    responseModel.AddError(_localizer["User creation failed."]);
                    responseModel.Message = "User creation failed!";
                    return StatusCode(StatusCodes.Status500InternalServerError, responseModel);
                }
                responseModel.Message = "User created successfully!";
                return Ok(responseModel);
            }
        }

        [HttpPost]
        [Route("sso")]
        public async Task<IActionResult> SSO([FromBody] ExternalLoginModel model)
        {
            ResponseModel<LoginRegisterResponseModel> responseModel = new ResponseModel<LoginRegisterResponseModel>();
            responseModel.Value = new LoginRegisterResponseModel();
            switch (model.ProviderName)
            {
                case "Google":
                    var payload = await ValidateGoogleToken(model.Token);
                    if (!payload.EmailVerified)
                    {
                        responseModel.AddError(_localizer["Email not verified."]);
                        return BadRequest(responseModel);
                    }
                    var existedUser = await _userManager.FindByEmailAsync(payload.Email);
                    if (existedUser == null)
                    {
                        ApplicationUser user = new ApplicationUser()
                        {
                            Email = payload.Email,
                            SecurityStamp = Guid.NewGuid().ToString(),
                            UserName = payload.Email
                        };
                        IdentityResult identityResult = await _userManager.CreateAsync(user);
                        user.EmailConfirmed = true;
                        responseModel.Value.EmailConfirmed = user.EmailConfirmed;
                        _vwsDbContext.Save();
                        if (identityResult.Succeeded)
                        {
                            identityResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(model.ProviderName, payload.Subject, model.ProviderName));
                            if (identityResult.Succeeded)
                            {
                                try
                                {
                                    UserProfile createdUserProfile = await CreateUserProfile(Guid.Parse(user.Id));
                                    CreateUserTaskStatuses(Guid.Parse(user.Id));
                                    responseModel.Value.NickNameSecurityStamp = createdUserProfile.NickNameSecurityStamp;
                                    responseModel.Value.ProfileImageSecurityStamp = createdUserProfile.ProfileImageSecurityStamp;
                                }
                                catch
                                {
                                    await _userManager.DeleteAsync(existedUser);
                                    responseModel.AddError(_localizer["User creation failed."]);
                                    responseModel.Message = "User creation failed!";
                                    return StatusCode(StatusCodes.Status500InternalServerError, responseModel);
                                }
                                responseModel.Message = "User created successfully!";
                                return Ok(responseModel);
                            }
                        }
                        else
                        {
                            responseModel.AddError(_localizer["User creation failed."]);
                            responseModel.Message = "User creation failed!";
                            return StatusCode(StatusCodes.Status500InternalServerError, responseModel);
                        }
                    }
                    else
                    {
                        responseModel.Value.EmailConfirmed = true;
                        UserProfile userProfile = await _vwsDbContext.GetUserProfileAsync(Guid.Parse(existedUser.Id));
                        if (string.IsNullOrWhiteSpace(userProfile.NickName))
                        {
                            responseModel.Value.HasNickName = false;
                            responseModel.Value.NickNameSecurityStamp = userProfile.NickNameSecurityStamp;
                            responseModel.Value.ProfileImageSecurityStamp = userProfile.ProfileImageSecurityStamp;
                            responseModel.Message = "User has not nick-name.";
                            return Ok(responseModel);
                        }
                        else
                        {
                            responseModel.Value.HasNickName = true;
                            responseModel.Value.JwtToken = await GenerateJWT(existedUser, userProfile.NickName, userProfile.ProfileImageGuid);
                            responseModel.Message = "Logged in successfully!";
                            return Ok(responseModel);
                        }
                    }
                    break;
                default:
                    return BadRequest();
            }
            return Forbid();
        }

        [HttpPost]
        [Route("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenModel model)
        {
            var response = new ResponseModel<JwtTokenModel>();
            var principal = GetPrincipalFromExpiredToken(model.Token);
            Guid userId = new Guid(principal.Claims.First(claim => claim.Type == "UserId").Value);
            var varRefreshToken = await _vwsDbContext.GetRefreshTokenAsync(userId, model.RefreshToken);

            if (varRefreshToken == null || varRefreshToken.IsValid == false)
            {
                response.Message = "Invalid refresh token";
                response.AddError(_localizer["Refresh token is invalid."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            _vwsDbContext.MakeRefreshTokenInvalid(model.RefreshToken);
            var newRefreshToken = new RefreshToken
            {
                IsValid = true,
                Token = GenerateRefreshToken(),
                UserId = userId
            };
            await _vwsDbContext.AddRefreshTokenAsync(newRefreshToken);
            _vwsDbContext.Save();

            var newJWToken = GenerateToken(principal.Claims);

            return Ok(new ResponseModel<JwtTokenModel>(new JwtTokenModel
            {
                Token = new JwtSecurityTokenHandler().WriteToken(newJWToken),
                RefreshToken = newRefreshToken.Token,
                ValidTo = newJWToken.ValidTo
            }));
        }

        [HttpPost]
        [Authorize]
        [Route("logOut")]
        public async Task<IActionResult> LogOut([FromBody] TokenModel model)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();

            var refreshToken = await _vwsDbContext.GetRefreshTokenAsync(userId, model.Token);

            if (refreshToken == null || refreshToken.IsValid == false)
            {
                response.Message = "Invalid refresh token";
                response.AddError(_localizer["Refresh token is invalid."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            refreshToken.IsValid = false;
            _vwsDbContext.Save();

            response.Message = "Logged out successfully!";
            return Ok(response);
        }

        #endregion

        #region ConfirmEmailAPIS
        [HttpPost]
        [Route("sendConfirmEmail")]
        public async Task<IActionResult> SendConfirmEmail([FromBody] EmailModel model)
        {
            List<string> errors = new List<string>();

            if (!_emailChecker.IsValid(model.Email))
            {
                errors.Add(_localizer["Email is invalid."]);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseModel { Message = "Invalid Email.", Errors = errors });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                errors.Add(_localizer["User does not exist."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "User does not exist!", Errors = errors });
            }

            if (user.EmailConfirmed)
            {
                errors.Add(_localizer["Email already confirmed."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Email already confirmed!", Errors = errors });
            }

            if (user.EmailVerificationSendTime != null)
            {
                var timeDiff = DateTime.Now - user.EmailVerificationSendTime;
                if (timeDiff.TotalDays < 365 && timeDiff.TotalMinutes < Int16.Parse(_configuration["EmailCode:EmailTimeDifferenceInMinutes"]))
                {
                    errors.Add(_localizer["Too many requests."]);
                    return StatusCode(StatusCodes.Status400BadRequest, new ResponseModel { Message = "Too Many Requests!", Errors = errors });
                }
            }

            var randomCode = new string(Enumerable.Repeat(_configuration["EmailCode:CodeCharSet"], Int16.Parse(_configuration["EmailCode:SizeOfCode"])).Select(s => s[_random.Next(s.Length)]).ToArray());

            user.EmailVerificationSendTime = DateTime.Now;
            user.EmailVerificationCode = randomCode;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                string emailErrorMessage;
                SendEmailModel emailModel = new SendEmailModel
                {
                    FromEmail = _configuration["EmailSender:RegistrationEmail:EmailAddress"],
                    ToEmail = user.Email,
                    Subject = "Email Confirmation",
                    Body = EmailTemplateUtility.GetEmailTemplate((int)EmailTemplateEnum.EmailCode).Replace("{0}", randomCode),
                    Credential = new NetworkCredential
                    {
                        UserName = _configuration["EmailSender:RegistrationEmail:UserName"],
                        Password = _configuration["EmailSender:RegistrationEmail:Password"]
                    },
                    IsBodyHtml = true
                };
                await _emailSender.SendEmailAsync(emailModel, out emailErrorMessage);
                if (string.IsNullOrEmpty(emailErrorMessage))
                    return Ok(new ResponseModel { Message = "Email sent successfully!" });
                errors.Add(_localizer[emailErrorMessage]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Sending email failed!", Errors = errors });
            }
            errors.Add(_localizer["Problem happened in sending email."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Sending email failed!", Errors = errors });
        }

        [HttpPost]
        [Route("confirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ValidationModel model)
        {
            List<string> errors = new List<string>();

            if (!_emailChecker.IsValid(model.Email))
            {
                errors.Add(_localizer["Email is invalid."]);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseModel { Message = "Invalid Email.", Errors = errors });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                errors.Add(_localizer["User does not exist."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "User does not exist!", Errors = errors });
            }

            var timeDiff = user.EmailVerificationSendTime - DateTime.Now;

            if (user.EmailConfirmed)
            {
                errors.Add(_localizer["Email already confirmed."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Email already confirmed!", Errors = errors });
            }

            if (user.EmailVerificationCode == model.ValidationCode &&
                timeDiff.TotalMinutes <= Int16.Parse(_configuration["EmailCode:ValidDurationTimeInMinutes"]))
            {
                user.EmailConfirmed = true;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    CreateUserTaskStatuses(Guid.Parse(user.Id));
                    return Ok(new ResponseModel { Message = "Email confirmed successfully!" });
                }
            }

            errors.Add(_localizer["Emil confirmation failed."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Email confirmation failed!", Errors = errors });
        }
        #endregion

        #region UserProfileAPIS
        [HttpPost]
        [Route("sendResetPassEmail")]
        public async Task<IActionResult> SendResetPassEmail([FromBody] EmailModel model)
        {
            List<string> errors = new List<string>();

            if (!_emailChecker.IsValid(model.Email))
            {
                errors.Add(_localizer["Email is invalid."]);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseModel { Message = "Invalid Email.", Errors = errors });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                errors.Add(_localizer["User does not exist."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "User does not exist!", Errors = errors });
            }

            if (user.ResetPasswordSendTime != null)
            {
                var timeDiff = DateTime.Now - user.ResetPasswordSendTime;
                if (timeDiff.TotalDays < 365 && timeDiff.TotalMinutes < Int16.Parse(_configuration["EmailCode:EmailTimeDifferenceInMinutes"]))
                {
                    errors.Add(_localizer["Too many requests."]);
                    return StatusCode(StatusCodes.Status400BadRequest, new ResponseModel { Message = "Too Many Requests!", Errors = errors });
                }
            }

            var randomCode = new string(Enumerable.Repeat(_configuration["EmailCode:CodeCharSet"], Int16.Parse(_configuration["EmailCode:SizeOfCode"])).Select(s => s[_random.Next(s.Length)]).ToArray());

            user.ResetPasswordSendTime = DateTime.Now;
            user.ResetPasswordCode = randomCode;
            user.ResetPasswordCodeIsValid = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                string emailErrorMessage;
                SendEmailModel emailModel = new SendEmailModel
                {
                    FromEmail = _configuration["EmailSender:RegistrationEmail:EmailAddress"],
                    ToEmail = user.Email,
                    Subject = "Reset Password",
                    Body = randomCode,
                    Credential = new NetworkCredential { UserName = _configuration["EmailSender:RegistrationEmail:UserName"], Password = _configuration["EmailSender:RegistrationEmail:Password"] },
                    IsBodyHtml = true
                };
                await _emailSender.SendEmailAsync(emailModel, out emailErrorMessage);
                if (string.IsNullOrEmpty(emailErrorMessage))
                    return Ok(new ResponseModel { Message = "Email sent successfully!" });
                errors.Add(_localizer[emailErrorMessage]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Sending email failed!", Errors = errors });
            }
            errors.Add(_localizer["Problem happened in sending email."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Sending email failed!", Errors = errors });
        }

        [HttpPut]
        [Route("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            List<string> errors = new List<string>();

            if (!_emailChecker.IsValid(model.Email))
            {
                errors.Add(_localizer["Email is invalid."]);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseModel { Message = "Invalid Email.", Errors = errors });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                errors.Add(_localizer["User does not exist."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "User does not exist!", Errors = errors });
            }

            var timeDiff = user.ResetPasswordSendTime - DateTime.Now;

            if (!user.ResetPasswordCodeIsValid)
            {
                errors.Add(_localizer["Request for reset password is not valid. Request for reset password again."]);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Reset password is not valid!", Errors = errors });
            }

            if (user.ResetPasswordCode == model.ValidationCode &&
                timeDiff.TotalMinutes <= Int16.Parse(_configuration["EmailCode:ValidDurationTimeInMinutes"]))
            {
                var passwordValidator = new PasswordValidator<ApplicationUser>();
                var result = await passwordValidator.ValidateAsync(_userManager, user, model.NewPassword);
                if (result.Succeeded)
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
                    user.ResetPasswordCodeIsValid = false;
                    var res = await _userManager.UpdateAsync(user);
                    if (res.Succeeded == true)
                        return Ok(new ResponseModel { Message = "Password changed successfully!" });
                    errors.Add("Reseting the password was unsuccessful.");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Password changing failed!", Errors = errors });
                }
                foreach (var error in result.Errors)
                {
                    errors.Add(_localizer[error.Description]);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "New password is not valid!", Errors = errors });
            }
            errors.Add(_localizer["Reset password failed. Code is invalid or code validation time is paased."]);
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Password changing failed!", Errors = errors });
        }

        /// <summary>
        /// If user is authorized, he can upload his image by bearer header content;
        /// else if user is not authorized (eg: while onboarding),
        /// he can upload his image by passing {email} and {securityStamp} in Request.Form
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("uploadProfileImage")]
        public async Task<IActionResult> UploadProfileImage(IFormFile image)
        {
            if (LoggedInUserId.HasValue)
                return Ok(await UploadProfileImage(LoggedInUserId.Value, image, Request.Form.Files));
            else
            {
                var response = new ResponseModel<Guid>();
                var email = Request.Form.First(e => e.Key == "email");
                var securityStamp = Request.Form.First(e => e.Key == "securityStamp");

                if (!_emailChecker.IsValid(email.Value.ToString()))
                {
                    response.AddError(_localizer["Email is invalid."]);
                    response.Message = "Invalid Email.";
                    return BadRequest(response);
                }

                var user = await _userManager.FindByEmailAsync(email.Value.ToString());
                if (user == null)
                {
                    response.AddError(_localizer["User does not exist."]);
                    response.Message = "User does not exist.";
                    return BadRequest(response);
                }

                var userProfile = await _vwsDbContext.GetUserProfileAsync(Guid.Parse(user.Id));
                if (userProfile.ProfileImageSecurityStamp != Guid.Parse(securityStamp.Value.ToString()))
                {
                    response.AddError(_localizer["Invalid security stamp."]);
                    response.Message = "Invalid security stamp!";
                    return BadRequest(response);
                }
                else
                {
                    return Ok(await UploadProfileImage(userProfile.UserId, image, Request.Form.Files));
                }
            }
        }

        [HttpPut]
        [Route("setNickName")]
        public async Task<IActionResult> SetNickName([FromBody] NickNameModel model)
        {
            ResponseModel<JwtTokenModel> responseModel = new ResponseModel<JwtTokenModel>();

            if (!_emailChecker.IsValid(model.Email))
            {
                responseModel.AddError(_localizer["Email is invalid."]);
                responseModel.Message = "Invalid Email.";
                return BadRequest(responseModel);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                responseModel.AddError(_localizer["User does not exist."]);
                responseModel.Message = "User does not exist!";
                return BadRequest(responseModel);
            }
            else if (!user.EmailConfirmed)
            {
                responseModel.AddError(_localizer["Email is not confirmed yet."]);
                responseModel.Message = "Email is not confirmed yet!";
                return BadRequest(responseModel);
            }

            var userProfile = await _vwsDbContext.GetUserProfileAsync(Guid.Parse(user.Id));
            if (userProfile.NickNameSecurityStamp != model.NickNameSecurityStamp)
            {
                responseModel.AddError(_localizer["Invalid security stamp."]);
                responseModel.Message = "Invalid security stamp!";
                return BadRequest(responseModel);
            }

            if (string.IsNullOrWhiteSpace(model.NickName) || model.NickName.Length > 100)
            {
                responseModel.AddError(_localizer["Nick-Name can not be over 100 chars and not empty."]);
                responseModel.Message = "Invalid nick-name!";
                return BadRequest(responseModel);
            }

            userProfile.NickName = model.NickName;
            userProfile.NickNameSecurityStamp = Guid.NewGuid();
            _vwsDbContext.Save();

            responseModel.Value = await GenerateJWT(user, model.NickName, userProfile.ProfileImageGuid);
            responseModel.Message = "Logged in successfully!";
            return Ok(responseModel);
        }

        [HttpPut]
        [Authorize]
        [Route("changePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var errors = new List<string>();
            string userId = LoggedInUserId.Value.ToString();
            var user = await _userManager.FindByIdAsync(userId);

            if (await _userManager.CheckPasswordAsync(user, model.LastPassword))
            {
                var passwordValidator = new PasswordValidator<ApplicationUser>();
                var result = await passwordValidator.ValidateAsync(_userManager, user, model.NewPassword);
                if (result.Succeeded)
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
                    var res = await _userManager.UpdateAsync(user);
                    if (res.Succeeded == true)
                    {
                        string emailError;
                        SendEmailModel emailModel = new SendEmailModel
                        {
                            FromEmail = _configuration["EmailSender:RegistrationEmail:EmailAddress"],
                            ToEmail = user.Email,
                            Subject = "Change Password Alert",
                            Body = "Email Changed",
                            Credential = new NetworkCredential { UserName = _configuration["EmailSender:RegistrationEmail:UserName"], Password = _configuration["EmailSender:RegistrationEmail:Password"] },
                            IsBodyHtml = true
                        };
                        await _emailSender.SendEmailAsync(emailModel, out emailError);
                        return Ok(new ResponseModel { Message = "Password changed successfully!" });
                    }
                    errors.Add("Changing the password was unsuccessful.");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "Password changing failed!", Errors = errors });
                }
                foreach (var error in result.Errors)
                {
                    errors.Add(_localizer[error.Description]);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Message = "New password is not valid!", Errors = errors });
            }

            errors.Add(_localizer["Last password is not true."]);
            return StatusCode(StatusCodes.Status401Unauthorized, new ResponseModel { Message = "Unauthorized", Errors = errors });
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
                response.AddError(_localizer["There is no culture with given Id."]);
                response.Message = "Culture not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var userProfile = _vwsDbContext.UserProfiles.FirstOrDefault(profile => profile.UserId == userId);
            userProfile.CultureId = cultureId;
            _vwsDbContext.Save();

            response.Message = "Culture set successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getCulture")]
        public Object GetCulture()
        {
            var userId = LoggedInUserId.Value;

            var userProfile = _vwsDbContext.UserProfiles.Include(profile => profile.Culture).FirstOrDefault(profile => profile.UserId == userId);

            return userProfile.CultureId == null ? new { CultureAbbriviation = SeedDataEnum.Cultures.en_US.ToString().Replace('_', '-') } : new { CultureAbbriviation = userProfile.Culture.CultureAbbreviation };
        }

        [HttpGet]
        [Authorize]
        [Route("getUserProfileImage")]
        public async Task<Guid?> GetUserProfileImage()
        {
            var userId = LoggedInUserId.Value;

            var selectedProfile = await _vwsDbContext.GetUserProfileAsync(userId);
            return selectedProfile.ProfileImageGuid;
        }
        #endregion

        [HttpGet]
        [Route("getEmailTimeWait")]
        public int GetEmailTimeWait()
        {
            return Int16.Parse(_configuration["EmailCode:EmailTimeDifferenceInMinutes"]);
        }
    }
}
