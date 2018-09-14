-- Help Wanted --

I've not got the Windows setup to manage this library. If you have the time to help, It'd be greatly appreciated. <3

GoogleMaps.LocationServices
=======================================

A simple library for Google Maps geolocation and reverse geolocation.

The easiest way to get hold of it is to install the [Nuget package](http://nuget.org/List/Packages/GoogleMaps.LocationServices).

From the package manager console:
`PM> Install-Package GoogleMaps.LocationServices` 

Example Lookup
----------------------

```C#
using GoogleMaps.LocationServices;
.....

AddressData[] addresses = new AddressData[] 
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

// Constructor has 3 overload
// No parameters. It does not use API Key
var gls = new GoogleLocationService();

// Boolean parameter to force the requests to use https 
// var gls = new GoogleLocationService(useHttps: true);

// String paremeter that provides the google map api key
// var gls = new GoogleLocationService(apikey: "YOUR API KEY");
foreach (var address in addresses)
{
    try
    {
        var latlong = gls.GetLatLongFromAddress(address);
        var Latitude = latlong.Latitude;
        var Longitude = latlong.Longitude;
        System.Console.WriteLine("Address ({0}) is at {1},{2}", address, Latitude, Longitude);
    }
    catch(System.Net.WebException ex)
    {
        System.Console.WriteLine("Google Maps API Error {0}", ex.Message);
    }
                
}
```
