using Microsoft.OpenApi.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Spaghetti.Common.Configuration;
using Spaghetti.Common.Kafka;
using Spaghetti.Common.Models.Supabase;
using Spaghetti.Core.ExtensionMethod.Kafka;
using Spaghetti.IngestService.DTO;
using Supabase;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        });
});

builder.Services.AddEndpointsApiExplorer();

#region Services

#region Kafka

builder.Services.Configure<KafkaConsumerConfiguration>(builder.Configuration.GetSection(nameof(KafkaConsumerConfiguration)));
builder.Services.Configure<KafkaProducerConfiguration>(builder.Configuration.GetSection(nameof(KafkaProducerConfiguration)));
builder.Services.AddSingleton<KafkaProducer>();
builder.Services.AddLogging();
builder.Services.RegisterKafkaConsumers(typeof(Program).Assembly);

#endregion

#region Supabase

builder.Services.AddScoped<Supabase.Client>(_ =>
    new Supabase.Client(
        builder.Configuration["SupabaseConfig:SupabaseUrl"]!,
        builder.Configuration["SupabaseConfig:SupabaseKey"],
        new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        }));

#endregion

#endregion

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});


app.MapPost("/MangaInfoGeneric", async (ICollection<MangaInfoGenericRequest> request, KafkaProducer _kafkaProducer) =>
{
    _ = await _kafkaProducer.ProduceAsync("Manga-Generic", request);
});

app.MapPost("/MangaInfoDetail", async (ICollection<MangaInfoGeneric> request, KafkaProducer _kafkaProducer) =>
{
    _ = await _kafkaProducer.ProduceAsync("Manga-Detail", request);
});

app.MapPost("/SaveChapterImages", async (HttpContext httpContext) =>
{
    var form = httpContext.Request.Form;
    foreach (var formFile in form.Files)
    {
        using var ms = new MemoryStream();
        await formFile.CopyToAsync(ms);
        byte[] byteArray = ms.ToArray();

        using (var image = Image.Load(byteArray))
        {
            var imageEncoder = new JpegEncoder
            {
                Quality = 75,  // Adjust the quality parameter to reduce the file size
            };

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(image.Width / 2, image.Height / 2),  // Adjust the size parameters to reduce the file size
                Mode = ResizeMode.Max,
            }));
            await image.SaveAsync($"image11{formFile.FileName}_.jpg", imageEncoder);
        }
    }
    /* var form = httpContext.Request.Form;
     using (var archiveStream = new FileStream("images1.zip", FileMode.Create))
     {
         using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
         {
             foreach (var formFile in form.Files)
             {
                 using var ms = new MemoryStream();
                 await formFile.CopyToAsync(ms);
                 byte[] byteArray = ms.ToArray();

                 var zipArchiveEntry = archive.CreateEntry(formFile.FileName, CompressionLevel.Optimal);
                 using (var zipStream = zipArchiveEntry.Open())
                 {
                     zipStream.Write(byteArray, 0, byteArray.Length);
                 }
             }
         }
     }*/

});

/*app.MapGet("/ProduceNotification123", async () =>
{
    var client = new SocketIOClient.SocketIO("http://localhost:3000");

    client.OnConnected += async (sender, e) =>
    {
        // Emit a string
        //await client.EmitAsync("hi", "socket.io");

        // Emit a string and an object
        var dto = new KafkaMessage { MessageId = "123", MessageContent = "321"};
        await client.EmitAsync("message", dto);
    };
    await client.ConnectAsync();
    *//*var client = new SocketIO()
    socket.On(Socket.EVENT_CONNECT, () =>
    {
        socket.Emit("message", "Hello from .NET!");
    });*//*
    return Task.CompletedTask;
});*/

// Configure the HTTP request pipeline.
app.UseCors("AllowAllOrigins");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.Run();

/*
    client.On("Message", response =>
    {
        // You can print the returned data first to decide what to do next.
        // output: ["hi client"]
        Console.WriteLine(response);

        string text = response.GetValue<string>();

        // The socket.io server code looks like this:
        // socket.emit('hi', 'hi client');
    });

    client.On("test", response =>
    {
        // You can print the returned data first to decide what to do next.
        // output: ["ok",{"id":1,"name":"tom"}]
        Console.WriteLine(response);

        // Get the first data in the response
        string text = response.GetValue<string>();
        // Get the second data in the response

        // The socket.io server code looks like this:
        // socket.emit('hi', 'ok', { id: 1, name: 'tom'});
    });
*/