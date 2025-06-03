using BaseLibrary.DTOs;
using BaseLibrary.Responses;
using ClientLibrary.Helpers;
using ClientLibrary.Services.Contracts;
using System.Net.Http.Json;

namespace ClientLibrary.Services.Implementations
{
    public class UserAccountService(GetHttpClient getHttpClient) : IUserAccountService
    {
        public const string AuthUrl = "api/authentication";
        public async Task<GeneralResponse> CreateAsync(Register user)
        {
            var httpClient = getHttpClient.GetPublicHttpClient();
            var result = await httpClient.PostAsJsonAsync($"{AuthUrl}/register", user);
            if (!result.IsSuccessStatusCode)
            {
                return new GeneralResponse(false, "Failed to create user account.");
            }
            return await result.Content.ReadFromJsonAsync<GeneralResponse>() ?? new GeneralResponse(false, "Failed to create user account.");
        }

    
        public async Task<LoginResponse> SignInAsync(Login user)
        {
            var httpClient = getHttpClient.GetPublicHttpClient();
            var result = await httpClient.PostAsJsonAsync($"{AuthUrl}/login", user);
            if (!result.IsSuccessStatusCode)
            {
                return new LoginResponse(false, "Failed to sign in.");
            }
            return await result.Content.ReadFromJsonAsync<LoginResponse>()!;
        }

        public Task<LoginResponse> RefreshTokenAsync(RefreshToken token)
        {
            throw new NotImplementedException();
        }

    }
}
