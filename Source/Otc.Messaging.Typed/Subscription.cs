using Otc.Messaging.Abstractions;
using Otc.Messaging.Typed.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Otc.Messaging.Typed
{
    public class Subscription<T> : ISubscription<T>
    {
        private readonly ISubscription subscription;

        public Subscription(IMessaging messaging, ISerializer serializer,
            Action<T, IMessageContext> handler, params string[] queues)
        {
            if (messaging is null)
            {
                throw new ArgumentNullException(nameof(messaging));
            }

            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            subscription = messaging.Subscribe((message, messageContext) =>
            {
                var typedMessage = serializer.Deserialize<T>(message);
                handler.Invoke(typedMessage, messageContext);
            }, queues);
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await subscription.StartAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public void Start()
        {
            subscription.Start();
        }

        /// <inheritdoc/>
        public void Stop()
        {
            subscription.Stop();
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    subscription.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}