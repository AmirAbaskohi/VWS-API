using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using vws.web.Models;
using vws.web.Repositories;

namespace vws.web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IEmailSender emailSender;
        private readonly IConfiguration configuration;

        public AccountController(UserManager<ApplicationUser> _userManager, RoleManager<IdentityRole> _roleManager,
            SignInManager<ApplicationUser> _signInManager, IConfiguration _configuration, IEmailSender _emailSender)
        {
            userManager = _userManager;
            roleManager = _roleManager;
            signInManager = _signInManager;
            configuration = _configuration;
            emailSender = _emailSender;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var userExists = await userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseModel { Status = "Error", Message = "User already exists!" });

            ApplicationUser user = new ApplicationUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };

            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User creation failed!" });

            await SendConfirmEmail(new UserModel { Email = user.Email });

            return Ok(new ResponseModel { Status = "Success", Message = "User created successfully!" });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (signInManager.IsSignedIn(User))
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User already signed in!" });

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
            return Unauthorized();
        }

        [HttpPost]
        [Route("confirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ValidationModel model)
        {

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User does not exist!" });

            var timeDiff = user.EmailVerificationSendTime - DateTime.Now;

            if (user.EmailConfirmed)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Email already confirmed!" });

            if(user.EmailVerificationCode == model.ValidationCode &&
                timeDiff.TotalMinutes <= Int16.Parse(configuration["EmailCode:ValidDurationTimeInMinutes"]))
            {
                user.EmailConfirmed = true;
                return Ok(new ResponseModel { Status = "Success", Message = "Email confirmed successfully!" });
            }

            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "Email confirmation failed!" });
        }

        [HttpPost]
        [Route("sendConfirmEmail")]
        public async Task<IActionResult> SendConfirmEmail([FromBody] UserModel model)
        {

            var user = await userManager.FindByEmailAsync(model.Email);
            if(user == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User does not exist!" });

            var random = new Random();
            var randomCode = new string(Enumerable.Repeat(configuration["EmailCode:CodeCharSet"], Int16.Parse(configuration["EmailCode:SizeOfCode"])).Select(s => s[random.Next(s.Length)]).ToArray());

            await emailSender.SendEmailAsync(user.Email, "EmailConfirmation", randomCode);

            user.EmailVerificationSendTime = DateTime.Now;
            user.EmailVerificationCode = randomCode;

            return Ok(new ResponseModel { Status = "Success", Message = "Email sent successfully!" });
        }

        [HttpPost]
        [Route("sendResetPassEmail")]
        public async Task<IActionResult> SendResetPassEmail([FromBody] UserModel model)
        {

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User does not exist!" });

            var random = new Random();
            var randomCode = new string(Enumerable.Repeat(configuration["EmailCode:CodeCharSet"], Int16.Parse(configuration["EmailCode:SizeOfCode"])).Select(s => s[random.Next(s.Length)]).ToArray());

            await emailSender.SendEmailAsync(user.Email, "EmailConfirmation", randomCode);

            user.ResetPasswordSendTime = DateTime.Now;
            user.ResetPasswordCode = randomCode;
            user.ResetPasswordCodeIsValid = true;

            return Ok(new ResponseModel { Status = "Success", Message = "Email sent successfully!" });
        }
    }
}
