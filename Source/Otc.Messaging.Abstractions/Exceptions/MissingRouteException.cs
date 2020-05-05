namespace Otc.Messaging.Abstractions.Exceptions
{
    /// <summary>
    /// Broker could not route a message published to specified topic and queue.
    /// </summary>
    public class MissingRouteException : PublishException
    {
        public MissingRouteException(string topic, string queue, byte[] messageBytes)
            : base($"Missing route trying to publish to topic '{topic}' and queue '{queue}'",
                  topic, queue, messageBytes)
        {
        }
    }
}