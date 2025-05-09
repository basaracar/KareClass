using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KareClass.Data;
using KareClass.Models;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
#if DEBUG
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
#else
var connectionString = builder.Configuration.GetConnectionString("ServerConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
#endif
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// // Veritabanı işlemleri
// using (var scope = app.Services.CreateScope())
// {

//     var services = scope.ServiceProvider;
//     var context = services.GetRequiredService<ApplicationDbContext>();
//     var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
//     context.Database.Migrate();
//     await context.SeedCourses();
//     await context.SeedDepartments();
//     await context.SeedClasses();
//     await context.SeedTimeSlots();
//     await context.SeedAdminUser(userManager);
// }

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
app.MapRazorPages();

app.Run();
