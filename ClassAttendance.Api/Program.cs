using ClassAttendance.Application.Interfaces.Repositories;
using ClassAttendance.Infrastructure.Data;
using ClassAttendance.Infrastructure.Persistence;
using ClassAttendance.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ILectureRepository, LectureRepository>();    


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<DataContext>();
    await Seed.SeedAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
