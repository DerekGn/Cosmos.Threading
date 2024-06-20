﻿/*
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

using Cosmos.Threading.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Cosmos.Threading
{
    public class Mutex : IMutex
    {
        private readonly CosmosClient _client;
        private readonly Container _container;
        private readonly ILogger<Mutex> _logger;
        private readonly IOptions<MutexOptions> _options;

        /// <summary>
        /// Create an instance of a <see cref="Mutex"/>
        /// </summary>
        /// <param name="client">The <see cref="CosmosClient"/> instance</param>
        /// <param name="logger">An <see cref="ILogger"/> instance for logging</param>
        /// <param name="options">The <see cref="Mutex"/> configuration settings</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the parameters are null or empty</exception>
        public Mutex(
            CosmosClient client,
            ILogger<Mutex> logger,
            IOptions<MutexOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<bool> AcquireAsync(
            string owner,
            TimeSpan leaseExpiry,
            CancellationToken cancellationToken = default)
        {
            ValidateOwner(owner);

            _logger.LogDebug("Acquiring Mutex Id: [{id}] Owner: [{owner}]", _options.Value.Name, owner);

            return await PatchAsync(DateTime.UtcNow.Add(leaseExpiry), string.Empty, owner, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> ReleaseAsync(
            string owner,
            CancellationToken cancellationToken = default)
        {
            ValidateOwner(owner);

            _logger.LogDebug("Releasing Mutex Id: [{id}] Owner: [{owner}]", _options.Value.Name, owner);

            return await PatchAsync(DateTime.UtcNow, owner, string.Empty, cancellationToken);
        }

        private static void ValidateOwner(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner))
            {
                throw new ArgumentNullException(nameof(owner));
            }
        }

        private async Task<bool> PatchAsync(
            DateTime leaseExpiry,
            string predicateCheck,
            string owner,
            CancellationToken cancellationToken)
        {
            bool result = false;

            var operations = new[] {
                PatchOperation.Set($"/{nameof(MutexItem.Owner).ToCamelCase()}", owner),
                PatchOperation.Set($"/{nameof(MutexItem.LeaseExpiry).ToCamelCase()}", leaseExpiry)
            };

            PatchItemRequestOptions options = new()
            {
                FilterPredicate = $"FROM {MutexInitialization.MutexContainerName} c " +
                $"WHERE c.{nameof(MutexItem.Owner).ToCamelCase()} = \"{predicateCheck}\" " +
                $"OR c.{nameof(MutexItem.LeaseExpiry).ToCamelCase()} < \"{DateTime.UtcNow.ToString("O")}\""
            };

            try
            {
                var response = await _container!.PatchItemAsync<Mutex>(
                    _options.Value.Name,
                    new PartitionKey(_options.Value.Name),
                    operations,
                    options,
                    cancellationToken);

                _logger.LogDebug("Mutex update StatusCode: [{statusCode}] {rus} RUs", response.StatusCode, response.RequestCharge);

                result = true;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed) { }

            return result;
        }
    }
}