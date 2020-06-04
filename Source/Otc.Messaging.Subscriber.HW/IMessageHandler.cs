using Otc.Messaging.Abstractions;
using System.Threading.Tasks;

namespace Otc.Messaging.Subscriber.HW
{
    public interface IMessageHandler<in TMessage>
    {
        Task HandleAsync(TMessage message, IMessageContext messageContext);
    }
}