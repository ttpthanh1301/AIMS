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

var builder = WebApplication.CreateBuilder(args);

// ── 1. Database ────────────────────────────────────────────
// ── Database ──────────────────────────────────────────
builder.Services.AddDbContext<AimsDbContext>(opts =>
    opts.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null   // Retry tất cả lỗi transient
            );
            sqlOptions.CommandTimeout(60);
        }
    )
);

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
builder.Services.AddOpenApi();

// ── CORS (cho WebPortal gọi API) ───────────────────────────
builder.Services.AddCors(opts => opts.AddPolicy("AllowWebPortal", policy =>
    policy.WithOrigins("http://localhost:5000", "https://localhost:5000")
          .AllowAnyHeader().AllowAnyMethod()));

// ── Build ──────────────────────────────────────────────────
var app = builder.Build();

// ── Migration + Seed Data ─────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var sv = scope.ServiceProvider;
    var logger = sv.GetRequiredService<ILogger<Program>>();
    var context = sv.GetRequiredService<AimsDbContext>();
    var userMgr = sv.GetRequiredService<UserManager<AppUser>>();
    var roleMgr = sv.GetRequiredService<RoleManager<AppRole>>();

    // Retry loop — chờ SQL Server tạo xong DB
    var maxRetries = 10;
    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            logger.LogInformation("⏳ Attempt {i}/{max}: Migrating database...", i, maxRetries);

            // Tạo database + chạy migration
            await context.Database.MigrateAsync();

            // Seed data
            await DbInitializer.SeedAsync(context, userMgr, roleMgr);

            logger.LogInformation("✅ Database ready!");
            break; // Thành công → thoát loop
        }
        catch (Exception ex)
        {
            logger.LogWarning("⚠️ Attempt {i} failed: {msg}", i, ex.Message);

            if (i == maxRetries)
            {
                logger.LogError("❌ Could not initialize database after {max} attempts.", maxRetries);
                throw;
            }

            await Task.Delay(TimeSpan.FromSeconds(5 * i)); // Tăng dần: 5s, 10s, 15s...
        }
    }
}

// ── Middleware Pipeline ─────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
        opts.WithTitle("AIMS API")
            .WithTheme(ScalarTheme.Moon)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));
}

app.UseCors("AllowWebPortal");
app.UseHttpsRedirection();
app.UseAuthentication();   // ← PHẢI trước UseAuthorization
app.UseAuthorization();
app.MapControllers();
app.Run();