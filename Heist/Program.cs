using Heist.Domain;
using Heist.Realtime;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);
var types= new FileExtensionContentTypeProvider();
types.Mappings[".glb"] = "model/gltf-binary";

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR().AddJsonProtocol(o => 
{
    o.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.PayloadSerializerOptions.Converters.Add( new System.Text.Json.Serialization.JsonStringEnumConverter());
});
//JSON protocol helps with casing
builder.Services.AddSingleton<RoomRegistry>();
builder.Services.AddHostedService<GameLoop>();

//Level Catalog register
builder.Services.AddSingleton(sp => new LevelCatalog(
    Path.Combine(sp.GetRequiredService<IWebHostEnvironment>().WebRootPath, "levels"),
    sp.GetRequiredService<ILogger<LevelCatalog>>()));

var app = builder.Build();
app.Services.GetRequiredService<LevelCatalog>(); //force load, crash loud if no levels exist

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseDefaultFiles(); //lets use index.html for root
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = types});   //serve wwwroot folder, added glb mime type for 3D models

app.MapControllers();
app.MapHub<GameHub>("/gamehub");

//JSON TEST
//var json = File.ReadAllText("wwwroot/levels/test.json");
//var lvl = System.Text.Json.JsonSerializer.Deserialize<LevelFile>(
//    json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
//Console.WriteLine($"walls={lvl.Walls.Count} spawns={lvl.Spawns.Count} objX={lvl.Objective.X}");

app.Run();
