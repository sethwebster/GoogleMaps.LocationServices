GoogleMaps.LocationServices
=======================================

A simple library for Google Maps geolocation and reverse geolocation built with modern
SDK-style .NET tooling.

Install from NuGet:

```text
PM> Install-Package GoogleMaps.LocationServices
```

Supported targets:

- `.NET Standard 2.0`
- `.NET 8.0`

Example Lookup
----------------------

```C#
using GoogleMaps.LocationServices;

AddressData[] addresses = new[]
{
    new AddressData // Belgium
    {
        Address = "Rue du Cornet 6",
        City = "VERVIERS",
        State = null,
        Country = "Belgium",
        Zip = "B-4800"
    },
    new AddressData
    {
        Address = "1600 Pennsylvania ave",
        City = "Washington",
        State = "DC"
    },
    new AddressData
    {
        Address = "407 N Maple Dr. #1",
        City = "Beverly Hills",
        State = "CA"
    }
};

// No API key constructor still works (legacy use).
var legacyClient = new GoogleLocationService();

// Preferred: pass your Google Maps API key for reliability.
var client = new GoogleLocationService("YOUR_API_KEY");

foreach (var address in addresses)
{
    try
    {
        var latlong = client.GetLatLongFromAddress(address);
        if (latlong == null) continue;

        var latitude = latlong.Latitude;
        var longitude = latlong.Longitude;

        System.Console.WriteLine($"Address ({address}) is at {latitude},{longitude}");
    }
    catch (System.Net.WebException ex)
    {
        System.Console.WriteLine($"Google Maps API Error: {ex.Message}");
    }
}
```
