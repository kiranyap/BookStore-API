﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BookStore_API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BookStore_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _config;

        public UsersController(SignInManager<IdentityUser> signInManager, 
            UserManager<IdentityUser> userManager,
            IConfiguration config)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _config = config;
        }


        /// <summary>
        /// User Registration Endpoint
        /// </summary>
        /// <param name="userDTO"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] UserDTO userDTO)
        {
            try
            {
                var username = userDTO.EmailAddress;
                var password = userDTO.Password;

                var user = new IdentityUser { Email = username, UserName = username };
                var result = await _userManager.CreateAsync(user, password);

                if(!result.Succeeded)
                {
                    foreach(var error in result.Errors)
                    {
                        //log
                    }
                    return StatusCode(500);
                }

                return Ok(new { result.Succeeded });
            }
            catch (Exception)
            {

                throw;
            }
        }



        /// <summary>
        /// User Login Endpoint
        /// </summary>
        /// <param name="userDTO"></param>
        /// <returns></returns>
        [Route("login")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] UserDTO userDTO)
        {
            var username = userDTO.EmailAddress;
            var password = userDTO.Password;
            var result = await _signInManager.PasswordSignInAsync(username, password, false, false);

            if(result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(username);
                var tokenString = await GenerateJSONWebToken(user);
                return Ok(new { token = tokenString });
            }

            return Unauthorized(userDTO);
        }

        private async Task<string> GenerateJSONWebToken(IdentityUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(r => new Claim(ClaimsIdentity.DefaultRoleClaimType, r)));

            var token = new JwtSecurityToken(_config["Jwt:Issuer"]
                , _config["Jwt:Issuer"]
                , claims
                , null
                , expires: DateTime.Now.AddMinutes(5)
                , signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
