using Mcba.AdminApi.Repositories;
using Mcba.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<McbaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(nameof(McbaContext))));

// Repository pattern: controllers depend on the interfaces, not on McbaContext.
builder.Services.AddScoped<IPayeeRepository, PayeeRepository>();
builder.Services.AddScoped<ILoginsRepository, LoginsRepository>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
