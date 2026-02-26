using System.Text;
using LinkVault.Api.Data;
using LinkVault.Api.Services;
using LinkVault.Api.Services.Interfaces;
using LinkVault.Api.Services.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace LinkVault.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.AddHttpContextAccessor();
        services.AddSignalR();
        services.AddHttpClient(); // for SSO OAuth token exchange

        // Core services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDirectoryService, DirectoryService>();
        services.AddScoped<ILinkService, LinkService>();

        // New services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IExportRequestService, ExportRequestService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IStorageProvider, LocalStorageProvider>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                    ClockSkew = TimeSpan.Zero
                };
                // Allow SignalR to receive JWT via query string
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query["access_token"];
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            ctx.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LinkVault API",
                Version = "v1",
                Description = "A comprehensive link management, file hosting, and team collaboration API"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
