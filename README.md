# Cosmos.Threading

[![Build Status](https://derekgn.visualstudio.com/GitHub/_apis/build/status%2FDerekGn.Cosmos.Threading?branchName=main)](https://derekgn.visualstudio.com/GitHub/_build/latest?definitionId=14&branchName=main)

[![NuGet Badge](https://buildstats.info/nuget/Cosmos.Threading)](https://www.nuget.org/packages/Cosmos.Threading/)

A library that contains a simple cosmos based distributed mutex. The mutex allows processes to coordinate access to a shared set of resources.

## Installing Cosmos.Threading

Install the Cosmos.Threading package via nuget package manager console:

``` shell
Install-Package Cosmos.Threading
```

## Initializing Cosmos.Threading Mutex

### Cosmos.Threading Configuration

The cosmos threading mutex requires some configuration options to be set. This can be read from a configuration section from the app configuration.

``` json
  "Cosmos.Threading.Mutex": {
    "DatabaseId": "mutex-database",
    "ContainerName": "mutex-container"
  }
```

Setting| Description
---------|----------
 DatabaseId | This is the name of the cosmos database that the mutex support containers need to be set.
 ContainerName | The name of the container to which mutex items will be read/written

### Cosmos.Threading Initialization

Create a container to store the mutex instances. The application has full control over the definition of the container.

```csharp
await _cosmosClient
    .GetDatabase(_options.Value.DatabaseId)
    .CreateContainerIfNotExistsAsync(
    new ContainerProperties()
    {
        Id = _options.Value.ContainerName,
        PartitionKeyPath = MutexInitialization.PartitionKeyPath
    });
```

To create the support containers in the cosmos database the **MutexInitialization** class must be used to initialize the mutex in the container.

``` csharp
// Initialize the default mutex
await _mutexInitialization.InitializeAsync();

// Initialize a named mutex
await _mutexInitialization.InitializeAsync(NamedMutexName);
```

### Acquire And Release Mutex

The **AcquireAsync(string, TimeSpan)** method acquires the mutex. The *string* argument is the name of the owner attempting to acquire the mutex. The *TimeSpan* argument is the upper limit of the acquisition time for the mutex. After the *TimeSpan* the mutex can be acquired by another owner. This timeout value is to prevent a deadlocked or terminated process from holding on to the mutex.

``` csharp
if (await _mutex.AcquireAsync(Environment.MachineName, TimeSpan.FromSeconds(10)))
{
    // Do some work and then release
    await _mutex.ReleaseAsync(Environment.MachineName);
}
```

The **AcquireAsync(string, string TimeSpan)** method acquires a named mutex. The first *string* argument is the name of the owner attempting to acquire the mutex. The second *string* argument is the name of the mutex. The *TimeSpan* argument is the upper limit of the acquisition time for the mutex. After the *TimeSpan* the mutex can be acquired by another owner. This timeout value is to prevent a deadlocked or terminated process from holding on to the mutex.

``` csharp
if (await _mutex.AcquireAsync(Environment.MachineName, NamedMutexName, TimeSpan.FromSeconds(10)))
{
    // Do some work and then release
    await _mutex.ReleaseAsync(Environment.MachineName, NamedMutexName);
}
```
