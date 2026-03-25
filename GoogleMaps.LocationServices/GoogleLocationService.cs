using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace GoogleMaps.LocationServices;

public class GoogleLocationService : ILocationService
{
    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleLocationService"/> class.
    /// </summary>
    /// <param name="useHttps">Indicates whether to call the Google API over HTTPS or not.</param>
    public GoogleLocationService(bool useHttps)
    {
        APIKey = string.Empty;
        UseHttps = useHttps;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleLocationService"/> class.
    /// Defaults to HTTPS for modern API compatibility.
    /// </summary>
    public GoogleLocationService()
        : this(true)
    {
        APIKey = string.Empty;
    }

    public GoogleLocationService(string apiKey)
    {
        APIKey = apiKey;
        UseHttps = true;
    }
    #endregion


    #region Properties
    /// <summary>
    /// Gets a value indicating whether to use the Google API over HTTPS.
    /// </summary>
    public bool UseHttps { get; private set; }

    private string APIKey { get; set; }

    private string UrlProtocolPrefix => UseHttps ? "https://" : "http://";

    protected string APIUrlRegionFromLatLong => UrlProtocolPrefix + Constants.ApiUriTemplates.ApiRegionFromLatLong;

    protected string APIUrlLatLongFromAddress => UrlProtocolPrefix + Constants.ApiUriTemplates.ApiLatLongFromAddress;

    protected string APIUrlDirections => UrlProtocolPrefix + Constants.ApiUriTemplates.ApiDirections;
    #endregion

    #region Protected helpers
    protected virtual XDocument LoadXDocumentFromUrl(string requestUrl)
    {
        return XDocument.Load(requestUrl);
    }

    protected virtual XmlDocument LoadXmlDocumentFromUrl(string requestUrl)
    {
        var doc = new XmlDocument();
        doc.Load(requestUrl);
        return doc;
    }

    protected string BuildRequestUrl(string template, params object[] parameters)
    {
        var rawUrl = string.Format(CultureInfo.InvariantCulture, template, parameters);
        return rawUrl + "&key=" + APIKey;
    }
    #endregion

    #region Public instance methods
    /// <summary>
    /// Translates a Latitude / Longitude into a Region (state) using Google Maps api
    /// </summary>
    public Region? GetRegionFromLatLong(double latitude, double longitude)
    {
        var doc = LoadXDocumentFromUrl(BuildRequestUrl(APIUrlRegionFromLatLong, latitude, longitude));

        var administrativeArea = doc
            .Descendants("result")
            .SelectMany(r => r.Descendants("address_component"))
            .FirstOrDefault(component => component.Descendants("type").Any(t => t.Value == "administrative_area_level_1"));

        if (administrativeArea == null)
        {
            return null;
        }

        return new Region
        {
            Name = administrativeArea.Descendants("long_name").First().Value,
            ShortCode = administrativeArea.Descendants("short_name").First().Value
        };
    }

    /// <summary>
    /// Translates a Latitude / Longitude into an address using Google Maps api
    /// </summary>
    public AddressData? GetAddressFromLatLang(double latitude, double longitude)
    {
        var addressCountry = string.Empty;
        var addressAdministrativeAreaLevel1 = string.Empty;
        var addressLocality = string.Empty;
        var addressSublocality = string.Empty;
        var addressRoute = string.Empty;
        var addressStreetNumber = string.Empty;
        var addressPostalCode = string.Empty;

        var doc = LoadXmlDocumentFromUrl(BuildRequestUrl(APIUrlRegionFromLatLong, latitude, longitude));

        var status = doc.SelectSingleNode("//GeocodeResponse/status");
        if (status == null || status.InnerText == Constants.ApiResponses.ZeroResults)
        {
            return null;
        }

        var addressComponents = doc.SelectNodes("//GeocodeResponse/result/address_component");
        if (addressComponents == null)
        {
            return null;
        }

        foreach (XmlNode addressNode in addressComponents)
        {
            var longName = addressNode["long_name"]?.InnerText ?? string.Empty;
            var shortName = addressNode["short_name"]?.InnerText ?? string.Empty;
            var componentType = addressNode["type"]?.InnerText;

            switch (componentType)
            {
                case "country":
                    addressCountry = longName;
                    break;
                case "locality":
                    addressLocality = longName;
                    break;
                case "sublocality":
                    addressSublocality = longName;
                    break;
                case "administrative_area_level_1":
                    addressAdministrativeAreaLevel1 = shortName;
                    break;
                case "route":
                    addressRoute = shortName;
                    break;
                case "street_number":
                    addressStreetNumber = shortName;
                    break;
                case "postal_code":
                    addressPostalCode = longName;
                    break;
            }
        }

        return new AddressData
        {
            Country = addressCountry,
            State = addressAdministrativeAreaLevel1,
            City = addressLocality,
            Address = string.Join(" ", new[] { addressStreetNumber, addressRoute, addressSublocality }.Where(part => !string.IsNullOrWhiteSpace(part))),
            Zip = addressPostalCode,
        };
    }

    /// <summary>
    /// Gets the latitude and longitude that belongs to an address.
    /// </summary>
    /// <param name="address">The address.</param>
    public MapPoint? GetLatLongFromAddress(string address)
    {
        var doc = LoadXDocumentFromUrl(BuildRequestUrl(APIUrlLatLongFromAddress, Uri.EscapeDataString(address)));

        string status = doc.Descendants("status").FirstOrDefault()?.Value ?? string.Empty;
        if (status == Constants.ApiResponses.OverQueryLimit)
        {
            throw new WebException("QueryLimit exceeded, check your dashboard");
        }

        if (status == Constants.ApiResponses.RequestDenied)
        {
            throw new WebException("Request denied, it's likely you need to enable the necessary Google maps APIs");
        }

        var elements = doc.Descendants("result").Descendants("geometry").Descendants("location").FirstOrDefault();
        if (elements == null)
        {
            return null;
        }

        var locationValues = elements.Elements("lat").Concat(elements.Elements("lng")).ToArray();
        if (locationValues.Length < 2)
        {
            return null;
        }

        var latitude = ParseUS(locationValues[0].Value);
        var longitude = ParseUS(locationValues[1].Value);
        return new MapPoint { Latitude = latitude, Longitude = longitude };
    }

    /// <summary>
    /// Gets the latitude and longitude that belongs to an address.
    /// </summary>
    /// <param name="address">The address.</param>
    public MapPoint? GetLatLongFromAddress(AddressData address) => GetLatLongFromAddress(address.ToString());

    /// <summary>
    /// Gets an array of string addresses that matched a possibly ambiguous address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns></returns>
    public string[]? GetAddressesListFromAddress(string address)
    {
        var doc = LoadXDocumentFromUrl(BuildRequestUrl(APIUrlLatLongFromAddress, Uri.EscapeDataString(address)));
        var status = doc.Descendants("status").FirstOrDefault()?.Value;

        if (status == Constants.ApiResponses.OverQueryLimit)
        {
            throw new WebException("QueryLimit exceeded, check your dashboard");
        }

        if (status == Constants.ApiResponses.RequestDenied)
        {
            throw new WebException("Request denied, it's likely you need to enable the necessary Google maps APIs");
        }

        var addresses = doc.Descendants("result").Descendants("formatted_address").Select(elem => elem.Value).ToArray();
        if (addresses.Length > 0)
        {
            return addresses;
        }

        return null;
    }

    /// <summary>
    /// Gets an array of string addresses that matched a possibly ambiguous address.
    /// </summary>
    /// <param name="address">The address.</param>
    public string[]? GetAddressesListFromAddress(AddressData address) => GetAddressesListFromAddress(address.ToString());


    /// <summary>
    /// Gets the directions.
    /// </summary>
    /// <param name="latitude">The latitude.</param>
    /// <param name="longitude">The longitude.</param>
    public Directions GetDirections(double latitude, double longitude)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the directions.
    /// </summary>
    /// <param name="originAddress">From address.</param>
    /// <param name="destinationAddress">To address.</param>
    public Directions GetDirections(AddressData originAddress, AddressData destinationAddress)
    {
        var direction = new Directions();

        var xdoc = LoadXDocumentFromUrl(BuildRequestUrl(APIUrlDirections,
            Uri.EscapeDataString(originAddress.ToString()),
            Uri.EscapeDataString(destinationAddress.ToString())));

        var status = xdoc.Descendants("DirectionsResponse").Descendants("status").FirstOrDefault();

        if (status != null && status.Value == "OK")
        {
            direction.StatusCode = Directions.Status.OK;
            direction.Distance = xdoc
                .Descendants("DirectionsResponse")
                .Descendants("route")
                .Descendants("leg")
                .Elements("distance")
                .Elements("text")
                .FirstOrDefault()?.Value;

            direction.Duration = xdoc
                .Descendants("DirectionsResponse")
                .Descendants("route")
                .Descendants("leg")
                .Elements("duration")
                .Elements("text")
                .FirstOrDefault()?.Value;

            var steps = xdoc
                .Descendants("DirectionsResponse")
                .Descendants("route")
                .Descendants("leg")
                .Descendants("step");

            foreach (var step in steps)
            {
                direction.Steps.Add(new Step
                {
                    Instruction = step.Element("html_instructions")?.Value ?? string.Empty,
                    Distance = step.Descendants("distance").FirstOrDefault()?.Element("text")?.Value,
                });
            }

            return direction;
        }

        if (status != null && status.Value != "OK")
        {
            direction.StatusCode = Directions.Status.FAILED;
            return direction;
        }

        throw new Exception("Unable to get Directions from Google");
    }

    private double ParseUS(string value)
    {
        return double.Parse(value, CultureInfo.GetCultureInfo("en-US"));
    }
    #endregion
}
