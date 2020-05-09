using Otc.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace Otc.Messaging.RabbitMQ.Tests
{
    public class MessageHandler
    {
        public ITestOutputHelper OutputHelper;
        public IList<string> Messages = new List<string>();
        public Stopwatch StopWatch = new Stopwatch();
        public int StopCount;
        public bool UseLock = false;

        public void Handle(IMessage message)
        {
            if (!StopWatch.IsRunning)
            {
                StopWatch.Start();
            }

            var text = Encoding.UTF8.GetString(message.Body);

            OutputHelper?.WriteLine($"{nameof(MessageHandler)}: Queue: {message.Queue} - Text: {text}");

            if (UseLock)
            {
                lock (Messages)
                {
                    Messages.Add(text);
                }
            }
            else
            {
                Messages.Add(text);
            }

            if (Messages.Count >= StopCount)
            {
                StopWatch.Stop();
            }

            if (text.Contains(BadMessage.Text))
            {
                throw new InvalidOperationException($"Rogue, thou hast lived too long");
            }
        }
    }

    public static class BadMessage
    {
        public const string Text =
            "Gracious madam, I that do bring the news made not the match. He’s married, madam.";
    }
}
