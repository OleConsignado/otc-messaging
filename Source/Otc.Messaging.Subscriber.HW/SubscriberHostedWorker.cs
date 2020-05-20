using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Otc.HostedWorker.Abstractions;
using Otc.Messaging.Abstractions;

namespace Otc.Messaging.Subscriber.HW
{
    public class SubscriberHostedWorker<TMesssage> : IHostedWorker
    {
        private readonly ILogger logger;
        private readonly IMessaging messaging;
        private readonly SubscriberHWConfiguration subscriberConfiguration;
        private readonly IMessageHandler<TMesssage> messageHandler;

        public bool HasPendingWork { get => true; set { _ = value; } }

        public SubscriberHostedWorker(ILoggerFactory loggerFactory, IMessaging messaging,
            SubscriberHWConfiguration subscriberConfiguration, IMessageHandler<TMesssage> messageHandler)
        {
            logger = loggerFactory?.CreateLogger<SubscriberHostedWorker<TMesssage>>() ?? 
                throw new System.ArgumentNullException(nameof(loggerFactory));
            this.messaging = messaging ?? 
                throw new ArgumentNullException(nameof(messaging));
            this.subscriberConfiguration = subscriberConfiguration ?? 
                throw new ArgumentNullException(nameof(subscriberConfiguration));
            this.messageHandler = messageHandler ?? 
                throw new ArgumentNullException(nameof(messageHandler));
        }

        public async Task WorkAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"{nameof(WorkAsync)} begin.");
            var subscriber = messaging.Subscribe<TMesssage>(HandleMessage, subscriberConfiguration.Queues);
            await subscriber.StartAsync(cancellationToken);

            logger.LogInformation($"{nameof(WorkAsync)} end.");
        }

        private void HandleMessage(TMesssage message, IMessageContext messageContext)
        {
            logger.LogInformation($"{nameof(HandleMessage)}: start {{@MessageContext}}", 
                messageContext);
            
            try
            {
                messageHandler.Handle(message, messageContext);
            }
            catch(Exception e)
            {
                logger.LogWarning(e, 
                    $"{nameof(HandleMessage)}: error {{@MessageContext}}", messageContext);
                throw;
            }

            logger.LogInformation($"{nameof(HandleMessage)}: success {{@MessageContext}}",
                messageContext);
        }
    }
}
