using System;
using System.Runtime.Serialization;

namespace Otc.Messaging.Abstractions.Exceptions
{
    /// <summary>
    /// Base exception for EnsureTopology operation
    /// </summary>
    [Serializable]
    public class EnsureTopologyException : MessagingException
    {
        public string Name { get; }

        public EnsureTopologyException(string name, Exception innerException)
            : base($"EnsureTopology operation named '{name}' failed, " +
                  $"see innerException.", innerException)
        {
            Name = name;
        }

        protected EnsureTopologyException(SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}