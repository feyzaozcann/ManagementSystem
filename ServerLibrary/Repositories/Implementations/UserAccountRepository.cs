using BaseLibrary.DTOs;
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
    public class UserAccountRepository(IOptions<JwtSection> config, AppDbContext appDbContext) : IUserAccount
    {
        public async Task<GeneralResponse> CreateAsync(Register user)
        {
            if (user is null)
                return new GeneralResponse(false, "User cannot be null");

            var existing = await FindUserByEmail(user.Email!);
            if (existing != null)
                return new GeneralResponse(false, "User already exists");

            var applicationUser = await AddToDatabase(new ApplicationUser
            {
                FullName = user.FullName,
                Email = user.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(user.Password)
            });

            var adminRole = await appDbContext.SystemRoles
                                              .FirstOrDefaultAsync(r => r.Name == Constants.Admin);
            if (adminRole is null)
            {
                adminRole = await AddToDatabase(new SystemRole { Name = Constants.Admin });
                await AddToDatabase(new UserRole { RoleId = adminRole.Id, UserId = applicationUser.Id });
                return new GeneralResponse(true, "User created successfully");
            }

            var userRole = await appDbContext.SystemRoles
                                             .FirstOrDefaultAsync(r => r.Name == Constants.User);
            if (userRole is null)
            {
                userRole = await AddToDatabase(new SystemRole { Name = Constants.User });
            }
            await AddToDatabase(new UserRole { RoleId = userRole.Id, UserId = applicationUser.Id });

            return new GeneralResponse(true, "User created successfully");
        }

        public async Task<LoginResponse> SignInAsync(Login user)
        {
            if (user is null)
                return new LoginResponse(false, "User cannot be null");

            var applicationUser = await FindUserByEmail(user.Email!);
            if (applicationUser is null)
                return new LoginResponse(false, "User not found");

            if (!BCrypt.Net.BCrypt.Verify(user.Password, applicationUser.Password))
                return new LoginResponse(false, "Invalid email/password");

            var userRole = await FindUserRole(applicationUser.Id);
            var roleEntity = await FindRoleNAme(userRole?.RoleId ?? 0);
            string roleName = roleEntity?.Name ?? Constants.User;

            string jwtToken = GenerateToken(applicationUser, roleName);
            string refreshToken = GenerateRefreshToken();

            var existingToken = await appDbContext.RefreshTokenInfos
                                                 .FirstOrDefaultAsync(r => r.UserId == applicationUser.Id);
            if (existingToken is null)
            {
                await AddToDatabase(new RefreshTokenInfo
                {
                    UserId = applicationUser.Id,
                    Token = refreshToken
                });
            }
            else
            {
                existingToken.Token = refreshToken;
                await appDbContext.SaveChangesAsync();
            }

            return new LoginResponse(
                true,
                "Login successfully",
                jwtToken,
                refreshToken
            );
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshToken tokenDto)
        {
            if (tokenDto is null || string.IsNullOrWhiteSpace(tokenDto.Token))
                return new LoginResponse(false, "Refresh token missing");

            var stored = await appDbContext.RefreshTokenInfos
                                           .FirstOrDefaultAsync(r => r.Token == tokenDto.Token);
            if (stored is null)
                return new LoginResponse(false, "Token required");

            var user = await appDbContext.ApplicationUsers
                                         .FirstOrDefaultAsync(u => u.Id == stored.UserId);
            if (user is null)
                return new LoginResponse(false, "User not found for this token");

            var userRole = await FindUserRole(user.Id);
            var roleEntity = await FindRoleNAme(userRole?.RoleId ?? 0);
            string roleName = roleEntity?.Name ?? Constants.User;

            string newJwt = GenerateToken(user, roleName);
            string newRefresh = GenerateRefreshToken();

            stored.Token = newRefresh;
            await appDbContext.SaveChangesAsync();

            return new LoginResponse(
                true,
                "Token refreshed successfully",
                newJwt,
                newRefresh
            );
        }

        private string GenerateToken(ApplicationUser user, string role)
        {
            var key = Encoding.UTF8.GetBytes(config.Value.Key);
            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, role)
            };
            var jwt = new JwtSecurityToken(
                issuer: config.Value.Issuer,
                audience: config.Value.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        private static string GenerateRefreshToken()
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(46));

        private async Task<UserRole?> FindUserRole(int userId)
            => await appDbContext.UserRoles.FirstOrDefaultAsync(r => r.UserId == userId);

        private async Task<SystemRole?> FindRoleNAme(int roleId)
            => await appDbContext.SystemRoles.FirstOrDefaultAsync(r => r.Id == roleId);

        private async Task<ApplicationUser?> FindUserByEmail(string email)
            => await appDbContext.ApplicationUsers
                                 .FirstOrDefaultAsync(u => u.Email!.ToLower() == email.ToLower());

        private async Task<T> AddToDatabase<T>(T entity)
        {
            var entry = appDbContext.Add(entity!);
            await appDbContext.SaveChangesAsync();
            return (T)entry.Entity;
        }
    }
}
