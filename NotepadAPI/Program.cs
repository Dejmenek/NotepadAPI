using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotepadAPI.Data;
using NotepadAPI.Models;
using NotepadAPI.Requests;
using NotepadAPI.Responses;
using NotepadAPI.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<JwtService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured."),
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured."),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured."))
            )
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

var notes = app.MapGroup("/notes").RequireAuthorization();

app.MapPost("/login", async Task<Results<BadRequest<ErrorResponse>, Ok<string>>> (
    [FromBody] LoginRequest request,
    UserManager<ApplicationUser> userManager,
    JwtService jwtService) =>
{
    var validationResults = new List<ValidationResult>();

    var validationContext = new ValidationContext(request);

    if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
    {
        var errors = validationResults.ToDictionary(
                    v => v.MemberNames.FirstOrDefault() ?? "Error",
                    v => new string[] { v.ErrorMessage! });

        return TypedResults.BadRequest(new ErrorResponse("Validation failed", errors));
    }

    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null) return TypedResults.BadRequest(new ErrorResponse("Invalid email or password"));

    var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
    if (!isPasswordValid) return TypedResults.BadRequest(new ErrorResponse("Invalid email or password"));

    var jwtToken = jwtService.CreateToken(user);

    return TypedResults.Ok(jwtToken);
});

app.MapPost("/register", async Task<Results<BadRequest<ErrorResponse>, Ok>> (
    [FromBody] RegisterRequest request,
    UserManager<ApplicationUser> userManager,
    JwtService jwtService
    ) =>
{
    var validationResults = new List<ValidationResult>();

    var validationContext = new ValidationContext(request);

    if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
    {
        var errors = validationResults.ToDictionary(
                    v => v.MemberNames.FirstOrDefault() ?? "Error",
                    v => new string[] { v.ErrorMessage! });

        return TypedResults.BadRequest(new ErrorResponse("Validation failed", errors));
    }

    var registerResult = await userManager.CreateAsync(
        new ApplicationUser() { Email = request.Email, UserName = request.Email },
        request.Password
    );

    if (!registerResult.Succeeded) return TypedResults.BadRequest(new ErrorResponse("Registration failed"));

    return TypedResults.Ok();
});

notes.MapGet("/", async Task<Results<UnauthorizedHttpResult, Ok<IEnumerable<GetNoteResponse>>>> (
    ClaimsPrincipal user, CancellationToken token, ApplicationDbContext context) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null) return TypedResults.Unauthorized();

    var notes = await context.Notes.Where(n => n.UserId == userId).ToListAsync(token);
    var notesResponse = notes.Select(n => new GetNoteResponse(n.Id, n.Content));

    return TypedResults.Ok(notesResponse);
});

notes.MapPost("/", async Task<Results<UnauthorizedHttpResult, BadRequest<ErrorResponse>, Created<GetNoteResponse>>> (
    ClaimsPrincipal user,
    NoteRequest request,
    CancellationToken token,
    ApplicationDbContext context) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null) return TypedResults.Unauthorized();

    var validationResults = new List<ValidationResult>();

    var validationContext = new ValidationContext(request);

    if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
    {
        var errors = validationResults.ToDictionary(
                    v => v.MemberNames.FirstOrDefault() ?? "Error",
                    v => new string[] { v.ErrorMessage! });

        return TypedResults.BadRequest(new ErrorResponse("Validation failed", errors));
    }

    var noteToAdd = new Note
    {
        Content = request.Content,
        UserId = userId
    };

    var note = context.Notes.Add(noteToAdd);
    await context.SaveChangesAsync(token);

    var noteResponse = new GetNoteResponse(note.Entity.Id, note.Entity.Content);
    return TypedResults.Created($"/notes/{noteResponse.Id}", noteResponse);
});

notes.MapPut("/{id}", async Task<Results<BadRequest<ErrorResponse>, NotFound, NoContent, UnauthorizedHttpResult, ForbidHttpResult>> (
    int id,
    ClaimsPrincipal user,
    NoteRequest request,
    CancellationToken token,
    ApplicationDbContext context) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null) return TypedResults.Unauthorized();

    var validationResults = new List<ValidationResult>();

    var validationContext = new ValidationContext(request);

    if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
    {
        var errors = validationResults.ToDictionary(
                    v => v.MemberNames.FirstOrDefault() ?? "Error",
                    v => new string[] { v.ErrorMessage! });

        return TypedResults.BadRequest(new ErrorResponse("Validation failed", errors));
    }

    var noteToEdit = await context.Notes
        .FirstOrDefaultAsync(n => n.Id == id, token);

    if (noteToEdit is null) return TypedResults.NotFound();

    if (noteToEdit.UserId != userId) return TypedResults.Forbid();

    noteToEdit.Content = request.Content;
    await context.SaveChangesAsync(token);

    return TypedResults.NoContent();
});

notes.MapDelete("/{id}", async Task<Results<NotFound, NoContent, UnauthorizedHttpResult, ForbidHttpResult>> (
    int id,
    ClaimsPrincipal user,
    CancellationToken token,
    ApplicationDbContext context) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null) return TypedResults.Unauthorized();

    var noteToDelete = await context.Notes
        .FirstOrDefaultAsync(n => n.Id == id, token);

    if (noteToDelete is null) return TypedResults.NotFound();

    if (noteToDelete.UserId != userId) return TypedResults.Forbid();

    context.Notes.Remove(noteToDelete);
    await context.SaveChangesAsync(token);

    return TypedResults.NoContent();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.Run();