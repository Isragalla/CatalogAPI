using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration de la base de données selon l'environnement
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("ProductCatalogTestDb"));
}
else
{
    var connectionString =
        builder.Configuration.GetConnectionString("CatalogDbConnection");

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Activation de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Création automatique de la base de données
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Healthcheck
app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow
    }))
    .WithName("HealthCheck");

// GET : récupérer tous les produits
app.MapGet("/api/get-all-products", async (AppDbContext db) =>
    await db.Products
        .AsNoTracking()
        .ToListAsync())
    .WithName("GetAllProducts");

// POST : ajouter un produit
app.MapPost("/api/create-product", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/get-product/{product.Id}",
        product
    );
})
.WithName("CreateProduct");

// GET : récupérer un produit par ID
// app.MapGet("/api/get-product/{id:int}", async (int id, AppDbContext db) =>
// {
//     var product = await db.Products.AsNoTracking()
//         .FirstOrDefaultAsync(p => p.Id == id);

//     if (product is null)
//     {
//         return Results.NotFound(new
//         {
//             message = $"Aucun produit trouvé avec l'identifiant {id}."
//         });
//     }

//     return Results.Ok(product);
// })
// .WithName("GetProductById");

// app.Run();

// public partial class Program { }
app.MapGet("/api/products/{id:int}", async (int id, AppDbContext db) =>
{
    var product = await db.Products
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id);

    if (product == null)
    {
        return Results.NotFound(new
        {
            message = $"Aucun produit trouvé avec l'identifiant {id}."
        });
    }

    return Results.Ok(product);
})
.WithName("GetProductById");

app.Run();

public partial class Program { }