using Otc.Messaging.Abstractions;

namespace Otc.Messaging.Subscriber.HW
{
    public interface IMessageHandler<in TMessage>
    {
        void Handle(TMessage message, IMessageContext messageContext);
    }
}