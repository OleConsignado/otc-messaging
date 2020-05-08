namespace Otc.Messaging.RabbitMQ.Configurations
{
    /// <summary>
    /// Sets the <see cref="RabbitMQSubscription"/> behavior when message handling throws 
    /// an exception.
    /// </summary>
    public enum MessageHandlerErrorBehavior
    {
        RejectOnFistDelivery = 0,
        RejectOnRedelivery = 1,
    }
}
