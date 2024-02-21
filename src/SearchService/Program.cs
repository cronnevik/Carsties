using System.Net;
using MassTransit;
using Polly;
using Polly.Extensions.Http;
using SearchService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());
builder.Services.AddMassTransit(x =>
{
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

    // prefixed with search: search-auction-created. False means to not include namespace in name
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false)); 

    x.UsingRabbitMq((context, cfg) => 
    {
        // retry policy for specific endpoints
        cfg.ReceiveEndpoint("search-auction-created", e => 
        {
            e.UseMessageRetry(r => r.Interval(5, 5));
            // which consumer retry policy is configured for
            e.ConfigureConsumer<AuctionCreatedConsumer>(context);
        });
        cfg.ConfigureEndpoints(context);
    });
});

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