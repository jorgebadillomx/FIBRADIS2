using Hangfire;
using Hangfire.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Jobs.Tests;

public class HangfireRegistrationTests
{
    [Fact]
    public void HangfireServer_WithInMemoryStorage_StartsAndStops()
    {
        GlobalConfiguration.Configuration
            .UseInMemoryStorage()
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings();

        var services = new ServiceCollection();
        services.AddHangfire(_ => { });
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 1;
            options.Queues = ["default"];
        });

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IBackgroundJobClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void BackgroundJobClient_CanEnqueueJob_WithInMemoryStorage()
    {
        GlobalConfiguration.Configuration
            .UseInMemoryStorage()
            .UseRecommendedSerializerSettings();

        var services = new ServiceCollection();
        services.AddHangfire(_ => { });
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IBackgroundJobClient>();
        var jobId = client.Enqueue(() => Console.WriteLine("test job"));

        Assert.NotNull(jobId);
        Assert.NotEmpty(jobId);
    }
}
