using FinCtrl.Backend.Core.RestAPI.BL.Implementation;
using FinCtrl.DAL;
using FinCtrl.DAL.Implementation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// data layer access
builder.Services.AddDbContext<FinCtrlDBContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultDatabase")));

builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddTransient<PaymentSourceRepository>(); // TODO service types. Is it good?
builder.Services.AddScoped<PaymentRepository>();

// domain logic
builder.Services.AddScoped<ExcelFileLoader>();

// some nedeed shit
builder.Services.AddControllers().AddJsonOptions(x =>
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
//// -- mapping dto shit
//var mapperConfig = new MapperConfiguration(mc =>
//{
//    mc.AddProfile(new DTOMappingProfile());
//});
//builder.Services.AddSingleton(mapperConfig.CreateMapper());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
