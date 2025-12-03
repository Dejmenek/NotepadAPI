using System.Text.Json.Serialization;

namespace NotepadAPI.Responses;

public record GetNoteResponse(
   [property: JsonPropertyName("id")] int Id,
   [property: JsonPropertyName("content")] string Content
);
