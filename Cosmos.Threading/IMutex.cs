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

namespace Cosmos.Threading
{
    public interface IMutex
    {
        /// <summary>
        /// Acquire the distributed mutex
        /// </summary>
        /// <param name="owner">The owner moniker for the thread/process acquiring the <see cref="IMutex"/> instance</param>
        /// <param name="leaseExpiry">The maximum length if time that the mutex can be held</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to cancel the operation</param>
        /// <returns>True if the mutex is acquired</returns>
        /// <remarks>
        /// The owner must be consistent between <seealso cref="AcquireAsync(string, TimeSpan, CancellationToken)"/> and <seealso cref="ReleaseAsync(string, CancellationToken)"/>
        /// The <paramref name="leaseExpiry"/> sets the upper limit for the mutex lease.
        /// After this time the mutex is released and can be aquired again by another process. 
        /// </remarks>
        Task<bool> AcquireAsync(string owner, TimeSpan leaseExpiry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Release the distributed mutex
        /// </summary>
        /// <param name="owner">The owner moniker for the thread/process releasing the <see cref="IMutex"/> instance</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to cancel the operation</param>
        /// <returns>True if the mutex is released</returns>
        /// <remarks>
        /// The owner must be consistent between <seealso cref="AcquireAsync(string, TimeSpan, CancellationToken)"/> and <seealso cref="ReleaseAsync(string, CancellationToken)"/>
        /// </remarks>
        Task<bool> ReleaseAsync(string owner, CancellationToken cancellationToken = default);
    }
}
