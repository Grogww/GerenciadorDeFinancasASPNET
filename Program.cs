using System.Globalization;
using GerenciadorDeFinancasASPNET.data;
using GerenciadorDeFinancasASPNET.Services;
using Microsoft.EntityFrameworkCore;

// Moeda e datas em pt-BR em toda a aplicação (views, ToString, model binding).
var ptBr = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBr;
CultureInfo.DefaultThreadCurrentUICulture = ptBr;

var builder = WebApplication.CreateBuilder(args);

// Caminho relativo vindo do appsettings, ancorado na raiz do app (ContentRootPath).
// Assim não depende do diretório de onde o processo foi iniciado.
var dbRelativePath = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' não configurada.");
var dbFullPath = Path.Combine(builder.Environment.ContentRootPath, dbRelativePath);
var connectionString = $"Data Source={dbFullPath}";

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<ITransactionImportService, TransactionImportService>();
builder.Services.AddScoped<ICategorizationService, CategorizationService>();
builder.Services.AddSingleton<IImportSessionService, ImportSessionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
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
