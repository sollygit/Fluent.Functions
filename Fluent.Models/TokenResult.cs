using Newtonsoft.Json;

namespace Fluent.Models
{
    public record TokenResult(
        [property: JsonProperty("token_type")] string TokenType,
        [property: JsonProperty("expires_in")] int ExpiresIn,
        [property: JsonProperty("ext_expires_in")] int ExtExpiresIn,
        [property: JsonProperty("access_token")] string AccessToken
    );
}
