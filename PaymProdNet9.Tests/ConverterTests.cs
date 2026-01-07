using PaymProdNet9.Converters;
using System.Globalization;
using System.Windows;
using Xunit;

namespace PaymProdNet9.Tests.Converters;

public class ConverterTests
{
    [Fact]
    public void NullToBoolConverter_ShouldReturnFalse_WhenNull()
    {
        var converter = new NullToBoolConverter();
        var result = converter.Convert(null, null, null, CultureInfo.InvariantCulture);
        Assert.False((bool)result);
    }

    [Fact]
    public void NullToBoolConverter_ShouldReturnTrue_WhenNotNull()
    {
        var converter = new NullToBoolConverter();
        var result = converter.Convert(new object(), null, null, CultureInfo.InvariantCulture);
        Assert.True((bool)result);
    }

    [Theory]
    [InlineData(-1, Visibility.Collapsed)]
    [InlineData(-100, Visibility.Collapsed)]
    [InlineData(0, Visibility.Visible)]
    [InlineData(1, Visibility.Visible)]
    [InlineData(100, Visibility.Visible)]
    [InlineData(null, Visibility.Visible)] // Invalid type -> Visible
    [InlineData("string", Visibility.Visible)] // Invalid type -> Visible
    public void ProductVisibilityConverter_ShouldWork(object value, Visibility expected)
    {
        var converter = new ProductVisibilityConverter();
        var result = converter.Convert(value, null, null, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }
}