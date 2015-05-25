GoogleMaps.LocationServices
=======================================

A simple library for Google Maps geolocation and reverse geolocation.

The easiest way to get hold of it is to install the [Nuget package](http://nuget.org/List/Packages/GoogleMaps.LocationServices).

Example Lookup
----------------------

<pre>
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

var gls = new GoogleLocationService();
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
</pre>
