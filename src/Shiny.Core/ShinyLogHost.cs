using Microsoft.Extensions.Logging;


namespace Shiny
{
    public static class ShinyLogHost
    {
        static ILogger? defaultLogger;
        public static ILogger? Default => defaultLogger ??= GetLog("Default");
        public static ILogger<T>? GetLog<T>() => ShinyHost.LoggerFactory.CreateLogger<T>();
        public static ILogger? GetLog(string categoryName) => ShinyHost.LoggerFactory.CreateLogger(categoryName);
    }
}