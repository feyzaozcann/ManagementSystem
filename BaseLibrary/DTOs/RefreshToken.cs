using System.Text.Json.Serialization;

namespace BaseLibrary.DTOs
{
    public class RefreshToken
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; } 
    }
}
