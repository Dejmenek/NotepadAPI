using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotepadAPI.Requests;

public record CreateNoteRequest(
   [property: JsonPropertyName("content"), Required] string Content
);
