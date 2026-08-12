using System;
using UnityModManagerNet;

namespace KingmakerBuffPlanner.Infrastructure
{
    internal sealed class ModLog
    {
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        internal ModLog(UnityModManager.ModEntry.ModLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException("logger");
        }

        internal void Info(string message)
        {
            _logger.Log(message ?? string.Empty);
        }

        internal void Error(string message, Exception exception)
        {
            _logger.Error((message ?? string.Empty) + (exception == null
                ? string.Empty
                : Environment.NewLine + exception));
        }
    }
}
