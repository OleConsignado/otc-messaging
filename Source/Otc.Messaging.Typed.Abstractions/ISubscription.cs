using Otc.Messaging.Abstractions;

namespace Otc.Messaging.Typed.Abstractions
{
    /// <inheritdoc cref="ISubscription"/>
    public interface ISubscription<out T> : ISubscription
    {
    }
}
