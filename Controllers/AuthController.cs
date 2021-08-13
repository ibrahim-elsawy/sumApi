using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using sumApi.Configuration;
using sumApi.Controllers.Models;
using sumApi.Controllers.Models.DTOs.Requests;
using sumApi.Controllers.Models.DTOs.Responses;
using sumApi.Data;

namespace sumApi.Controllers
{
	[ApiController]
	[Route("[controller]")]
	[Produces("application/json")]
	public class AuthController : ControllerBase
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly JwtConfig _jwtConfig;
		private readonly TokenValidationParameters _tokenValidationParams;
		private readonly ApiDbContext _apiDbContext;

		public AuthController(
		    UserManager<IdentityUser> userManager,
		    IOptionsMonitor<JwtConfig> optionsMonitor,
		    TokenValidationParameters tokenValidationParameters,
		    ApiDbContext apiDbContext)
		{
			_userManager = userManager;
			_jwtConfig = optionsMonitor.CurrentValue;
			_tokenValidationParams = tokenValidationParameters;
			_apiDbContext = apiDbContext;
		}
		[HttpGet]
		[Route("test")]
		public async Task<IActionResult> Test()
		{
			return Ok("7amra ya ota");
		}
		[HttpPost]
		[Route("Register")]
		public async Task<IActionResult> Register( UserRegistrationDto user)
		{
			if (ModelState.IsValid)
			{
				// We can utilise the model
				var existingUser = await _userManager.FindByEmailAsync(user.Email);

				if (existingUser != null)
				{
					return BadRequest(new RegistrationResponse()
					{
						Errors = new List<string>() {
				"Email already in use"
			    },
						Success = false
					});
				}

				var newUser = new IdentityUser() { Email = user.Email, UserName = user.Username };
				var isCreated = await _userManager.CreateAsync(newUser, user.Password);
				if (isCreated.Succeeded)
				{
					var jwtToken = await GenerateJwtToken(newUser);
					Console.WriteLine($"register function line 68 {jwtToken}");
					
					return Ok(jwtToken);
				}
				else
				{
					return BadRequest(new RegistrationResponse()
					{
						Errors = isCreated.Errors.Select(x => x.Description).ToList(),
						Success = false
					});
				}
			}

			return BadRequest(new RegistrationResponse()
			{
				Errors = new List<string>() {
			"Invalid payload"
		    },
				Success = false
			});
		}

		[HttpPost]
		[Route("Login")]
		public async Task<IActionResult> Login([FromBody] UserLoginRequest user)
		{
			if (ModelState.IsValid)
			{
				var existingUser = await _userManager.FindByEmailAsync(user.Email);

				if (existingUser == null)
				{
					return BadRequest(new RegistrationResponse()
					{
						Errors = new List<string>() {
				"Invalid login request"
			    },
						Success = false
					});
				}

				var isCorrect = await _userManager.CheckPasswordAsync(existingUser, user.Password);

				if (!isCorrect)
				{
					return BadRequest(new RegistrationResponse()
					{
						Errors = new List<string>() {
				"Invalid login request"
			    },
						Success = false
					});
				}

				var jwtToken = await GenerateJwtToken(existingUser);
				Console.WriteLine($"{jwtToken}");
				
				return Ok(jwtToken);
			}

			return BadRequest(new RegistrationResponse()
			{
				Errors = new List<string>() {
			"Invalid payload"
		    },
				Success = false
			});
		}

		[HttpPost]
		[Route("RefreshToken")]
		public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
		{
			if (ModelState.IsValid)
			{
				var result = await VerifyAndGenerateToken(tokenRequest);

				if (result == null)
				{
					return BadRequest(new RegistrationResponse()
					{
						Errors = new List<string>() {
			    "Invalid tokens"
			},
						Success = false
					});
				}

				return Ok(result);
			}

			return BadRequest(new RegistrationResponse()
			{
				Errors = new List<string>() {
		    "Invalid payload"
		},
				Success = false
			});
		}


