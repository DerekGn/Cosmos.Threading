/*
* MIT License
*
* Copyright (c) 2024 Derek Goslin https://github.com/DerekGn
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Cosmos.Threading
{
    public class MutexInitialization
    {
        private readonly CosmosClient _client;
        private readonly ILogger<MutexInitialization> _logger;
        private readonly IOptions<MutexOptions> _options;

        /// <summary>
        /// Create an instance of a <see cref="MutexInitialization"/> instance
        /// </summary>
        /// <param name="client">The <see cref="CosmosClient"/> instance used to initiates the cosmos db container</param>
        /// <param name="options">The <see cref="IOptions{MutexOptions}"/> instance</param>
        /// <param name="logger">The <see cref="ILogger{MutexInitialization}"/> instance</param>
        /// <exception cref="ArgumentNullException">Thrown if an argument is null</exception>
        public MutexInitialization(
            CosmosClient client,
            IOptions<MutexOptions> options,
            ILogger<MutexInitialization> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Initialize the default mutex instance
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to cancel the operation</param>
        public async Task InitializeAsync(
            CancellationToken cancellationToken = default)
        {
            await InitializeAsync(Mutex.DefaultMutexName, cancellationToken);
        }

        /// <summary>
        /// Initialize a named mutex instance
        /// </summary>
        /// <param name="mutexName">The mutex instance name</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to cancel the operation</param>
        public async Task InitializeAsync(
            string mutexName,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Initializing Mutex Id: [{id}]", mutexName);

            var containerResponse = await _client
                .GetDatabase(_options.Value.DatabaseName)
                .CreateContainerIfNotExistsAsync(new ContainerProperties()
                {
                    Id = Mutex.MutexContainerName,
                    PartitionKeyPath = "/id"
                },
                cancellationToken: cancellationToken);

            _logger.LogDebug("Creating mutex container: [{container}] in database: [{database}] [{rus}] RUs",
                containerResponse.Container.Id,
                containerResponse.Container.Database,
                containerResponse.RequestCharge);

            await CreateMutexInstanceIfNotExistAsync(
                mutexName,
                containerResponse.Container,
                cancellationToken);
        }

        private async Task CreateMutexInstanceIfNotExistAsync(
            string mutexName,
            Container container,
            CancellationToken cancellationToken)
        {
            var partitionKey = new PartitionKeyBuilder()
                .Add(mutexName)
                .Build();

            ItemResponse<MutexItem>? mutexResponse = null;

            try
            {
                mutexResponse = await container.ReadItemAsync<MutexItem>
                (
                    mutexName,
                    partitionKey,
                    cancellationToken: cancellationToken
                );

                _logger.LogDebug("Mutex [{id}] exists cost [{rus}] RUs",
                    mutexName,
                    mutexResponse.RequestCharge);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }

            if (mutexResponse == null)
            {
                var mutex = new MutexItem()
                {
                    Id = mutexName
                };

                var mutexCreateResponse = await container.CreateItemAsync(
                    mutex,
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Mutex [{id}] created [{rus}] RUs",
                    mutexName,
                    mutexCreateResponse.RequestCharge);
            }
        }
    }
}