using Cosmos.Threading;
using Microsoft.Extensions.Logging;

using Mutex = Cosmos.Threading.Mutex;

namespace TestConsole
{
    internal class Executor
    {
        private const string NamedMutexName = "named-mutex";

        private readonly ILogger<Executor> _logger;
        private readonly Mutex _mutex;
        private readonly MutexInitialization _mutexInitialization;

        public Executor(
            ILogger<Executor> logger,
            Mutex mutex,
            MutexInitialization mutexInitialization)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mutex = mutex ?? throw new ArgumentNullException(nameof(mutex));
            _mutexInitialization = mutexInitialization ?? throw new ArgumentNullException(nameof(mutexInitialization));
        }

        public async Task ExecuteAsync()
        {
            try
            {
                _logger.LogInformation("Initializing cosmos mutex");

                // Initialize the default mutex
                await _mutexInitialization.InitializeAsync();

                // Initialize a named mutex
                await _mutexInitialization.InitializeAsync(NamedMutexName);

                _logger.LogInformation("Acquiring default mutex");

                if (await _mutex.AcquireAsync(Environment.MachineName, TimeSpan.FromSeconds(1)))
                {
                    _logger.LogInformation("Acquired default mutex");

                    if (await _mutex.AcquireAsync(Environment.MachineName.Reverse() + "-x", TimeSpan.FromSeconds(1)))
                    {
                        _logger.LogError("Acquired previously acquired default mutex");
                    }

                    if (await _mutex.ReleaseAsync(Environment.MachineName))
                    {
                        _logger.LogInformation("Releasing default mutex");
                    }
                }

                _logger.LogInformation("Acquiring named mutex");

                if (await _mutex.AcquireAsync(Environment.MachineName, NamedMutexName, TimeSpan.FromSeconds(1)))
                {
                    _logger.LogInformation("Acquired named mutex");

                    if (await _mutex.AcquireAsync(Environment.MachineName + "-x", NamedMutexName, TimeSpan.FromSeconds(1)))
                    {
                        _logger.LogError("Acquired previously acquired named mutex");
                    }

                    if (await _mutex.ReleaseAsync(Environment.MachineName, NamedMutexName))
                    {
                        _logger.LogInformation("Releasing named mutex");
                    }
                }

                _logger.LogInformation("Execution Completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");
            }
        }
    }
}