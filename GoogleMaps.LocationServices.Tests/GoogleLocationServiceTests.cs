using System;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace GoogleMaps.LocationServices.Tests;

public class GoogleLocationServiceTests
{
    private sealed class TestableGoogleLocationService(string apiKey = "test-api-key") : GoogleLocationService(apiKey)
    {
        public Func<string, XDocument>? XDocumentLoader { get; set; }

        public Func<string, XmlDocument>? XmlDocumentLoader { get; set; }

        protected override XDocument LoadXDocumentFromUrl(string requestUrl)
        {
            if (XDocumentLoader == null)
            {
                throw new InvalidOperationException("XDocumentLoader is not configured");
            }

            return XDocumentLoader(requestUrl);
        }

        protected override XmlDocument LoadXmlDocumentFromUrl(string requestUrl)
        {
            if (XmlDocumentLoader == null)
            {
                throw new InvalidOperationException("XmlDocumentLoader is not configured");
            }

            return XmlDocumentLoader(requestUrl);
        }
    }

    [Fact]
    public void GetRegionFromLatLong_ReturnsStateRegion_WhenResponseContainsAdministrativeArea()
    {
        const string response = @"
<GeocodeResponse>
  <status>OK</status>
  <result>
    <address_component>
      <long_name>California</long_name>
      <short_name>CA</short_name>
      <type>administrative_area_level_1</type>
    </address_component>
  </result>
</GeocodeResponse>";

        var service = new TestableGoogleLocationService("api")
        {
            XDocumentLoader = _ => XDocument.Parse(response)
        };

        var region = service.GetRegionFromLatLong(37.7749, -122.4194);

        Assert.NotNull(region);
        Assert.Equal("California", region.Name);
        Assert.Equal("CA", region.ShortCode);
    }

    [Fact]
    public void GetAddressFromLatLang_ReturnsParsedAddressData()
    {
        const string response = @"
<GeocodeResponse>
  <status>OK</status>
  <result>
    <address_component>
      <long_name>United States</long_name>
      <short_name>US</short_name>
      <type>country</type>
    </address_component>
    <address_component>
      <long_name>Mountain View</long_name>
      <short_name>CA</short_name>
      <type>administrative_area_level_1</type>
    </address_component>
    <address_component>
      <long_name>Mountain View</long_name>
      <short_name>Mountain View</short_name>
      <type>locality</type>
    </address_component>
    <address_component>
      <long_name>1600</long_name>
      <short_name>1600</short_name>
      <type>street_number</type>
    </address_component>
    <address_component>
      <long_name>Amphitheatre Pkwy</long_name>
      <short_name>Amphitheatre Pkwy</short_name>
      <type>route</type>
    </address_component>
    <address_component>
      <long_name>94043</long_name>
      <short_name>94043</short_name>
      <type>postal_code</type>
    </address_component>
  </result>
</GeocodeResponse>";

        var service = new TestableGoogleLocationService("api")
        {
            XmlDocumentLoader = _ =>
            {
                var doc = new XmlDocument();
                doc.LoadXml(response);
                return doc;
            }
        };

        var address = service.GetAddressFromLatLang(37.422, -122.084);

        Assert.NotNull(address);
        Assert.Equal("United States", address.Country);
        Assert.Equal("CA", address.State);
        Assert.Equal("Mountain View", address.City);
        Assert.Equal("1600 Amphitheatre Pkwy", address.Address);
        Assert.Equal("94043", address.Zip);
    }

    [Fact]
    public void GetAddressFromLatLang_ReturnsNull_WhenNoResults()
    {
        const string response = @"
<GeocodeResponse>
  <status>ZERO_RESULTS</status>
</GeocodeResponse>";

        var service = new TestableGoogleLocationService("api")
        {
            XmlDocumentLoader = _ =>
            {
                var doc = new XmlDocument();
                doc.LoadXml(response);
                return doc;
            }
        };

        var address = service.GetAddressFromLatLang(1, 2);

        Assert.Null(address);
    }

    [Fact]
    public void GetLatLongFromAddress_ReturnsCoordinates()
    {
        const string response = @"
<GeocodeResponse>
  <status>OK</status>
  <result>
    <geometry>
      <location>
        <lat>37.422</lat>
        <lng>-122.084</lng>
      </location>
    </geometry>
  </result>
</GeocodeResponse>";

        var service = new TestableGoogleLocationService("api")
        {
            XDocumentLoader = _ => XDocument.Parse(response)
        };

        var mapPoint = service.GetLatLongFromAddress("1600 Amphitheatre Parkway, Mountain View, CA");

        Assert.NotNull(mapPoint);
        Assert.Equal(37.422, mapPoint.Latitude);
        Assert.Equal(-122.084, mapPoint.Longitude);
    }

