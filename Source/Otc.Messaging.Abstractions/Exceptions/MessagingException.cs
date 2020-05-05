using System;

namespace Otc.Messaging.Abstractions.Exceptions
{
    /// <summary>
    /// Base exception for Messaging
    /// </summary>
    public class MessagingException : Exception
    {
        public MessagingException(string message)
            : base(message)
        {
        }

        public MessagingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}