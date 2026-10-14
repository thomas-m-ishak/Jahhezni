using Jahhezni.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. إضافة خدمات الـ MVC (Controllers & Views)
builder.Services.AddControllersWithViews();

// 2. إعداد قاعدة البيانات (SQL Server)
builder.Services.AddDbContext<JahhezniContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. إضافة خدمات الـ Session والكاش (مكانها الصح قبل الـ Build)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // وقت صلاحية السشن
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient<PaymobService>();

// 4. إضافة خدمة الـ Authentication باستخدام الـ Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});


// ----------------------------------------------------
var app = builder.Build(); // قفلنا بناء الخدمات تماماً
// ----------------------------------------------------

// 5. إعدادات الـ Middleware Pipeline (الترتيب الذهبي)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// تفعيل السشن هنا عشان الـ Authentication والـ Controllers يقدروا يقروا بيانات التاجر
app.UseSession();

// نظام الأمان والصلاحيات
app.UseAuthentication(); // أولاً: مين أنت؟ 

app.UseCors("AllowAll");

app.UseAuthorization();  // ثانياً: مسموح لك تدخل؟

app.MapStaticAssets();

// 6. إعداد المسارات (Routing)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}")
    .WithStaticAssets();

app.Run();