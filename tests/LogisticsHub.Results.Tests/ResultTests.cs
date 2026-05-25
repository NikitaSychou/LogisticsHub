using LogisticsHub.Results;
using Xunit;

namespace LogisticsHub.Results.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_ReturnsSuccessfulResultWithoutError()
    {
        // Arrange

        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ReturnsFailedResultWithError()
    {
        // Arrange
        var error = new Error("inventory.insufficient_stock", "Insufficient stock.");

        // Act
        var result = Result.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        // Arrange
        const string value = "created";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void GenericFailure_WhenValueIsRead_Throws()
    {
        // Arrange
        var result = Result<string>.Failure(new Error("shipments.not_found", "Shipment was not found."));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);

        // Assert
        Assert.Equal("Cannot access the value of a failed result.", exception.Message);
    }

    [Fact]
    public void Error_CanStoreMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object?>
        {
            ["sku"] = "TEST-SKU-001",
            ["requestedQuantity"] = 5
        };

        // Act
        var error = new Error("inventory.insufficient_stock", "Insufficient stock.", metadata);

        // Assert
        Assert.Equal("inventory.insufficient_stock", error.Code);
        Assert.Equal("Insufficient stock.", error.Description);
        Assert.Equal("TEST-SKU-001", error.Metadata["sku"]);
        Assert.Equal(5, error.Metadata["requestedQuantity"]);
    }
}
