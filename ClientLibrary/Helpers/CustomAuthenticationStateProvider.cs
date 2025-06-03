using BaseLibrary.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ClientLibrary.Helpers
{
    public class CustomAuthenticationStateProvider(LocalStorageService localStorageService) : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal anonymous = new(new ClaimsIdentity());
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var stringToken = await localStorageService.GetToken();
            if(string.IsNullOrEmpty(stringToken)) return await Task.FromResult(new AuthenticationState(anonymous));

            var deserializeToken = Serializations.DeserializeJsonString<UserSession>(stringToken);
            if (deserializeToken is null) return await Task.FromResult(new AuthenticationState(anonymous));

            var getUserClaims = DescryptToken(deserializeToken.Token);
            if (getUserClaims is null) return await Task.FromResult(new AuthenticationState(anonymous));

            var claimsPrincipal = new SetClaimPrincipal(getUserClaims);
            return await Task.FromResult(new AuthenticationState(claimsPrincipal));

        }

        private static CustomUserClaims DecryptToken(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken)) return new CustomUserClaims();

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);

            var userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var name = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
            var email = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var role = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

            return new CustomUserClaims(
                userId: userId?.Value ?? "",
                name: name?.Value ?? "",
                email: email?.Value ?? "",
                role: role?.Value ?? ""
            );
        }
    }
}
