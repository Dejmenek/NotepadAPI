namespace NotepadAPI.Responses;

public sealed record AuthenticationResponse(string Token, DateTime Expiration);
