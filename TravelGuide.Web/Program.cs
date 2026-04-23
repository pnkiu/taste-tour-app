using Microsoft.EntityFrameworkCore;
using TravelGuide.Web.Data; // Đổi TravelGuide.Web thành tên Project của bạn nếu khác

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(); // Bật dịch vụ SignalR
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================================================
// --- CẤU HÌNH DBCONTEXT (MySQL) ---
// =======================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ----------------------------------------------
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<TravelGuide.Web.Hubs.DeviceHub>("/deviceHub");

app.Use(async (context, next) =>
{
    // Lấy IP của người gọi
    var clientIp = context.Connection.RemoteIpAddress?.ToString();
    var apiPath = context.Request.Path;

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"[CO KET NOI] Thiet bi có IP: {clientIp} vua goi du lieu tu {apiPath}");
    Console.ResetColor(); // Trả lại màu trắng mặc định

    await next();
});
app.Urls.Add("http://0.0.0.0:5220");
app.Run();