﻿using BaseLibrary.DTOs;
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerLibrary.Data;
using ServerLibrary.Helpers;
using ServerLibrary.Repositories.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;



namespace ServerLibrary.Repositories.Implementations
{
    public class UserAccountRepository(IOptions<JwtSection> config, AppDbContext appDbContext ) : IUserAccount
    {
        public async Task<GeneralResponse> CreateAsync(Register user)
        {
            if (user is null) return new GeneralResponse(false, "User cannot be null");

            var checkUser = await FindUserByEmail(user.Email!);
            if (checkUser != null) return new GeneralResponse(false, "User already exists");

            var applicationUser = await AddToDatabase(new ApplicationUser()
            {
                FullName = user.FullName,
                Email = user.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(user.Password)
            });



            var checkAdminRole = await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.Admin));
            if(checkAdminRole is null)
            {
                var createAdminRole = await AddToDatabase(new SystemRole() { Name = Constants.Admin });
                await AddToDatabase(new UserRole() { RoleId = createAdminRole.Id, UserId = applicationUser.Id });
                return new GeneralResponse(true, "User created successfully");
            }

            var checkUserRole = await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.User));
            SystemRole response = new();
            if (checkUserRole is null)
            {
                response = await AddToDatabase(new SystemRole() { Name = Constants.User });
                await AddToDatabase(new UserRole() { RoleId = response.Id, UserId = applicationUser.Id });
            }
            else
            {
                await AddToDatabase(new UserRole() { RoleId = checkUserRole.Id, UserId = applicationUser.Id });
            }
            return new GeneralResponse(true, "User created successfully");
        }

        public async Task<LoginResponse> SignInAsync(Login user)
        {
            if(user is null) return new LoginResponse(false, "User cannot be null");

            var applicationUser = await FindUserByEmail(user.Email!);
            if (applicationUser is null) return new LoginResponse(false, "User not found");

            //verifyyyy password
            if (!BCrypt.Net.BCrypt.Verify(user.Password, applicationUser.Password))
                return new LoginResponse(false, "Invalid email/password");

            var getUserRole = await appDbContext.UserRoles.FirstOrDefaultAsync(_ => _.UserId == applicationUser.Id);
            if (getUserRole is null) return new LoginResponse(false, "User role not found");

            var getRoleName = await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Id == getUserRole.RoleId);
            if (getUserRole is null) return new LoginResponse(false, "User role not found");


            string jwtToken = GenerateToken(applicationUser, getRoleName.Name!);
            string refreshToken = GenerateRefreshToken();
            return new LoginResponse(true, "Login successfully", jwtToken, refreshToken);

        }


        private string GenerateToken(ApplicationUser user, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Value.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                 new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                 new Claim(ClaimTypes.Name, user.FullName!),
                 new Claim(ClaimTypes.Email, user.Email!),
                 new Claim(ClaimTypes.Role, role)
    };
            var token = new JwtSecurityToken(
                issuer: config.Value.Issuer,
                audience: config.Value.Audience,
                claims: userClaims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(46));
        private async Task<ApplicationUser> FindUserByEmail(string email) =>
            await appDbContext.ApplicationUsers.FirstOrDefaultAsync(x => x.Email!.ToLower()!.Equals(email!.ToLower()));


        private async Task<T> AddToDatabase<T>(T model)
        {
            var result = appDbContext.Add(model!);
            await appDbContext.SaveChangesAsync();
            return (T)result.Entity;
        }
    }
}
