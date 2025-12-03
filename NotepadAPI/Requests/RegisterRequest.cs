using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotepadAPI.Requests;

public record RegisterRequest(
    [property: Required, EmailAddress, JsonPropertyName("email")] string Email,
    [property: Required, JsonPropertyName("password")] string Password
);
