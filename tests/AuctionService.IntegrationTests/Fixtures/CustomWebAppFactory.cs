using AuctionService.Data;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WebMotions.Fake.Authentication.JwtBearer;

namespace AuctionService.IntegrationTests;

// Create a test instance of our Web application and add test services inside to be reused among the tests
public class CustomWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder().Build();
    public async Task InitializeAsync()
    {
        // Inside docker, this is going to start a running instance of a test container database server
        await _postgreSqlContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services => {
            // cannot override the db context, so it has to be removed
            services.RemoveDbContext<AuctionDbContext>();
            // and replaced with the test db container
            services.AddDbContext<AuctionDbContext>(options => 
            {
                options.UseNpgsql(_postgreSqlContainer.GetConnectionString());
            });

            // Replace ReabbitMq - method by Masstransit
            services.AddMassTransitTestHarness();

            // migrate db schema and add test data
            services.EnsureCreated<AuctionDbContext>();

            // handle authentication
            services.AddAuthentication(FakeJwtBearerDefaults.AuthenticationScheme)
                .AddFakeJwtBearer(opt =>
                {
                    opt.BearerValueType = FakeJwtBearerBearerValueType.Jwt;
                });
        });
    }

    Task IAsyncLifetime.DisposeAsync() => _postgreSqlContainer.DisposeAsync().AsTask();
}
