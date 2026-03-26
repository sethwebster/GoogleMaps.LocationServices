# GoogleMaps.LocationServices

A lightweight .NET library for Google Maps geolocation workflows:
- geocoding (`address -> lat/lng`)
- reverse geocoding (`lat/lng -> address`)
- simple directions lookups

[![NuGet](https://img.shields.io/nuget/v/GoogleMaps.LocationServices.svg)](https://www.nuget.org/packages/GoogleMaps.LocationServices)
[![.NET](https://github.com/sethwebster/GoogleMaps.LocationServices/actions/workflows/dotnetcore.yml/badge.svg)](https://github.com/sethwebster/GoogleMaps.LocationServices/actions/workflows/dotnetcore.yml)

## Install

```bash
# Package Manager
# Recommended current version: 2.1.0
dotnet add package GoogleMaps.LocationServices --version 2.1.0

# or
PM> Install-Package GoogleMaps.LocationServices
```

## Requirements

- A valid **Google Maps API key**
- Enable the Google APIs you intend to use (Geocoding API, Directions API if needed)

## Supported targets

- `netstandard2.0`
- `net8.0`

## Quick examples

```csharp
using GoogleMaps.LocationServices;

var client = new GoogleLocationService("YOUR_API_KEY");

var address = new AddressData
{
    Address = "1600 Amphitheatre Pkwy",
    City = "Mountain View",
    State = "CA",
    Zip = "94043",
    Country = "USA"
};

// Forward geocode
var point = client.GetLatLongFromAddress(address);
System.Console.WriteLine(point == null
    ? "No match found"
    : $"{address} => {point.Latitude}, {point.Longitude}");

// Reverse geocode
var reverse = client.GetAddressFromLatLang(37.422, -122.084);
System.Console.WriteLine(reverse);

// Directions (address to address)
var destination = new AddressData
{
    Address = "407 N Maple Dr. #1",
    City = "Beverly Hills",
    State = "CA"
};

var directions = client.GetDirections(address, destination);
System.Console.WriteLine($"Directions status: {directions.StatusCode}");
System.Console.WriteLine($"Distance: {directions.Distance}, Duration: {directions.Duration}");
foreach (var step in directions.Steps)
{
    System.Console.WriteLine($"- {step.Instruction} ({step.Distance})");
}
```

### Async overloads

Use async methods for non-blocking calls (recommended in services and UI apps):

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

var point = await client.GetLatLongFromAddressAsync("1600 Amphitheatre Parkway", cts.Token);
var region = await client.GetRegionFromLatLongAsync(37.422, -122.084, cts.Token);
var matches = await client.GetAddressesListFromAddressAsync(address);
var route = await client.GetDirectionsAsync(address, destination, cts.Token);
```

## Reliability and resilience defaults

`GoogleLocationService` now includes request hardening for production workloads:

- configurable retry count (`MaxRetryAttempts`)
- exponential backoff with jitter (`RetryDelay`)
- per-attempt request timeout (`RequestTimeout`)
- cancellation support (`CancellationToken`) on async APIs
- resilient HTTP client handling for transient network/HTTP errors (`HttpRequestException`, `TimeoutException`, `TaskCanceledException`)

```csharp
var hardened = new GoogleLocationService("YOUR_API_KEY")
{
    MaxRetryAttempts = 3,
    RetryDelay = TimeSpan.FromMilliseconds(250),
    RequestTimeout = TimeSpan.FromSeconds(8)
};
```

You can also inject a shared `HttpClient` when integrating into DI/container-controlled clients:

```csharp
using var httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
var fromDi = new GoogleLocationService("YOUR_API_KEY", httpClient);
```

## Error handling

- `OVER_QUERY_LIMIT` throws `WebException` with an actionable message.
- `REQUEST_DENIED` throws `WebException` when the required Google Maps APIs are not enabled.
- transient failures honor retry configuration before surfacing an exception.

## Testing

The repo includes a full unit test suite:

```bash
dotnet test --configuration Release
```

Current test target includes:
- geocode parsing
- reverse geocode parsing
- directions parsing and failure states
- async behavior with retry validation

## Development

If you are contributing, open the solution and run:

```bash
dotnet restore
dotnet build
dotnet test
dotnet pack GoogleMaps.LocationServices/GoogleMaps.LocationServices.csproj --configuration Release
```
