using Microsoft.EntityFrameworkCore;
using waranon_csi402.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<Csi402dbContext>(
    options => options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// ลงทะเบียนระบบอัตโนมัติทำงานเบื้องหลัง (Auto-cancel & Restock)
builder.Services.AddHostedService<waranon_csi402.Services.OrderAutomationService>();

var app = builder.Build();

// Seed ข้อมูลเสื้อผ้าเริ่มต้นหากฐานข้อมูลยังว่างเปล่า
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<Csi402dbContext>();
        DbInitializer.EnsureExtraTables(db); // สร้างตาราง PromotionProducts, Banners ถ้ายังไม่มี
        DbInitializer.Seed(db);
    }
    catch
    {
        // ข้ามหากเชื่อมต่อไม่ได้ ให้รันผ่าน MySQL Workbench แทนได้
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
