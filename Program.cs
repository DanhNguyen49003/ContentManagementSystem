using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ContentManagementSystem.ApplicationCore.Entities.Identity;
using ContentManagementSystem.DataLayer;
using ContentManagementSystem.Seeders;
using ContentManagementSystem.Service.Implementations;
using ContentManagementSystem.Service.Interface;
using ContentManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. Controller & Razor Views =====
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ===== 2. Email Sender Configuration =====
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();

// ===== 3. CORS & HttpClient Configuration =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ContentManagementSystem.Services.ApiClients.IApiClient, ContentManagementSystem.Services.ApiClients.ApiClient>();


// ===== 4. DI: Service Layer (Sử dụng trực tiếp Database cho cả Web CMS & RESTful API) =====
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IContactMessageService, ContactMessageService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IFaqService, FaqService>();
builder.Services.AddScoped<IMediaAssetService, MediaAssetService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddScoped<INewsletterSubscriberService, NewsletterSubscriberService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ISubmissionWindowService, SubmissionWindowService>();
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITestimonialService, TestimonialService>();

// ===== 5. DbContext Configuration (Supabase PostgreSQL) =====
builder.Services.AddDbContext<ContentManageDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ContentConnection")));

builder.Services.AddDbContext<ContentManageIdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ContentIdentityConnection") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== 6. Identity Configuration with ContentUser & ContentRole =====
builder.Services.AddIdentity<ContentUser, ContentRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 0;
    options.Lockout.AllowedForNewUsers = false;
})
.AddEntityFrameworkStores<ContentManageIdentityDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.Lockout.AllowedForNewUsers = false;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});

// ===== 7. Dual Authentication Configuration (Cookie for Razor Web + JWT Bearer for RESTful API) =====
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSection["SecretKey"] ?? "DefaultFallbackSecretKeyForCMSPortalJwtToken2026!";
var issuer = jwtSection["Issuer"] ?? "CMSPortalAPI";
var audience = jwtSection["Audience"] ?? "CMSPortalClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "SMART_AUTH";
    options.DefaultChallengeScheme = "SMART_AUTH";
})
.AddPolicyScheme("SMART_AUTH", "Bearer or Cookie", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        string? authHeader = context.Request.Headers["Authorization"];
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }
        return IdentityConstants.ApplicationScheme;
    };
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ClockSkew = TimeSpan.Zero
    };
});

// ===== 8. Swagger / OpenAPI Configuration =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CMS Portal RESTful API",
        Version = "v1",
        Description = "Hệ thống Web API hợp nhất chạy cùng CMS Portal Web."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập Token theo cú pháp: Bearer {token của bạn}",
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Seed Identity Roles and default Accounts
await IdentityDataSeeder.SeedAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=604800";
    }
});

// Swagger Documentation UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CMS Portal API v1");
    c.RoutePrefix = "swagger"; // Truy cập tại /swagger
});

app.UseCors("AllowAll");
app.UseRouting();

app.UseAuthentication();

// Middleware hỗ trợ xác thực System API Key từ các client ngoài
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated != true)
    {
        var configuredKey = app.Configuration["ApiSettings:SystemKey"] ?? "CMSPortalSecretKey2026!#";
        if (context.Request.Headers.TryGetValue("X-System-Key", out var systemKey) &&
            systemKey == configuredKey)
        {
            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "system-mvc-client"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "System Client"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "QA Manager"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "QA Coordinator"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Customer")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "SystemKey");
            context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        }
    }
    await next();
});

app.UseAuthorization();

// Route cho Web API Controllers
app.MapControllers();

// Route mặc định cho Web MVC Razor Pages & Views
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
