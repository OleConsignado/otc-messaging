namespace Otc.Messaging.Typed.Abstractions
{
    /// <summary>
    /// Message objects serialization and deserialization.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serializes a message object into a byte[].
        /// </summary>
        /// <typeparam name="T">The type of message object.</typeparam>
        /// <param name="message">The message object.</param>
        /// <returns>Representation of message as byte[].</returns>
        byte[] Serialize<T>(T message);

        /// <summary>
        /// Deserializes a representation of the message object comming in a byte[].
        /// </summary>
        /// <typeparam name="T">The type of message object.</typeparam>
        /// <param name="message">Representation of message in a byte[].</param>
        /// <returns>The message object.</returns>
        T Deserialize<T>(byte[] message);
    }
}