    [Fact]
    public void GetLatLongFromAddress_Throws_OnOverQueryLimit()
    {
        const string response = @"
<GeocodeResponse>
  <status>OVER_QUERY_LIMIT</status>
</GeocodeResponse>";

        var service = new TestableGoogleLocationService("api")
        {
            XDocumentLoader = _ => XDocument.Parse(response)
        };

        var ex = Assert.Throws<WebException>(() => service.GetLatLongFromAddress("test"));
        Assert.Contains("QueryLimit exceeded", ex.Message);
    }

    [Fact]
    public void GetAddressesListFromAddress_ReturnsAllFormattedAddresses()
    {
        const string response = @"
<GeocodeResponse>
  <status>OK</status>
  <result>
    <formatted_address>1600 Amphitheatre Pkwy, Mountain View, CA 94043</formatted_address>
    <formatted_address>Google Building 41, Mountain View, CA</formatted_address>
  </result>
</GeocodeResponse>";

        var service = new TestableGoogleLocationService("api")
        {
            XDocumentLoader = _ => XDocument.Parse(response)
        };

        var addresses = service.GetAddressesListFromAddress("1600 Amphitheatre Parkway, Mountain View, CA");

        Assert.NotNull(addresses);
        Assert.Equal(2, addresses!.Length);
        Assert.Contains("1600 Amphitheatre Pkwy, Mountain View, CA 94043", addresses);
        Assert.Contains("Google Building 41, Mountain View, CA", addresses);
    }

    [Fact]
    public void GetDirections_ReturnsParsedDirectionsWhenStatusOk()
    {
        const string response = @"
<DirectionsResponse>
  <status>OK</status>
  <route>
    <leg>
      <distance>
        <text>7.5 mi</text>
      </distance>
      <duration>
        <text>15 mins</text>
      </duration>
      <step>
        <distance><text>1.0 mi</text></distance>
        <html_instructions>Head north</html_instructions>
      </step>
      <step>
        <distance><text>2.0 mi</text></distance>
        <html_instructions>Turn right</html_instructions>
      </step>
    </leg>
  </route>
</DirectionsResponse>";

        var service = new TestableGoogleLocationService("api")
        {
            XDocumentLoader = _ => XDocument.Parse(response)
        };

        var direction = service.GetDirections(
            new AddressData { Address = "1 Infinite Loop", City = "Cupertino", State = "CA", Zip = "95014", Country = "USA" },
            new AddressData { Address = "1600 Amphitheatre Parkway", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" });

        Assert.NotNull(direction);
        Assert.Equal(Directions.Status.OK, direction.StatusCode);
        Assert.Equal("7.5 mi", direction.Distance);
        Assert.Equal("15 mins", direction.Duration);
        Assert.Equal(2, direction.Steps.Count);
        Assert.Equal("Head north", direction.Steps[0].Instruction);
        Assert.Equal("2.0 mi", direction.Steps[1].Distance);
    }

    [Fact]
    public void GetDirections_ReturnsFailedStatus_WhenApiReturnsNotOk()
    {
        const string response = @"
<DirectionsResponse>
  <status>ZERO_RESULTS</status>
</DirectionsResponse>";

        var service = new TestableGoogleLocationService("api")
        {
            XDocumentLoader = _ => XDocument.Parse(response)
        };

        var direction = service.GetDirections(
            new AddressData { Address = "A", City = "City", State = "ST", Zip = "12345", Country = "USA" },
            new AddressData { Address = "B", City = "City", State = "ST", Zip = "12345", Country = "USA" });

        Assert.Equal(Directions.Status.FAILED, direction.StatusCode);
        Assert.Empty(direction.Steps);
        Assert.Null(direction.Distance);
        Assert.Null(direction.Duration);
    }

    [Fact]
    public void GetDirections_BuildsUriFromInputAddresses()
    {
        var captured = string.Empty;

        var service = new TestableGoogleLocationService("api")
        {
            XDocumentLoader = requestUrl =>
            {
                captured = requestUrl;
                return XDocument.Parse(@"<DirectionsResponse><status>ZERO_RESULTS</status></DirectionsResponse>");
            }
        };

        _ = service.GetDirections(
            new AddressData { City = "New York" },
            new AddressData { City = "Boston" });

        Assert.Contains("origin=New%20York", captured, StringComparison.Ordinal);
        Assert.Contains("destination=Boston", captured, StringComparison.Ordinal);
        Assert.Contains("&key=api", captured, StringComparison.Ordinal);
    }
}
