using System.Security.Claims;
using Infrastructure.Data;
using Infrastructure.DI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => { options.IdleTimeout=TimeSpan.FromHours(2); options.Cookie.HttpOnly=true; options.Cookie.IsEssential=true; options.Cookie.SameSite=SameSiteMode.Lax; });
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options=>{options.LoginPath="/Admin/Login"; options.AccessDeniedPath="/Admin/Login"; options.Cookie.HttpOnly=true; options.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest; options.ExpireTimeSpan=TimeSpan.FromHours(8);});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("login", limiter=> { limiter.PermitLimit=5; limiter.Window=TimeSpan.FromMinutes(2); limiter.QueueLimit=0; }));
var app=builder.Build();
if(app.Environment.IsDevelopment() && app.Configuration.GetValue("Database:InitializeOnStartup",true))
{
 using var scope=app.Services.CreateScope();
 await using var db=await scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();
 // New standalone database only. Do not point this project at the BookNest database.
 // For production convert the schema to audited EF migrations, then disable automatic initialization.
 await db.Database.EnsureCreatedAsync();
 if(!await db.Categories.AnyAsync())
 {
  var firstAid=new Domain.Entities.Category{Name="Первая помощь",Description="Перевязочные средства и принадлежности"};
  var hygiene=new Domain.Entities.Category{Name="Гигиена",Description="Товары для повседневного ухода"};
  var devices=new Domain.Entities.Category{Name="Домашняя диагностика",Description="Приборы и аксессуары"};
  var personal=new Domain.Entities.Category{Name="Уход",Description="Товары для комфорта"};
  var manufacturer=new Domain.Entities.Manufacturer{Name="MediNova Demo",Country="Демонстрационные данные"};
  db.Categories.AddRange(firstAid,hygiene,devices,personal);db.Manufacturers.Add(manufacturer);
  db.Products.AddRange(
   new Domain.Entities.Product{Name="Бинт стерильный",Form="Упаковка 1 шт.",Price=12,Stock=48,Category=firstAid,Manufacturer=manufacturer,Description="Стерильный перевязочный материал."},
   new Domain.Entities.Product{Name="Пластырь медицинский",Form="Набор, 20 шт.",Price=19,Stock=80,Category=firstAid,Manufacturer=manufacturer,Description="Набор пластырей для домашней аптечки."},
   new Domain.Entities.Product{Name="Цифровой термометр",Form="1 прибор",Price=79,Stock=12,Category=devices,Manufacturer=manufacturer,Description="Электронный прибор для измерения температуры."},
   new Domain.Entities.Product{Name="Антисептик для рук",Form="Флакон 100 мл",Price=25,Stock=32,Category=hygiene,Manufacturer=manufacturer,Description="Средство для гигиены рук. Следуйте указаниям на упаковке."},
   new Domain.Entities.Product{Name="Влажные салфетки",Form="Упаковка 60 шт.",Price=23,Stock=65,Category=hygiene,Manufacturer=manufacturer,Description="Гигиенические салфетки для повседневного применения."},
   new Domain.Entities.Product{Name="Контейнер для таблеток",Form="7 отделений",Price=35,Stock=20,Category=personal,Manufacturer=manufacturer,Description="Органайзер для хранения на неделю."},
   new Domain.Entities.Product{Name="Эластичный бинт",Form="1 шт.",Price=34,Stock=24,Category=firstAid,Manufacturer=manufacturer,Description="Эластичный перевязочный материал."},
   new Domain.Entities.Product{Name="Маски одноразовые",Form="Упаковка 10 шт.",Price=18,Stock=90,Category=hygiene,Manufacturer=manufacturer,Description="Одноразовые гигиенические маски."});
  await db.SaveChangesAsync();
 }
}
if(!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name:"default",pattern:"{controller=Home}/{action=Index}/{id?}");
app.Run();
