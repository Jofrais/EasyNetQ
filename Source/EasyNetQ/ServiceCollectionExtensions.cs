using EasyNetQ.ChannelDispatcher;
using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace EasyNetQ;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterDefaultServices(
        this IServiceCollection services,
        Func<IServiceProvider, ConnectionConfiguration> connectionConfigurationFactory
    )
    {
        AddCommonServices(services);

        services.TryAddSingleton<IEventBus, EventBus>();

        services.TryAddSingleton(s =>
        {
            var configuration = connectionConfigurationFactory(s);

            configuration.SetDefaultProperties();
            return configuration;
        });

        services.TryAddSingleton<IProducerConnection, ProducerConnection>();
        services.TryAddSingleton<IConsumerConnection, ConsumerConnection>();
        services.TryAddSingleton<IInternalConsumerFactory, InternalConsumerFactory>();
        services.TryAddSingleton<IConsumerFactory, ConsumerFactory>();

        services.TryAddSingleton<IPersistentChannelDispatcher, SinglePersistentChannelDispatcher>();
        services.TryAddSingleton<IPersistentChannelFactory, PersistentChannelFactory>();

        services.TryAddSingleton<IPublishConfirmationListener, PublishConfirmationListener>();
        services.TryAddSingleton<IConnectionFactory>(serviceProvider =>
        {
            var connectionConfiguration = serviceProvider.GetRequiredService<ConnectionConfiguration>();
            return ConnectionFactoryFactory.CreateConnectionFactory(connectionConfiguration);
        });
        services.TryAddSingleton<IConsumeErrorStrategy, DefaultConsumeErrorStrategy>();

        services.TryAddSingleton<IMessageDeliveryModeStrategy, MessageDeliveryModeStrategy>();
        services.TryAddSingleton<IExchangeDeclareStrategy, DefaultExchangeDeclareStrategy>();
        services.TryAddSingleton<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>();
        services.TryAddSingleton<IPullingConsumerFactory, PullingConsumerFactory>();
        services.TryAddSingleton<ISendReceive, DefaultSendReceive>();
        services.TryAddSingleton<IAdvancedBus, RabbitAdvancedBus>();
        services.TryAddSingleton<IPubSub, DefaultPubSub>();
        services.TryAddSingleton<IRpc, DefaultRpc>();
        services.TryAddSingleton<IBus, RabbitBus>();
        return services;
    }

    private static void AddCommonServices(IServiceCollection services)
    {
        services.TryAddSingleton<IConnectionStringParser>(
            _ => new CompositeConnectionStringParser(new AmqpConnectionStringParser(), new ConnectionStringParser())
        );
        services.TryAddSingleton<ISerializer>(_ => new ReflectionBasedNewtonsoftJsonSerializer());
        services.TryAddSingleton<IConventions, Conventions>();
        services.TryAddSingleton<ITypeNameSerializer, DefaultTypeNameSerializer>();
        services.TryAddSingleton<ProducePipelineBuilder>(_ => new ProducePipelineBuilder().UseProduceInterceptors());
        services.TryAddSingleton<ConsumePipelineBuilder>(_ =>
            new ConsumePipelineBuilder().UseConsumeErrorStrategy().UseConsumeInterceptors());
        services.TryAddSingleton<ICorrelationIdGenerationStrategy, DefaultCorrelationIdGenerationStrategy>();
        services.TryAddSingleton<IMessageSerializationStrategy, DefaultMessageSerializationStrategy>();


        services.TryAddSingleton<AdvancedBusEventHandlers>(_ => new AdvancedBusEventHandlers());
        services.TryAddSingleton<IErrorMessageSerializer, DefaultErrorMessageSerializer>();

        services.TryAddSingleton<IHandlerCollectionFactory, HandlerCollectionFactory>();

        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.TryAddSingleton<ILoggerFactory>(new NullLoggerFactory());
    }

    public static IServiceCollection RegisterDefaultServicesKey(
        this IServiceCollection services,
        string name,
        Func<IServiceProvider, ConnectionConfiguration> connectionConfigurationFactory
    )
    {
        AddCommonServices(services);

        services.TryAddKeyedSingleton(name, (s, _) =>
        {
            var configuration = connectionConfigurationFactory(s);

            configuration.SetDefaultProperties();
            return configuration;
        });

        services.TryAddKeyedSingleton<IEventBus>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<EventBus>(name));

        services.TryAddKeyedSingleton<IConnectionFactory>(name, (serviceProvider, skName) =>
        {
            var connectionConfiguration = serviceProvider.GetRequiredKeyedService<ConnectionConfiguration>(skName);
            return ConnectionFactoryFactory.CreateConnectionFactory(connectionConfiguration);
        });

        services.TryAddKeyedSingleton<IPersistentChannelDispatcher>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<SinglePersistentChannelDispatcher>(name));

         services.TryAddKeyedSingleton<IPersistentChannelFactory>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<PersistentChannelFactory>(name));
        services.TryAddKeyedSingleton<IProducerConnection>(name,
            (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<ProducerConnection>(name));

        services.TryAddKeyedSingleton<IConsumerConnection>(name,
            (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<ConsumerConnection>(name));

        services.TryAddKeyedSingleton<IInternalConsumerFactory>(name,
            (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<InternalConsumerFactory>(name));

        services.TryAddKeyedSingleton<IConsumerFactory>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<ConsumerFactory>(name));
        services.TryAddKeyedSingleton<IPullingConsumerFactory>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<PullingConsumerFactory>(name));
        services.TryAddKeyedSingleton<IPublishConfirmationListener>(name,  (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<PublishConfirmationListener>(name));
        services.TryAddKeyedSingleton<IExchangeDeclareStrategy>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<DefaultExchangeDeclareStrategy>(name));
        services.TryAddKeyedSingleton<IMessageDeliveryModeStrategy>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<MessageDeliveryModeStrategy>(name));
        services.TryAddKeyedSingleton<IConsumeErrorStrategy>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<DefaultConsumeErrorStrategy>(name));

        services.TryAddKeyedSingleton<IScheduler>(name,
            (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<DeadLetterExchangeAndMessageTtlScheduler>(name));

        services.TryAddKeyedSingleton<ISendReceive>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<DefaultSendReceive>(name));

        services.TryAddKeyedSingleton<IAdvancedBus>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<RabbitAdvancedBus>(name));

        services.TryAddKeyedSingleton<IPubSub>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<DefaultPubSub>(name));

        services.TryAddKeyedSingleton<IRpc>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<DefaultRpc>(name));

        services.TryAddKeyedSingleton<IBus>(name, (serviceProvider, _) => serviceProvider.CreateInstanceKeyed<RabbitBus>(name));
        return services;
    }

    private static T CreateInstanceKeyed<T>(this IServiceProvider services, string key)
    {
        // Recherche du constructeur avec le plus de paramètres
        var constructors = typeof(T).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length);

        var constructor = constructors.FirstOrDefault();
        if (constructor == null)
            throw new InvalidOperationException($"Aucun constructeur trouvé pour {typeof(T).Name}");

        // Recherche des paramètres
        var parameters = constructor.GetParameters();
        var parameterInstances = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var parameterType = parameter.ParameterType;

            // Vérifier si le type est enregistré avec une clé ou non
            if (IsConnectionSpecificType(parameterType))
            {
                // Obtenir le service avec la clé
                var serviceParameter = services.GetKeyedServices(parameterType, key);
                if(serviceParameter == null)
                    throw new InvalidOperationException($"Aucun service trouvé pour {parameterType.Name} avec la clé {key}");

                var serviceList = serviceParameter.ToList();

                if(serviceList.Count > 1)
                    throw new InvalidOperationException($"Plusieurs services trouvés pour {parameterType.Name} avec la clé {key}");

                parameterInstances[i] = serviceList.FirstOrDefault();
            }
            else
            {
                // Sinon, on prend le service sans la clé
                var service = services.GetRequiredService(parameterType);

                parameterInstances[i] = service;
            }
        }

        return (T)constructor.Invoke(parameterInstances);
    }

    // Méthode pour déterminer si un type est spécifique à une connexion
    private static bool IsConnectionSpecificType(Type type)
    {
        // Liste des types qui sont spécifiques à une connexion
        var connectionSpecificTypes = new[]
        {
            typeof(ConnectionConfiguration),
            typeof(IConnectionFactory),
            typeof(IPersistentChannelDispatcher),
            typeof(IProducerConnection),
            typeof(IConsumerConnection),
            typeof(IConsumerFactory),
            typeof(IPullingConsumerFactory),
            typeof(IInternalConsumerFactory),
            typeof(IPersistentChannelFactory),
            typeof(IPublishConfirmationListener),
            typeof(IExchangeDeclareStrategy),
            typeof(IMessageDeliveryModeStrategy),
            typeof(IConsumeErrorStrategy),
            typeof(IEventBus),
            typeof(IAdvancedBus),
            typeof(IPubSub),
            typeof(IRpc),
            typeof(ISendReceive),
            typeof(IScheduler),
            typeof(IBus)
        };

        return connectionSpecificTypes.Any(t => t.IsAssignableFrom(type));
    }
}
