
using Cosmos.Threading;
using Microsoft.Extensions.Logging;

namespace TestConsole
{
    internal class Executor
    {
        private readonly ILogger<Executor> _logger;
        private readonly MutexInitialization _mutexInitialization;

        public Executor(
            ILogger<Executor> logger,
            MutexInitialization mutexInitialization)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mutexInitialization = mutexInitialization ?? throw new ArgumentNullException(nameof(mutexInitialization));
        }

        public async Task ExecuteAsync()
        {
            try
            {
                _logger.LogInformation("Initializing cosmos mutex");

                await _mutexInitialization.InitializeAsync();

                _logger.LogInformation("Execution Completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");
            }
        }
    }
}
