using Microsoft.AspNetCore.Identity;

namespace NotepadAPI.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
