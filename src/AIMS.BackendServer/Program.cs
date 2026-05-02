using System.Text;
using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.BackendServer.Data.SeedData;
using AIMS.BackendServer.Services;
using AIMS.BackendServer.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Database ────────────────────────────────────────────
builder.Services.AddDbContext<AimsDbContext>(opts =>
    opts.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        pgOptions =>
        {
            // KHÔNG dùng EnableRetryOnFailure ở đây vì chúng ta tự retry bên ngoài.
            // EnableRetryOnFailure conflict với MigrateAsync trong vòng lặp retry thủ công.
            pgOptions.CommandTimeout(60);
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

// ── 3. JWT Settings ────────────────────────────────────────
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
            ClockSkew = TimeSpan.Zero,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };
    });

// ── 5. Authorization ───────────────────────────────────────
builder.Services.AddAuthorization();

// ── 6. Application Services ────────────────────────────────
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IPermissionCacheService, PermissionCacheService>();
builder.Services.AddSingleton<AIMS.BackendServer.Services.ML.IScreeningModelService, AIMS.BackendServer.Services.ML.ScreeningModelService>();
builder.Services.AddScoped<IAIScreeningService, AIScreeningService>();
builder.Services.AddSingleton<AIMS.BackendServer.Services.ML.IFeatureExporter, AIMS.BackendServer.Services.ML.FeatureExporter>();
builder.Services.AddSingleton<AIMS.BackendServer.Services.ML.ICsvNormalizer, AIMS.BackendServer.Services.ML.CsvNormalizer>();
builder.Services.AddHostedService<AIMS.BackendServer.Services.ML.ScreeningAutoRetrainer>();
builder.Services.AddSingleton<AIMS.BackendServer.Services.ML.IFeatureCsvConverter, AIMS.BackendServer.Services.ML.FeatureCsvConverter>();
builder.Services.AddDirectoryBrowser();

// ── 7. Controllers + OpenAPI ───────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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

        document.Components ??= new();
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

// ── 8. Validation + Mapper + CORS ─────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")));

builder.Services.AddCors(opts => opts.AddPolicy("AllowWebPortal", policy =>
    policy.WithOrigins("http://localhost:5000", "https://localhost:5000")
          .AllowAnyHeader()
          .AllowAnyMethod()));

// ── Build ──────────────────────────────────────────────────
var app = builder.Build();

// ── Migration + Seed ───────────────────────────────────────
await InitializeDatabaseAsync(app);

// ── Middleware Pipeline ────────────────────────────────────
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
        opts.WithTitle("AIMS API Reference")
            .WithTheme(ScalarTheme.Moon)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .AddPreferredSecuritySchemes("Bearer"));
}
app.UseCors("AllowWebPortal");

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AIMS.BackendServer.Middleware.PermissionMiddleware>();
app.MapControllers();

app.Run();

// ═══════════════════════════════════════════════════════════
// Hàm khởi tạo DB tách riêng — dễ đọc, dễ test
// ═══════════════════════════════════════════════════════════
static async Task InitializeDatabaseAsync(WebApplication app)
{
    const int MaxRetries = 10;
    const int BaseDelaySecs = 5;

    using var scope = app.Services.CreateScope();
    var sv = scope.ServiceProvider;
    var logger = sv.GetRequiredService<ILogger<Program>>();
    var context = sv.GetRequiredService<AimsDbContext>();
    var userMgr = sv.GetRequiredService<UserManager<AppUser>>();
    var roleMgr = sv.GetRequiredService<RoleManager<AppRole>>();

    // ── Bước 1: Chờ SQL Server sẵn sàng + chạy Migration ──
    for (int attempt = 1; attempt <= MaxRetries; attempt++)
    {
        try
        {
            logger.LogInformation("⏳ [{attempt}/{max}] Connecting & migrating database...",
                attempt, MaxRetries);

            // Kiểm tra kết nối trước khi migrate
            await context.Database.OpenConnectionAsync();
            await context.Database.CloseConnectionAsync();

            // Chạy migration
            await context.Database.MigrateAsync();

            logger.LogInformation("✅ Migration completed.");
            break; // Thành công → thoát vòng lặp
        }
        catch (Exception ex) when (IsTransientError(ex))
        {
            // Lỗi tạm thời (SQL Server chưa start, network...) → retry
            logger.LogWarning("⚠️ [{attempt}/{max}] Transient error: {msg}", attempt, MaxRetries, ex.Message);

            if (attempt == MaxRetries)
            {
                logger.LogError("❌ Cannot connect to database after {max} attempts.", MaxRetries);
                throw;
            }

            var delay = TimeSpan.FromSeconds(BaseDelaySecs * attempt);
            logger.LogInformation("⏱️ Retrying in {delay}s...", delay.TotalSeconds);
            await Task.Delay(delay);
        }
        catch (Exception ex) when (IsPendingModelChanges(ex))
        {
            // Model thay đổi chưa có migration → cảnh báo và tiếp tục
            logger.LogWarning(
                "⚠️ Pending EF model changes detected. " +
                "Run: dotnet ef migrations add <Name> && dotnet ef database update");
            break;
        }
        catch (Exception ex)
        {
            // Lỗi không phải transient (schema sai, thiếu bảng...) → dừng ngay
            logger.LogError(ex, "❌ Non-retryable migration error.");
            throw;
        }
    }

    // ── Bước 2: Kiểm tra schema tối thiểu trước khi seed ──
    // Nếu bảng AppRoles không tồn tại dù migration "up to date",
    // tức là migration files bị thiếu → báo lỗi rõ ràng thay vì crash mơ hồ.
    var appRolesExists = await TableExistsAsync(context, "AppRoles");
    if (!appRolesExists)
    {
        logger.LogError(
            "❌ Table [AppRoles] does not exist after migration. " +
            "Migration files may be missing or __EFMigrationsHistory is stale. " +
            "Try: dotnet ef migrations add InitialCreate && dotnet ef database update");
        throw new InvalidOperationException(
            "Table [AppRoles] missing after migration. See logs for details.");
    }

    // ── Bước 3: Seed data (chỉ chạy 1 lần, không retry) ──
    try
    {
        logger.LogInformation("🌱 Seeding data...");
        await DbInitializer.SeedAsync(context, userMgr, roleMgr);
        logger.LogInformation("✅ Seed data completed. Database is ready!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Seed data failed.");
        throw;
    }
}

// ── Helper: Phân loại lỗi transient ───────────────────────
static bool IsTransientError(Exception ex)
{
    // PostgreSQL connection errors handling
    return ex is TimeoutException ||
           ex is InvalidOperationException && ex.Message.Contains("already open", StringComparison.OrdinalIgnoreCase);
}

static bool IsPendingModelChanges(Exception ex) =>
    ex.Message?.Contains("pending changes", StringComparison.OrdinalIgnoreCase) == true ||
    ex.Message?.Contains("PendingModelChangesWarning", StringComparison.OrdinalIgnoreCase) == true;

// ── Helper: Kiểm tra bảng tồn tại trong DB ────────────────
static async Task<bool> TableExistsAsync(AimsDbContext context, string tableName)
{
    var conn = context.Database.GetDbConnection();
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = $@"SELECT EXISTS(
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name = '{tableName}'
    )";
    var result = await cmd.ExecuteScalarAsync();
    await conn.CloseAsync();
    return result is true;
}