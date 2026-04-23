using System.Text;
using MarketSaaS.Api.Authorization;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Options;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection(MongoOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<MercadoPagoOptions>(builder.Configuration.GetSection(MercadoPagoOptions.SectionName));

var mongoOpt = builder.Configuration.GetSection(MongoOptions.SectionName).Get<MongoOptions>()
    ?? new MongoOptions();
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoOpt.ConnectionString));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoOpt.DatabaseName);
});

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<INegocioService, NegocioService>();
builder.Services.AddSingleton<ICategoriaService, CategoriaService>();
builder.Services.AddSingleton<IProductoService, ProductoService>();
builder.Services.AddSingleton<IPedidoService, PedidoService>();
builder.Services.AddSingleton<IMercadoPagoPreferenciaService, MercadoPagoPreferenciaService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddScoped<RequireMatchingNegocioFilter>();
builder.Services.AddHostedService<MongoIndexInitializer>();

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
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Policies.SuperAdminOnly, p => p.RequireRole(Roles.SuperAdmin));
    o.AddPolicy(Policies.AdminTiendaOnly, p => p.RequireRole(Roles.AdminTienda));
    o.AddPolicy(Policies.ClienteOnly, p => p.RequireRole(Roles.Cliente));
    o.AddPolicy(Policies.SuperAdminOrAdminTienda, p => p.RequireRole(Roles.SuperAdmin, Roles.AdminTienda));
});

builder.Services.AddCors(o =>
{
    o.AddPolicy("SpaDev", p => p
        .WithOrigins("http://localhost:5173", "https://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddControllers();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
    app.UseCors("SpaDev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
