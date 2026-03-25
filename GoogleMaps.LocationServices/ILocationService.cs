namespace GoogleMaps.LocationServices;

public interface ILocationService
{
    /// <summary>
    /// Translates a Latitude / Longitude into a Region (state) using Google Maps api
    /// </summary>
    Region? GetRegionFromLatLong(double latitude, double longitude);


    /// <summary>
    /// Gets the latitude and longitude that belongs to an address.
    /// </summary>
    MapPoint? GetLatLongFromAddress(string address);

    /// <summary>
    /// Gets the directions.
    /// </summary>
    Directions GetDirections(double latitude, double longitude);

    /// <summary>
    /// Gets the directions.
    /// </summary>
    Directions GetDirections(AddressData fromAddress, AddressData toAddress);
}
