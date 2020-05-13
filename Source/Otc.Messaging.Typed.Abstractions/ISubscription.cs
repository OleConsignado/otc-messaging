using Otc.Messaging.Abstractions;

namespace Otc.Messaging.Typed.Abstractions
{
    /// <inheritdoc cref="ISubscription"/>
    /// <typeparam name="T">The message object type</typeparam>
    public interface ISubscription<out T> : ISubscription
    {
    }
}
