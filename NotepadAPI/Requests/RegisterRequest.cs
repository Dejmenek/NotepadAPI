using System.ComponentModel.DataAnnotations;

namespace NotepadAPI.Requests;

public record RegisterRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password
);
