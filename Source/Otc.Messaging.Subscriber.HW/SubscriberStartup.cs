using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Otc.AspNetCore.ApiBoot;
using Otc.Extensions.Configuration;
using Otc.HostedWorker;
using Otc.Messaging.RabbitMQ.Configurations;
using System.ComponentModel;

namespace Otc.Messaging.Subscriber.HW
{
    /// <summary>
    /// WebHostedWorker Startup class. 
    /// </summary>
    public abstract class SubscriberStartup<TMessage, TMessageHandler> : ApiBootStartup
        where TMessageHandler : class, IMessageHandler<TMessage>
    {

        protected SubscriberStartup(IConfiguration configuration)
            : base(configuration)
        {

        }

        /// <summary>
        /// Consider not override <see cref="ConfigureApiServices"/>,
        /// use <see cref="ConfigureSubscriberServices"/> for service registration instead.
        /// </summary>
        /// <param name="services"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void ConfigureApiServices(IServiceCollection services)
        {
            ConfigureWebHostedWorkerServices(services);
            services.AddRabbitMQ(Configuration.SafeGet<RabbitMQConfiguration>());
            services.AddTypedMessaging();
            services.AddSingleton(Configuration.SafeGet<SubscriberHWConfiguration>());
            ConfigureSubscriberServices(services);
        }

        private void ConfigureWebHostedWorkerServices(IServiceCollection services)
        {
            var hostedWorkerConfiguration = Configuration.SafeGet<HostedWorkerConfiguration>();
            
            // TODO: integrate execution time guard to Otc.Messaging
            hostedWorkerConfiguration.WorkerTimeoutInSeconds = 604800;  // 1 semana

            hostedWorkerConfiguration.MaxConsecutiveErrors = 0;
            hostedWorkerConfiguration.WorkOnStartup = true;
            services.AddHostedWorker<SubscriberHostedWorker<TMessage>>(hostedWorkerConfiguration);
            services.AddScoped<IMessageHandler<TMessage>, TMessageHandler>();
        }

        protected abstract void ConfigureSubscriberServices(IServiceCollection services);
    }
}
