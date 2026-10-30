using System.Text;
using LinkVaultApi.Agent.Hubs;
using LinkVaultApi.Agent.Models;
using LinkVaultApi.Agent.Services;
using LinkVaultApi.Data;
using LinkVaultApi.Hubs;
using LinkVaultApi.Middleware;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IO.Compression;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Validate environment secrets and configuration before bootstrapping services
StartupValidator.Validate(builder.Configuration);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LinkVault API", Version = "v1" });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database Context
builder.Services.AddDbContext<LinkVaultDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.Zero
    };
    
    // SignalR JWT authentication
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireCorporate", policy => policy.RequireClaim("accountType", "Corporate"));
    // NOTE: claim names must exactly match what JwtService.GenerateToken mints (lowercase)
    options.AddPolicy("CanUpload", policy => policy.RequireClaim("canUpload", "True"));
    options.AddPolicy("CanUpdate", policy => policy.RequireClaim("canUpdate", "True"));
    options.AddPolicy("CanExport", policy => policy.RequireClaim("canExport", "True"));
    options.AddPolicy("CanDelete", policy => policy.RequireClaim("canDelete", "True"));
    options.AddPolicy("CanShare", policy => policy.RequireClaim("canShare", "True"));
    options.AddPolicy("CanManageUsers", policy => policy.RequireClaim("canManageUsers", "True"));
    options.AddPolicy("CanManageRoles", policy => policy.RequireClaim("canManageRoles", "True"));
    options.AddPolicy("CanManageSettings", policy => policy.RequireClaim("canManageSettings", "True"));
    // Only Admin can view audit logs
    options.AddPolicy("CanViewAuditLogs", policy => policy.RequireRole("Admin"));
});

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()!)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddSingleton<IStorageProvider, LocalStorageProvider>();
builder.Services.AddSingleton<IFileValidationService, FileValidationService>();

// Meta Llama & Workspace Agent Services
builder.Services.Configure<MetaLlamaConfig>(builder.Configuration.GetSection(MetaLlamaConfig.SectionName));
builder.Services.AddHttpClient<IMetaLlamaClient, MetaLlamaClient>();
builder.Services.AddScoped<ISafeShareGuard, SafeShareGuard>();
builder.Services.AddScoped<IVaultAgentService, VaultAgentService>();

// Rate limiting: 100 requests per minute per IP globally, 10 req/min for auth
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("agent_plan", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests. Please slow down.",
            retryAfterSeconds = 60
        }, cancellationToken: token);
    };
});

// Health checks
builder.Services.AddHealthChecks();

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Response compression (Brotli + GZip)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/plain", "text/css", "application/javascript", "image/svg+xml" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
    else
    {
        dbContext.Database.EnsureCreated();
    }
}

// Reverse proxy forwarded headers handling with single-hop limit to prevent IP spoofing
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
app.UseForwardedHeaders(forwardedHeadersOptions);

// Only redirect to HTTPS in production — local dev has no valid cert
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

// Response compression
app.UseResponseCompression();

// Security headers (Dropbox-grade)
app.UseMiddleware<SecurityHeadersMiddleware>();

// Rate limiting
app.UseRateLimiter();

// Ensure uploads folder exists for storage provider
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseAuthentication();
app.UseAuthorization();

// Custom middleware
app.UseMiddleware<AuditMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers();

// Health endpoint
app.MapHealthChecks("/health");

// SignalR hubs
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<ActivityHub>("/hubs/activity");
app.MapHub<AgentHub>("/hubs/agent");

app.Run();

public partial class Program { }

