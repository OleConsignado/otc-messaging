using System;
using System.Runtime.Serialization;

namespace Otc.Messaging.Abstractions.Exceptions
{
    /// <summary>
    /// Base exception for Messaging
    /// </summary>
    [Serializable]
    public abstract class MessagingException : Exception
    {
        protected MessagingException(string message)
            : base(message)
        {
        }

        protected MessagingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MessagingException(SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}