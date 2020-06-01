using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace Otc.Messaging.RabbitMQ
{
    public class RabbitMQChannelEventsHandler
    {
        private readonly IModel channel;
        private readonly ILogger logger;
        private readonly ushort MissingRoute = 312;

        private bool missingRoute = false;

        public RabbitMQChannelEventsHandler(IModel channel, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<RabbitMQChannelEventsHandler>();

            channel.ModelShutdown += ModelShutdownEvent;
            channel.CallbackException += CallbackExceptionEvent;
            channel.BasicReturn += BasicReturnEvent;
            channel.BasicRecoverOk += BasicRecoverOkEvent;
            channel.BasicNacks += BasicNacksEvent;

            this.channel = channel;
        }

        private void ModelShutdownEvent(object sender, ShutdownEventArgs ea)
        {
            logger.LogDebug($"{nameof(ModelShutdownEvent)}: Channel " +
                $"{channel.ChannelNumber} closed due to {channel.CloseReason}");
        }

        private void CallbackExceptionEvent(object sender, CallbackExceptionEventArgs ea)
        {
            // this event is triggered when any exception occurs inside consumer execution
            // chain, so we log it as error
            logger.LogError(123, ea.Exception, $"{nameof(CallbackExceptionEvent)}: Channel " +
                $"{channel.ChannelNumber} caught an exception inside consumer with nessage " +
                "{MessageConsumerException} ", ea.Exception.Message);
        }

        private void BasicReturnEvent(object sender, BasicReturnEventArgs ea)
        {
            logger.LogDebug($"{nameof(BasicReturnEvent)}: Channel " +
                $"{channel.ChannelNumber} returned message " +
                "{MessageId} with reply {MessageReturnedReason}",
                ea.BasicProperties.MessageId, $"{ea.ReplyCode}-{ea.ReplyText}");

            missingRoute = (ea.ReplyCode == MissingRoute);
        }

        private void BasicRecoverOkEvent(object sender, EventArgs ea)
        {
            logger.LogDebug($"{nameof(BasicRecoverOkEvent)}: Channel " +
                $"{channel.ChannelNumber} recovered ok");
        }

        private void BasicNacksEvent(object sender, BasicNackEventArgs ea)
        {
            logger.LogDebug($"{nameof(BasicNacksEvent)}: Channel " +
                $"{channel.ChannelNumber} sent nack for message " +
                $"corresponding to DeliveryTag {ea.DeliveryTag}");
        }

        internal bool IsRouteMissing
        {
            get
            {
                if (missingRoute)
                {
                    missingRoute = false;
                    return true;
                }
                return false;
            }
        }
    }
}