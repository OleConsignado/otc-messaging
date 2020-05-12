using Otc.Messaging.Abstractions;
using Otc.Messaging.Typed.Abstractions;
using System;

namespace Otc.Messaging.Typed
{
    public class Publisher<T> : IPublisher<T>
    {
        private readonly IPublisher publisher;
        private readonly ISerializer serializer;

        public Publisher(IMessaging messaging, ISerializer serializer)
        {
            publisher = messaging?.CreatePublisher() ??
                throw new ArgumentNullException(nameof(messaging));

            this.serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public void Publish(T message, string topic, string queue = null, string messageId = null)
        {
            var messageBytes = serializer.Serialize(message);
            publisher.Publish(messageBytes, topic, queue, messageId);
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    publisher.Dispose();
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