using System;
using System.Runtime.Serialization;

namespace Otc.Messaging.Abstractions.Exceptions
{
    /// <summary>
    /// Base exception for publish operations
    /// </summary>
    [Serializable]
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
        }

        public PublishException(string message, string topic, string queue,
            byte[] messageBytes, Exception innerException)
            : base(message, innerException)
        {
            Topic = topic;
            Queue = queue;

            // limit message's length for logging purposes
            var length = Math.Min(messageBytes.Length, 4096);
            MessageBytes = new byte[length];
            Array.Copy(messageBytes, MessageBytes, length);
        }

        protected PublishException(SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}