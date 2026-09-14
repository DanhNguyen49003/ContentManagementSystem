using Microsoft.EntityFrameworkCore;
using ContentManagementSystem.DataLayer;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Services
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.IAuditLogService, ContentManagementSystem.Services.AuditLogService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.IBannerService, ContentManagementSystem.Services.BannerService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.ICategoryService, ContentManagementSystem.Services.CategoryService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.ICommentService, ContentManagementSystem.Services.CommentService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.IContactMessageService, ContentManagementSystem.Services.ContactMessageService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.IEventService, ContentManagementSystem.Services.EventService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.IFaqService, ContentManagementSystem.Services.FaqService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.IMediaAssetService, ContentManagementSystem.Services.MediaAssetService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.IMenuService, ContentManagementSystem.Services.MenuService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.IMenuItemService, ContentManagementSystem.Services.MenuItemService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.INewsletterSubscriberService, ContentManagementSystem.Services.NewsletterSubscriberService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.IPageService, ContentManagementSystem.Services.PageService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.IPartnerService, ContentManagementSystem.Services.PartnerService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.ISettingService, ContentManagementSystem.Services.SettingService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.ITagService, ContentManagementSystem.Services.TagService>();
builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.ITestimonialService, ContentManagementSystem.Services.TestimonialService>();


builder.Services.AddScoped<ContentManagementSystem.ApplicationCore.Interfaces.IPostService, ContentManagementSystem.Services.PostService>();

// Dependency Injection

builder.Services.AddDbContext<ContentManageDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ContentConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ContentManageIdentityDbContext>();

builder.Services.AddDbContext<ContentManageIdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ContentIdentityConnection") ?? builder.Configuration.GetConnectionString("DefaultConnection")));



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();








