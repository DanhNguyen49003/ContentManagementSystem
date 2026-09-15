using Microsoft.EntityFrameworkCore;
using ContentManagementSystem.DataLayer;
using Microsoft.AspNetCore.Identity;
using ContentManagementSystem.ApplicationCore.Entities.Identity;
using ContentManagementSystem.Service.Interface;
using ContentManagementSystem.Service.Implementations;
using ContentManagementSystem.Seeders;

using Microsoft.AspNetCore.Identity.UI.Services;
using ContentManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ===== Email Sender Configuration =====
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();

// ===== HttpClient & API Client Configuration =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ContentManagementSystem.Services.ApiClients.IApiClient, ContentManagementSystem.Services.ApiClients.ApiClient>();

// ===== DI: Service Layer (Gọi 100% qua RESTful Web API) =====
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IBannerService, ContentManagementSystem.Services.ApiClients.BannerApiService>();
builder.Services.AddScoped<ICategoryService, ContentManagementSystem.Services.ApiClients.CategoryApiService>();
builder.Services.AddScoped<ICommentService, ContentManagementSystem.Services.ApiClients.CommentApiService>();
builder.Services.AddScoped<IContactMessageService, ContentManagementSystem.Services.ApiClients.ContactMessageApiService>();
builder.Services.AddScoped<IEventService, ContentManagementSystem.Services.ApiClients.EventApiService>();
builder.Services.AddScoped<IFaqService, ContentManagementSystem.Services.ApiClients.FaqApiService>();
builder.Services.AddScoped<IMediaAssetService, MediaAssetService>();
builder.Services.AddScoped<IMenuService, ContentManagementSystem.Services.ApiClients.MenuApiService>();
builder.Services.AddScoped<IMenuItemService, ContentManagementSystem.Services.ApiClients.MenuItemApiService>();
builder.Services.AddScoped<INewsletterSubscriberService, ContentManagementSystem.Services.ApiClients.NewsletterSubscriberApiService>();
builder.Services.AddScoped<IPageService, ContentManagementSystem.Services.ApiClients.PageApiService>();
builder.Services.AddScoped<IPartnerService, ContentManagementSystem.Services.ApiClients.PartnerApiService>();
builder.Services.AddScoped<IPostService, ContentManagementSystem.Services.ApiClients.PostApiService>();
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<ITagService, ContentManagementSystem.Services.ApiClients.TagApiService>();
builder.Services.AddScoped<ITestimonialService, ContentManagementSystem.Services.ApiClients.TestimonialApiService>();

// ===== DbContext =====
builder.Services.AddDbContext<ContentManageDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ContentConnection")));

builder.Services.AddDbContext<ContentManageIdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ContentIdentityConnection") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== Identity Configuration with ContentUser & ContentRole =====
builder.Services.AddIdentity<ContentUser, ContentRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ContentManageIdentityDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

