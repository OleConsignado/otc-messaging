using Newtonsoft.Json;
using Otc.Messaging.Typed.Abstractions;
using System.Text;

namespace Otc.Messaging.Typed
{
    /// <summary>
    /// Basic implementation of <see cref="ISerializer"/>.
    /// </summary>
    public class DefaultSerializer : ISerializer
    {
        /// <inheritdoc/>
        /// <remarks>Objects are serialized to JSON strings.</remarks>
        public byte[] Serialize<T>(T message)
        {
            var messageString = JsonConvert.SerializeObject(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageString);
            return messageBytes;
        }

        /// <inheritdoc/>
        /// <remarks>Objects are deserialized from JSON strings.</remarks>
        public T Deserialize<T>(byte[] message)
        {
            var messageString = Encoding.UTF8.GetString(message);
            var messageType = JsonConvert.DeserializeObject<T>(messageString);
            return messageType;
        }
    }
}