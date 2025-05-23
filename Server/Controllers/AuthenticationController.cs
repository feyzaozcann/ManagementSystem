﻿using BaseLibrary.DTOs;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary.Repositories.Contracts;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController(IUserAccount accountInterface) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> CreateAsync(Register user)
        {
            if (user is null) return BadRequest("User cannot be null");
            var result = await accountInterface.CreateAsync(user);
            return Ok(result);

        }
        
   

    [HttpPost("login")]
        public async Task<IActionResult> SignInAsync(Login user)
        {
            if (user is null) return BadRequest("User cannot be null");
            var result = await accountInterface.SignInAsync(user);
            return Ok(result);
        }
    }
}