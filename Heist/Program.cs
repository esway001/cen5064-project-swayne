using Heist.Domain;
using Heist.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR().AddJsonProtocol( o=>
    o.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
    //JSON protocol helps with casing
builder.Services.AddSingleton<GameRegistry>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseDefaultFiles(); //lets use index.html for root
app.UseStaticFiles();   //serve wwwroot folder

app.MapControllers();
app.MapHub<GameHub>("/gamehub");

app.Run();
