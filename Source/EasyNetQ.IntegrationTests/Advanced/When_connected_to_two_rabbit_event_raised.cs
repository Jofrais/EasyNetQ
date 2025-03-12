using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_connected_to_two_rabbit_event_raised : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus_1;
    private readonly IBus bus_2;

    public When_connected_to_two_rabbit_event_raised(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        var stopwatch = Stopwatch.StartNew();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1;publisherConfirms=True;virtualHost=test", serviceKeyName: "test");
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1;publisherConfirms=True;virtualHost=test2", serviceKeyName: "test2");

        serviceProvider = serviceCollection.BuildServiceProvider();
        stopwatch.Stop();
        Debug.WriteLine($"Time to build service provider: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.Restart();
        bus_1 = serviceProvider.GetKeyedService<IBus>("test");
        bus_2 = serviceProvider.GetKeyedService<IBus>("test2");
        stopwatch.Stop();
        Debug.WriteLine($"Time to get bus: {stopwatch.ElapsedMilliseconds}ms");
    }

    public void Dispose()
    {
        serviceProvider?.Dispose();
    }

    [Fact]
    public async Task Test_WithTwoConnection()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));


        var mre = new ManualResetEventSlim(false);
        var mre2 = new ManualResetEventSlim(false);
        bus_1.Advanced.Connected += (_, _) => mre.Set();
        bus_2.Advanced.Connected += (_, _) => mre2.Set();

        await bus_1.Advanced.ExchangeDeclareAsync(Guid.NewGuid().ToString("N"), cancellationToken: cts.Token);
        await bus_2.Advanced.ExchangeDeclareAsync(Guid.NewGuid().ToString("N"), cancellationToken: cts.Token);
    }
}
