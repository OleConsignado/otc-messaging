using System;

namespace Otc.Messaging.Abstractions.Exceptions
{
    /// <summary>
    /// Base exception for publish operations
    /// </summary>
    public class PublishException : MessagingException
    {
        public string Topic { get; }
        public string Queue { get; }
        public byte[] MessageBytes { get; }

        public PublishException(string message, string topic, string queue, byte[] messageBytes)
            : this(message, topic, queue, messageBytes, null)
        {
        }

        public PublishException(string topic, string queue,
            byte[] messageBytes,Exception innerException)
            : this($"Publish operation to topic '{topic}' and queue '{queue}' failed, " +
                  $"see innerException.", topic, queue, messageBytes, innerException)
        {
            Topic = topic;
            Queue = queue;
            MessageBytes = messageBytes;
        }

        public PublishException(string message, string topic, string queue,
            byte[] messageBytes, Exception innerException)
            : base(message, innerException)
        {
            Topic = topic;
            Queue = queue;
            MessageBytes = messageBytes;
        }
    }
}