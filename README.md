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
dotnet add package GoogleMaps.LocationServices --version 2.0.0

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

The package also supports the legacy no-key constructor:

```csharp
var legacyClient = new GoogleLocationService();
```

> For reliability and quota visibility, prefer using a valid API key.

## Error handling

- `OVER_QUERY_LIMIT` throws `WebException` with an actionable message.
- `REQUEST_DENIED` throws `WebException` when the required Google Maps APIs are not enabled.

## Testing

The repo includes a full unit test suite:

```bash
dotnet test --configuration Release
```

Current test target includes:
- geocode parsing
- reverse geocode parsing
- coordinates formatting and mapping
- directions parsing and failure states

## Development

If you are contributing, open the solution and run:

```bash
dotnet restore
dotnet build

dotnet test
dotnet pack GoogleMaps.LocationServices/GoogleMaps.LocationServices.csproj --configuration Release
```
