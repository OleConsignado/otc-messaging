#if NET451
using System;

namespace Otc.Messaging.RabbitMQ
{

    public static class Net451DateTimeOffsetExtensions
    {
        public static long ToUnixTimeMilliseconds(this DateTimeOffset dateTimeOffset)
        {
            var unixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, new TimeSpan(0));
            return (dateTimeOffset.UtcTicks - unixEpoch.UtcTicks) / TimeSpan.TicksPerMillisecond;
        }
    }

    public static class Net451DateTimeOffset
    {
        // https://stackoverflow.com/questions/249760/how-can-i-convert-a-unix-timestamp-to-datetime-and-vice-versa
        public static DateTimeOffset FromUnixTimeMilliseconds(long unixTimeMilliseconds)
        {
            // Unix timestamp is seconds past epoch
            DateTimeOffset dateTimeOffset = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, new TimeSpan(0));
            dateTimeOffset = dateTimeOffset.AddMilliseconds(unixTimeMilliseconds);

            return dateTimeOffset;
        }
    }

}
#endif
