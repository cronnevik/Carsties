using System.Net;
using Polly;
using Polly.Extensions.Http;
using SearchService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();

// Make the application run and start without the DbInitialier to block
app.Lifetime.ApplicationStarted.Register(async () => {
    try
    {
        await DbInitializer.InitDb(app);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
});

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetPolicy()
    => HttpPolicyExtensions
        .HandleTransientHttpError() // Network failures (HttpRequestException) - HTTP 5XX or HTTP 408
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound) // simple example of another policy
        .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));