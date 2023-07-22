using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController:BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;

        public AccountController(DataContext context, ITokenService tokenService)
        {
            this._tokenService = tokenService;
            this._context = context;
        }

        [HttpPost("register")] //Post: api/account/register
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerUser)
        {
            if(await UserExists(registerUser.UserName))
                return BadRequest($"{registerUser.UserName} already exists, try other user");
            else
            {
                using var hmac = new HMACSHA512();
            
                var user = new AppUser
                {
                    UserName = registerUser.UserName,
                    PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerUser.Password)),
                    PasswordSalt = hmac.Key
                };

                this._context.Users.Add(user);

                await this._context.SaveChangesAsync();
                return new UserDto()
                {
                    UserName = user.UserName,
                    Token = _tokenService.CreateToken(user)
                };
            }            
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _context.Users
            .FirstOrDefaultAsync(u=>u.UserName.ToLower()==loginDto.UserName.ToLower());
            if(user == null)
            {
                return BadRequest($"{loginDto.UserName} user is not valid !");
            }
            else
            {
                using var hmac = new HMACSHA512(user.PasswordSalt);
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

                for (int i = 0; i < computedHash.Length; i++)
                {
                    if(computedHash[i] != user.PasswordHash[i])
                    return Unauthorized("Login failed");
                }

                return new UserDto
                {
                    UserName = user.UserName,
                    Token = _tokenService.CreateToken(user)
                };
            }       
        }

        private async Task<bool> UserExists(string userName){
            return await this._context.Users.AnyAsync(u=>u.UserName.ToLower().Equals(userName.ToLower()));
        }
    }
}