using System;

namespace Otc.Messaging.RabbitMQ.Cli
{
    public static class ExceptionHandler
    {
        public static void Handle(Exception e)
        {
            Report(e);
            Environment.Exit(1);
        }

        private static void Report(Exception e)
        {
            Console.WriteLine(e.GetType().Name + ": " + e.Message);
            if (e.InnerException != null)
            {
                Report(e.InnerException);
            }
        }
    }
}
