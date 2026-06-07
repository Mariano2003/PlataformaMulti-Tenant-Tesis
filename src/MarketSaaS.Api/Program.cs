using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MarketSaaS.Api.Authorization;
using MarketSaaS.Api.Hubs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Options;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection(MongoOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<DevSeedOptions>(builder.Configuration.GetSection(DevSeedOptions.SectionName));
builder.Services.Configure<MercadoPagoOptions>(builder.Configuration.GetSection(MercadoPagoOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

var mongoOpt = builder.Configuration.GetSection(MongoOptions.SectionName).Get<MongoOptions>()
    ?? new MongoOptions();
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoOpt.ConnectionString));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoOpt.DatabaseName);
});

builder.Services.AddSingleton<IImageStorageService, LocalImageStorageService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<INegocioService, NegocioService>();
builder.Services.AddSingleton<ICategoriaService, CategoriaService>();
builder.Services.AddSingleton<IProductoService, ProductoService>();
builder.Services.AddSingleton<IPedidoService, PedidoService>();
builder.Services.AddSingleton<IAnalyticsService, AnalyticsService>();
builder.Services.AddSingleton<IChatRoomService, ChatRoomService>();
builder.Services.AddSingleton<IMercadoPagoPreferenciaService, MercadoPagoPreferenciaService>();
builder.Services.AddSingleton<IMercadoPagoConfirmacionService, MercadoPagoConfirmacionService>();
builder.Services.AddSingleton<MercadoPagoAccessTokenProvider>();
builder.Services.AddSingleton<IMercadoPagoAccessTokenProvider>(sp =>
    sp.GetRequiredService<MercadoPagoAccessTokenProvider>());
builder.Services.AddSingleton<IMercadoPagoOAuthService, MercadoPagoOAuthService>();
builder.Services.AddHttpClient(nameof(MercadoPagoOAuthService));
builder.Services.AddHttpClient(nameof(MercadoPagoAccessTokenProvider));
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddHttpClient(nameof(ResendEmailSender));
builder.Services.AddSingleton<MailKitEmailSender>();
builder.Services.AddSingleton<ResendEmailSender>();
builder.Services.AddSingleton<IEmailSender, EmailSenderFactory>();
builder.Services.AddSingleton<IPasswordRecoveryService, PasswordRecoveryService>();
builder.Services.AddScoped<RequireMatchingNegocioFilter>();
builder.Services.AddHostedService<MongoIndexInitializer>();
builder.Services.AddHostedService<DevIdentitySeedHostedService>();

var jwtOpt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var signingKey = string.IsNullOrEmpty(jwtOpt.SigningKey)
    ? null
    : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpt.SigningKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOpt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOpt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Policies.SuperAdminOnly, p => p.RequireRole(Roles.SuperAdmin));
    o.AddPolicy(Policies.AdminTiendaOnly, p => p.RequireRole(Roles.AdminTienda));
    o.AddPolicy(Policies.ClienteOnly, p => p.RequireRole(Roles.Cliente));
    o.AddPolicy(Policies.SuperAdminOrAdminTienda, p => p.RequireRole(Roles.SuperAdmin, Roles.AdminTienda));
});

var origenesProduccion = (builder.Configuration["Cors:ProductionOrigins"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(o =>
{
    o.AddPolicy("SpaDev", p => p
        .WithOrigins(
            "http://localhost:5173",
            "https://localhost:5173",
            "http://localhost:5174",
            "https://localhost:5174",
            "http://localhost:5175",
            "https://localhost:5175")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());

    if (origenesProduccion.Length > 0)
    {
        o.AddPolicy("SpaProd", p => p
            .WithOrigins(origenesProduccion)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    }
});

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MarketSaaS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "uploads")),
    RequestPath = "/uploads",
});
if (app.Environment.IsDevelopment())
    app.UseCors("SpaDev");
else if (origenesProduccion.Length > 0)
    app.UseCors("SpaProd");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.Run();
