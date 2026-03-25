using Xunit;

namespace GoogleMaps.LocationServices.Tests;

public class ModelTests
{
    [Fact]
    public void AddressData_ToString_IncludesOnlyProvidedParts()
    {
        var address = new AddressData
        {
            Address = "1600 Amphitheatre Pkwy",
            City = "Mountain View",
            State = "CA",
            Zip = "94043",
            Country = "USA",
        };

        Assert.Equal("1600 Amphitheatre Pkwy, Mountain View, CA, 94043, USA", address.ToString());
    }

    [Fact]
    public void AddressData_ToString_DropsMissingValues()
    {
        var address = new AddressData
        {
            City = "Los Angeles",
            State = "CA",
            Zip = string.Empty,
            Country = "USA"
        };

        Assert.Equal("Los Angeles, CA, , USA", address.ToString());
    }

    [Fact]
    public void Directions_Constructor_HasNoStepsByDefault()
    {
        var directions = new Directions();

        Assert.Equal(Directions.Status.OK, directions.StatusCode);
        Assert.Empty(directions.Steps);
        Assert.Null(directions.Distance);
        Assert.Null(directions.Duration);
    }
}
