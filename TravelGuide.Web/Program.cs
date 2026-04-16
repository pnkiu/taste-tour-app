using Microsoft.AspNetCore.Authentication.Cookies; // 1. KHAI BÁO THƯ VIỆN BẢO MẬT TẠI ĐÂY
using Microsoft.EntityFrameworkCore;
using TravelGuide.Web.Data; // Đổi TravelGuide.Web thành tên Project của bạn nếu khác

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================================================
// --- 2. CẤU HÌNH BẢO MẬT (ĐĂNG NHẬP BẰNG COOKIE) ---
// =======================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Đường dẫn sẽ chuyển tới nếu chưa đăng nhập
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(1); // Thời gian nhớ đăng nhập
    });

// =======================================================
// --- BƯỚC 3: THÊM CẤU HÌNH DBCONTEXT Ở ĐÂY (ĐÃ ĐỔI SANG MYSQL) ---
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

// =======================================================
// --- 4. GẮN Ổ KHÓA VÀO HỆ THỐNG (BẮT BUỘC ĐÚNG THỨ TỰ NÀY) ---
// =======================================================
app.UseAuthentication(); // <-- KIỂM TRA GIẤY TỜ (Xác thực)
app.UseAuthorization();  // <-- XEM CÓ QUYỀN KHÔNG (Phân quyền)

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();