using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Infrastructure;

namespace Contract_Monthly_Claim_System
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            QuestPDF.Settings.License = LicenseType.Community;

            // Add services to the container.
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });
            builder.Services.AddScoped<IDataService, SqlDataService>();
            builder.Services.AddScoped<IPdfService, PdfService>();
            builder.Services.AddScoped<Services.IAuthenticationService, Services.AuthenticationService>();

            // Anti-forgery tokens protect all unsafe HTTP requests.
            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.Name = ".CMCS.AntiForgery";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
            });

            // Apply secure defaults to application cookies.
            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.HttpOnly = HttpOnlyPolicy.Always;
                options.Secure = CookieSecurePolicy.Always;
                options.MinimumSameSitePolicy = SameSiteMode.Strict;
            });

            // Add Entity Framework with SQLite 
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Session support
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(1); // Session timeout
                options.Cookie.Name = ".CMCS.Session";
                options.Cookie.HttpOnly = true; // Security: prevent JavaScript access
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate(); // applies migrations automatically
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCookiePolicy();

            // Enable session middleware BEFORE authorization
            app.UseSession();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Auth}/{action=Login}/{id?}");

            app.Run();
        }
    }
}
