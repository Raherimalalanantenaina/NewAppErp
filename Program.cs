using NewAppErp.Services.Login;
using NewAppErp.Services.Employer;
using NewAppErp.Services.Util;
using NewAppErp.Services.Salary.SalarySlips;
using NewAppErp.Services.Import;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configuration commune
var baseUrl = builder.Configuration["NewAppErp:BaseUrl"];
var defaultTimeout = TimeSpan.FromMinutes(200); // ✅ Timeout augmenté ici

// Services HTTP avec timeout configuré
builder.Services.AddHttpClient<ILoginService, LoginService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = defaultTimeout;
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<IEmployeeService, EmployeeService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = defaultTimeout;
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<IUtilService, UtilService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = defaultTimeout;
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<ISalarySlipService, SalarySlipService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = defaultTimeout;
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<IImportService, ImportService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = defaultTimeout; // ✅ Timeout critique ici
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Enregistrements Scoped
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IUtilService, UtilService>();
builder.Services.AddScoped<ISalarySlipService, SalarySlipService>();
builder.Services.AddScoped<IImportService, ImportService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2); 
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Authentification
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Login/Index";
        options.AccessDeniedPath = "/Login/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Login/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
