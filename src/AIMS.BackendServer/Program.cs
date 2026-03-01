using System.Text;
using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.BackendServer.Data.SeedData;
using AIMS.BackendServer.Services;
using AIMS.BackendServer.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using FluentValidation;
using FluentValidation.AspNetCore;

// Bổ sung các namespace bắt buộc cho OpenAPI
using Microsoft.OpenApi;
var builder = WebApplication.CreateBuilder(args);

// ── 1. Database ────────────────────────────────────────────
builder.Services.AddDbContext<AimsDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration
        .GetConnectionString("DefaultConnection")));

// ── 2. Identity ────────────────────────────────────────────
builder.Services.AddIdentity<AppUser, AppRole>(opts =>
{
    opts.Password.RequireDigit = true;
    opts.Password.RequiredLength = 6;
    opts.Password.RequireNonAlphanumeric = false;
    opts.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AimsDbContext>()
.AddDefaultTokenProviders();

// ── 3. JWT Settings (bind từ appsettings.json) ─────────────
var jwtSettings = builder.Configuration
    .GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwtSettings);

// ── 4. JWT Authentication ──────────────────────────────────
builder.Services
    .AddAuthentication(opts =>
    {
        opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero, // Hết hạn chính xác
        };
    });

// ── 5. Authorization ───────────────────────────────────────
builder.Services.AddAuthorization();

// ── 6. Services ────────────────────────────────────────────
builder.Services.AddScoped<ITokenService, TokenService>();

// ── 7. Controllers + OpenAPI (Scalar) ──────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Cấu hình OpenAPI với Bearer Token
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Title = "AIMS API",
            Version = "v1",
            Description = "Hệ thống Quản lý Thực tập sinh Thông minh — DEHA Việt Nam",
        };

        // Thêm Bearer Token security scheme
        document.Components ??= new();

        // Fix: Sử dụng IOpenApiSecurityScheme cho Dictionary để match với .NET 9 OpenApi target type
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Nhập JWT token. VD: eyJhbGci...",
        };

        return Task.CompletedTask;
    });
});

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// ── CORS (cho WebPortal gọi API) ───────────────────────────
builder.Services.AddCors(opts => opts.AddPolicy("AllowWebPortal", policy =>
    policy.WithOrigins("http://localhost:5000", "https://localhost:5000")
          .AllowAnyHeader().AllowAnyMethod()));

// ── Build ──────────────────────────────────────────────────
var app = builder.Build();

// ── Seed Data ──────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var sv = scope.ServiceProvider;
    var ctx = sv.GetRequiredService<AimsDbContext>();
    var userMgr = sv.GetRequiredService<UserManager<AppUser>>();
    var roleMgr = sv.GetRequiredService<RoleManager<AppRole>>();
    await DbInitializer.SeedAsync(ctx, userMgr, roleMgr);
}

// ── Middleware Pipeline ─────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.WithTitle("AIMS API Reference")
            .WithTheme(ScalarTheme.Moon)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            // Fix: Sử dụng các extension method mới để tránh warning CS0618
            .AddPreferredSecuritySchemes("Bearer");
    });
}

app.UseCors("AllowWebPortal");
app.UseHttpsRedirection();
app.UseAuthentication();   // ← PHẢI trước UseAuthorization
app.UseAuthorization();
// ⭐ Permission Middleware — đặt SAU Authentication
app.UseMiddleware<AIMS.BackendServer.Middleware.PermissionMiddleware>();

app.MapControllers();
app.Run();