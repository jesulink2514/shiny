using System;
using Microsoft.Extensions.Logging;


namespace Shiny.Infrastructure
{
    public class ShinyLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
        }


        public ILogger CreateLogger(string categoryName)
        {
            return null;
        }


        public void Dispose() { }
    }
}
