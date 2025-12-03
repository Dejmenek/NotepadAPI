using System.Text.Json.Serialization;

namespace NotepadAPI.Responses;

public sealed record AuthenticationResponse(
   [property: JsonPropertyName("token")] string Token,
   [property: JsonPropertyName("expiration")] DateTime Expiration
);