		private async Task<AuthResult> GenerateJwtToken(IdentityUser user)
		{
			var jwtTokenHandler = new JwtSecurityTokenHandler();

			var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[]
			    {
		    new Claim("Id", user.Id),
		    new Claim(JwtRegisteredClaimNames.Email, user.Email),
		    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
		    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
		}),
				Expires = DateTime.UtcNow.AddSeconds(30), // 5-10 
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};

			var token = jwtTokenHandler.CreateToken(tokenDescriptor);
			var jwtToken = jwtTokenHandler.WriteToken(token);

			var refreshToken = new RefreshToken()
			{
				JwtId = token.Id,
				IsUsed = false,
				IsRevorked = false,
				UserId = user.Id,
				AddedDate = DateTime.UtcNow,
				ExpiryDate = DateTime.UtcNow.AddMonths(6),
				Token = RandomString(35) + Guid.NewGuid()
			};

			await _apiDbContext.RefreshToken.AddAsync(refreshToken);
			await _apiDbContext.SaveChangesAsync();
			var x = new AuthResult()
			{
				Token = jwtToken,
				Success = true,
				RefreshToken = refreshToken.Token
			};
			Console.WriteLine($"this generate funvtion line 210   {x}");

			return x;
		}

		private async Task<AuthResult> VerifyAndGenerateToken(TokenRequest tokenRequest)
		{
			var jwtTokenHandler = new JwtSecurityTokenHandler();

			try
			{
				// Validation 1 - Validation JWT token format
				var tokenInVerification = jwtTokenHandler.ValidateToken(tokenRequest.Token, _tokenValidationParams, out var validatedToken);

				// Validation 2 - Validate encryption alg
				if (validatedToken is JwtSecurityToken jwtSecurityToken)
				{
					var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

					if (result == false)
					{
						return null;
					}
				}

				// Validation 3 - validate expiry date
				var utcExpiryDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

				var expiryDate = UnixTimeStampToDateTime(utcExpiryDate);

				if (expiryDate > DateTime.UtcNow)
				{
					return new AuthResult()
					{
						Success = false,
						Errors = new List<string>() {
			    "Token has not yet expired"
			}
					};
				}

				// validation 4 - validate existence of the token
				var storedToken = await _apiDbContext.RefreshToken.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);

				if (storedToken == null)
				{
					return new AuthResult()
					{
						Success = false,
						Errors = new List<string>() {
			    "Token does not exist"
			}
					};
				}

				// Validation 5 - validate if used
				if (storedToken.IsUsed)
				{
					return new AuthResult()
					{
						Success = false,
						Errors = new List<string>() {
			    "Token has been used"
			}
					};
				}

				// Validation 6 - validate if revoked
				if (storedToken.IsRevorked)
				{
					return new AuthResult()
					{
						Success = false,
						Errors = new List<string>() {
			    "Token has been revoked"
			}
					};
				}

				// Validation 7 - validate the id
				var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

				if (storedToken.JwtId != jti)
				{
					return new AuthResult()
					{
						Success = false,
						Errors = new List<string>() {
			    "Token doesn't match"
			}
					};
				}

				// update current token 

				storedToken.IsUsed = true;
				_apiDbContext.RefreshToken.Update(storedToken);
				await _apiDbContext.SaveChangesAsync();

				// Generate a new token
				var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);
				return await GenerateJwtToken(dbUser);
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("Lifetime validation failed. The token is expired."))
				{

					return new AuthResult()
					{
						Success = false,
						Errors = new List<string>() {
			    "Token has expired please re-login"
			}
					};

				}
				else
				{
					return new AuthResult()
					{
						Success = false,
						Errors = new List<string>() {
			    "Something went wrong."
			}
					};
				}
			}
		}

		private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
		{
			var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dateTimeVal = dateTimeVal.AddSeconds(unixTimeStamp).ToUniversalTime();

			return dateTimeVal;
		}

		private string RandomString(int length)
		{
			var random = new Random();
			var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
			    .Select(x => x[random.Next(x.Length)]).ToArray());
		}
	}
}