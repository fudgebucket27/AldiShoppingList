using AldiApi;
using Azure;

var builder = WebApplication.CreateBuilder(args);

var dataFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataFolder);          // make sure it exists
var dbPath = Path.Combine(dataFolder, "aldi.db");

var connectionString = $"Data Source={dbPath};Cache=Shared;";

builder.Configuration["ConnectionStrings:Aldi"] =
    $"Data Source={dbPath};Cache=Shared;";

string[] arguments = new string[] { connectionString };

await AldiScraper.Program.Main(arguments);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IProductService, ProductService>();

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
