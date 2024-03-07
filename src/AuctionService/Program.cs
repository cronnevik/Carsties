using AuctionService;
using AuctionService.Data;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionDbContext>(opt => {
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<AuctionDbContext>(o =>
    {
        // If Service bus is unavailable, then every 10 seconds it will check the outbox for undelivered messages
        o.QueryDelay = TimeSpan.FromSeconds(10);
        o.UsePostgres(); // MassTransit currently only support postgres and mongodb
        o.UseBusOutbox();
    });

    // enough to specify one consumer and the others are picked up as well
    x.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>(); 
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));

    x.UsingRabbitMq((context, cfg) => 
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", host =>
        {
            host.Username(builder.Configuration.GetValue("RabbitMq:Username", "guest"));
            host.Password(builder.Configuration.GetValue("RabbitMq:Password", "guest"));
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => 
    {
        // tells the resource server who the token was issued by
        options.Authority = builder.Configuration["IdentityServiceUrl"]; 
        // Because the identity server is running on http:
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters.ValidateAudience = false;
        options.TokenValidationParameters.NameClaimType = "username";
    });

// scoped to the http request that comes in to the API controller
// repository will be created and instansiated when the request first comes in and disposed of when the request leaves
builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<GrpcAuctionService>();

try
{
    DbInitializer.InitiDb(app);
}
catch (Exception e)
{
    Console.WriteLine(e);
}

app.Run();

// used for integration test (WebApplicationFactory)
public partial class Program {}