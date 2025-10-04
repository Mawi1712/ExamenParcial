using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => 
    options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>() 
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

await SeedDataAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

static async Task SeedDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Coordinador"))
        {
            await roleManager.CreateAsync(new IdentityRole("Coordinador"));
        }

        if (!await roleManager.RoleExistsAsync("Estudiante"))
        {
            await roleManager.CreateAsync(new IdentityRole("Estudiante"));
        }

        var coordinadorEmail = "coordinador@universidad.edu";
        var coordinador = await userManager.FindByEmailAsync(coordinadorEmail);
        
        if (coordinador == null)
        {
            coordinador = new IdentityUser
            {
                UserName = coordinadorEmail,
                Email = coordinadorEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(coordinador, "Coordinador123!");
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(coordinador, "Coordinador");
            }
        }

        if (!context.Cursos.Any())
        {
            var cursos = new List<PortalAcademico.Models.Curso>
            {
                new PortalAcademico.Models.Curso
                {
                    Codigo = "CS101",
                    Nombre = "Introducción a la Programación",
                    Creditos = 4,
                    CupoMaximo = 30,
                    HorarioInicio = new TimeSpan(8, 0, 0),
                    HorarioFin = new TimeSpan(10, 0, 0),
                    Activo = true
                },
                new PortalAcademico.Models.Curso
                {
                    Codigo = "MAT201",
                    Nombre = "Cálculo Diferencial",
                    Creditos = 5,
                    CupoMaximo = 25,
                    HorarioInicio = new TimeSpan(10, 0, 0),
                    HorarioFin = new TimeSpan(12, 0, 0),
                    Activo = true
                },
                new PortalAcademico.Models.Curso
                {
                    Codigo = "FIS301",
                    Nombre = "Física General",
                    Creditos = 4,
                    CupoMaximo = 20,
                    HorarioInicio = new TimeSpan(14, 0, 0),
                    HorarioFin = new TimeSpan(16, 0, 0),
                    Activo = true
                },
                new PortalAcademico.Models.Curso
                {
                    Codigo = "BD401",
                    Nombre = "Bases de Datos",
                    Creditos = 4,
                    CupoMaximo = 28,
                    HorarioInicio = new TimeSpan(16, 0, 0),
                    HorarioFin = new TimeSpan(18, 0, 0),
                    Activo = true
                }
            };

            context.Cursos.AddRange(cursos);
            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al ejecutar el seed de datos iniciales");
    }
}