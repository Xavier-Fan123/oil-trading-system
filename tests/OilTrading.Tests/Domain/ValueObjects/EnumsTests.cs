using FluentAssertions;
using OilTrading.Core.ValueObjects;
using System.Linq;
using Xunit;

namespace OilTrading.Tests.Domain.ValueObjects;

public class EnumsTests
{
    #region DeliveryTerms Tests

    [Theory]
    [InlineData(DeliveryTerms.FOB, 1)]
    [InlineData(DeliveryTerms.CIF, 2)]
    [InlineData(DeliveryTerms.CFR, 3)]
    [InlineData(DeliveryTerms.DAP, 4)]
    [InlineData(DeliveryTerms.DDP, 5)]
    [InlineData(DeliveryTerms.DES, 6)]
    [InlineData(DeliveryTerms.DDU, 7)]
    [InlineData(DeliveryTerms.STS, 8)]
    [InlineData(DeliveryTerms.ITT, 9)]
    [InlineData(DeliveryTerms.EXW, 10)]
    public void DeliveryTerms_ShouldHaveCorrectValues(DeliveryTerms term, int expectedValue)
    {
        // Act & Assert
        ((int)term).Should().Be(expectedValue);
    }

    [Fact]
    public void DeliveryTerms_ShouldContainAllExpectedValues()
    {
        // Arrange
        var expectedTerms = new[]
        {
            DeliveryTerms.FOB, DeliveryTerms.CFR, DeliveryTerms.CIF,
            DeliveryTerms.DES, DeliveryTerms.DAP, DeliveryTerms.DDU,
            DeliveryTerms.STS, DeliveryTerms.ITT, DeliveryTerms.EXW,
            DeliveryTerms.DDP
        };

        // Act
        var allTerms = Enum.GetValues<DeliveryTerms>();

        // Assert
        allTerms.Should().HaveCount(10);
        allTerms.Should().Contain(expectedTerms);
    }

    [Theory]
    [InlineData("FOB", DeliveryTerms.FOB)]
    [InlineData("CIF", DeliveryTerms.CIF)]
    [InlineData("EXW", DeliveryTerms.EXW)]
    [InlineData("fob", DeliveryTerms.FOB)]
    [InlineData("cif", DeliveryTerms.CIF)]
    public void DeliveryTerms_ShouldParseFromString(string termString, DeliveryTerms expectedTerm)
    {
        // Act
        var success = Enum.TryParse<DeliveryTerms>(termString, true, out var parsedTerm);

        // Assert
        success.Should().BeTrue();
        parsedTerm.Should().Be(expectedTerm);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData("FOB_INVALID")]
    public void DeliveryTerms_ShouldFailToParse_InvalidStrings(string invalidString)
    {
        // Act
        var success = Enum.TryParse<DeliveryTerms>(invalidString, true, out var _);

        // Assert
        success.Should().BeFalse();
    }

    [Fact]
    public void DeliveryTerms_ToString_ShouldReturnCorrectNames()
    {
        // Test a few key delivery terms
        DeliveryTerms.FOB.ToString().Should().Be("FOB");
        DeliveryTerms.CIF.ToString().Should().Be("CIF");
        DeliveryTerms.EXW.ToString().Should().Be("EXW");
        DeliveryTerms.DDP.ToString().Should().Be("DDP");
    }

    [Fact]
    public void DeliveryTerms_AllValues_ShouldBeUnique()
    {
        // Arrange
        var allTerms = Enum.GetValues<DeliveryTerms>();
        var termValues = allTerms.Cast<int>().ToArray();

        // Assert
        termValues.Should().OnlyHaveUniqueItems();
        termValues.Should().AllSatisfy(value => value.Should().BeGreaterThan(0));
    }

    #endregion

    #region ContractPaymentMethod Tests

    [Theory]
    [InlineData(ContractPaymentMethod.TT, 1)]
    [InlineData(ContractPaymentMethod.LC, 2)]
    [InlineData(ContractPaymentMethod.CAD, 3)]
    [InlineData(ContractPaymentMethod.SBLC, 4)]
    [InlineData(ContractPaymentMethod.DP, 5)]
    public void ContractPaymentMethod_ShouldHaveCorrectValues(ContractPaymentMethod method, int expectedValue)
    {
        // Act & Assert
        ((int)method).Should().Be(expectedValue);
    }

    [Fact]
    public void ContractPaymentMethod_ShouldContainAllExpectedValues()
    {
        // Arrange
        var expectedMethods = new[]
        {
            ContractPaymentMethod.TT, ContractPaymentMethod.LC, ContractPaymentMethod.SBLC,
            ContractPaymentMethod.DP, ContractPaymentMethod.CAD
        };

        // Act
        var allMethods = Enum.GetValues<ContractPaymentMethod>();

        // Assert
        allMethods.Should().HaveCount(5);
        allMethods.Should().Contain(expectedMethods);
    }

    [Theory]
    [InlineData("TT", ContractPaymentMethod.TT)]
    [InlineData("LC", ContractPaymentMethod.LC)]
    [InlineData("SBLC", ContractPaymentMethod.SBLC)]
    [InlineData("tt", ContractPaymentMethod.TT)]
    [InlineData("lc", ContractPaymentMethod.LC)]
    public void ContractPaymentMethod_ShouldParseFromString(string methodString, ContractPaymentMethod expectedMethod)
    {
        // Act
        var success = Enum.TryParse<ContractPaymentMethod>(methodString, true, out var parsedMethod);

        // Assert
        success.Should().BeTrue();
        parsedMethod.Should().Be(expectedMethod);
    }

    [Fact]
    public void ContractPaymentMethod_ToString_ShouldReturnCorrectNames()
    {
        // Test all payment methods
        ContractPaymentMethod.TT.ToString().Should().Be("TT");
        ContractPaymentMethod.LC.ToString().Should().Be("LC");
        ContractPaymentMethod.SBLC.ToString().Should().Be("SBLC");
        ContractPaymentMethod.DP.ToString().Should().Be("DP");
        ContractPaymentMethod.CAD.ToString().Should().Be("CAD");
    }

    [Fact]
    public void ContractPaymentMethod_AllValues_ShouldBeUnique()
    {
        // Arrange
        var allMethods = Enum.GetValues<ContractPaymentMethod>();
        var methodValues = allMethods.Cast<int>().ToArray();

        // Assert
        methodValues.Should().OnlyHaveUniqueItems();
        methodValues.Should().AllSatisfy(value => value.Should().BeGreaterThan(0));
    }

    #endregion

    #region ShippingStatus Tests

    [Theory]
    [InlineData(ShippingStatus.Planned, 1)]
    [InlineData(ShippingStatus.Loading, 2)]
    [InlineData(ShippingStatus.InTransit, 3)]
    [InlineData(ShippingStatus.Discharged, 4)]
    [InlineData(ShippingStatus.Cancelled, 5)]
    public void ShippingStatus_ShouldHaveCorrectValues(ShippingStatus status, int expectedValue)
    {
        // Act & Assert
        ((int)status).Should().Be(expectedValue);
    }

    [Fact]
    public void ShippingStatus_ShouldContainAllExpectedValues()
    {
        // Arrange
        var expectedStatuses = new[]
        {
            ShippingStatus.Planned, ShippingStatus.Loading, ShippingStatus.InTransit,
            ShippingStatus.Discharged, ShippingStatus.Cancelled
        };

        // Act
        var allStatuses = Enum.GetValues<ShippingStatus>();

        // Assert
        allStatuses.Should().HaveCount(5);
        allStatuses.Should().Contain(expectedStatuses);
    }

    [Theory]
    [InlineData("Planned", ShippingStatus.Planned)]
    [InlineData("Loading", ShippingStatus.Loading)]
    [InlineData("InTransit", ShippingStatus.InTransit)]
    [InlineData("planned", ShippingStatus.Planned)]
    [InlineData("loading", ShippingStatus.Loading)]
    public void ShippingStatus_ShouldParseFromString(string statusString, ShippingStatus expectedStatus)
    {
        // Act
        var success = Enum.TryParse<ShippingStatus>(statusString, true, out var parsedStatus);

        // Assert
        success.Should().BeTrue();
        parsedStatus.Should().Be(expectedStatus);
    }

    [Fact]
    public void ShippingStatus_ToString_ShouldReturnCorrectNames()
    {
        // Test all shipping statuses
        ShippingStatus.Planned.ToString().Should().Be("Planned");
        ShippingStatus.Loading.ToString().Should().Be("Loading");
        ShippingStatus.InTransit.ToString().Should().Be("InTransit");
        ShippingStatus.Discharged.ToString().Should().Be("Discharged");
        ShippingStatus.Cancelled.ToString().Should().Be("Cancelled");
    }

    [Fact]
    public void ShippingStatus_AllValues_ShouldBeUnique()
    {
        // Arrange
        var allStatuses = Enum.GetValues<ShippingStatus>();
        var statusValues = allStatuses.Cast<int>().ToArray();

        // Assert
        statusValues.Should().OnlyHaveUniqueItems();
        statusValues.Should().AllSatisfy(value => value.Should().BeGreaterThan(0));
    }

    [Fact]
    public void ShippingStatus_ShouldRepresentLogicalWorkflow()
    {
        // Test that the enum values represent a logical workflow
        var workflowOrder = new[]
        {
            ShippingStatus.Planned,
            ShippingStatus.Loading,
            ShippingStatus.InTransit,
            ShippingStatus.Discharged
        };

        // Cancelled can happen at any time, so it's not in the sequence
        for (int i = 0; i < workflowOrder.Length - 1; i++)
        {
            ((int)workflowOrder[i]).Should().BeLessThan((int)workflowOrder[i + 1]);
        }
    }

    #endregion

    #region PricingEventType Tests

    [Theory]
    [InlineData(PricingEventType.BL, 1)]
    [InlineData(PricingEventType.NOR, 2)]
    [InlineData(PricingEventType.COD, 3)]
    public void PricingEventType_ShouldHaveCorrectValues(PricingEventType eventType, int expectedValue)
    {
        // Act & Assert
        ((int)eventType).Should().Be(expectedValue);
    }

    [Fact]
    public void PricingEventType_ShouldContainAllExpectedValues()
    {
        // Arrange
        var expectedEventTypes = new[]
        {
            PricingEventType.BL, PricingEventType.NOR, PricingEventType.COD
        };

        // Act
        var allEventTypes = Enum.GetValues<PricingEventType>();

        // Assert
        allEventTypes.Should().HaveCount(3);
        allEventTypes.Should().Contain(expectedEventTypes);
    }

    [Theory]
    [InlineData("BL", PricingEventType.BL)]
    [InlineData("NOR", PricingEventType.NOR)]
    [InlineData("COD", PricingEventType.COD)]
    [InlineData("bl", PricingEventType.BL)]
    [InlineData("nor", PricingEventType.NOR)]
    public void PricingEventType_ShouldParseFromString(string eventTypeString, PricingEventType expectedEventType)
    {
        // Act
        var success = Enum.TryParse<PricingEventType>(eventTypeString, true, out var parsedEventType);

        // Assert
        success.Should().BeTrue();
        parsedEventType.Should().Be(expectedEventType);
    }

    [Fact]
    public void PricingEventType_ToString_ShouldReturnCorrectNames()
    {
        // Test all pricing event types
        PricingEventType.BL.ToString().Should().Be("BL");
        PricingEventType.NOR.ToString().Should().Be("NOR");
        PricingEventType.COD.ToString().Should().Be("COD");
    }

    [Fact]
    public void PricingEventType_AllValues_ShouldBeUnique()
    {
        // Arrange
        var allEventTypes = Enum.GetValues<PricingEventType>();
        var eventTypeValues = allEventTypes.Cast<int>().ToArray();

        // Assert
        eventTypeValues.Should().OnlyHaveUniqueItems();
        eventTypeValues.Should().AllSatisfy(value => value.Should().BeGreaterThan(0));
    }

    #endregion

    #region Cross-Enum Tests

    [Fact]
    public void AllEnums_ShouldStartFromOne()
    {
        // Test that all enums start from 1 (database-friendly)
        var deliveryTermsMin = Enum.GetValues<DeliveryTerms>().Cast<int>().Min();
        var paymentMethodMin = Enum.GetValues<ContractPaymentMethod>().Cast<int>().Min();
        var shippingStatusMin = Enum.GetValues<ShippingStatus>().Cast<int>().Min();
        var pricingEventMin = Enum.GetValues<PricingEventType>().Cast<int>().Min();

        deliveryTermsMin.Should().Be(1);
        paymentMethodMin.Should().Be(1);
        shippingStatusMin.Should().Be(1);
        pricingEventMin.Should().Be(1);
    }

    [Fact]
    public void AllEnums_ShouldHaveConsecutiveValues()
    {
        // Test DeliveryTerms
        var deliveryTermsValues = Enum.GetValues<DeliveryTerms>().Cast<int>().OrderBy(x => x).ToArray();
        for (int i = 0; i < deliveryTermsValues.Length; i++)
        {
            deliveryTermsValues[i].Should().Be(i + 1);
        }

        // Test ContractPaymentMethod
        var paymentMethodValues = Enum.GetValues<ContractPaymentMethod>().Cast<int>().OrderBy(x => x).ToArray();
        for (int i = 0; i < paymentMethodValues.Length; i++)
        {
            paymentMethodValues[i].Should().Be(i + 1);
        }

        // Test ShippingStatus
        var shippingStatusValues = Enum.GetValues<ShippingStatus>().Cast<int>().OrderBy(x => x).ToArray();
        for (int i = 0; i < shippingStatusValues.Length; i++)
        {
            shippingStatusValues[i].Should().Be(i + 1);
        }

        // Test PricingEventType
        var pricingEventValues = Enum.GetValues<PricingEventType>().Cast<int>().OrderBy(x => x).ToArray();
        for (int i = 0; i < pricingEventValues.Length; i++)
        {
            pricingEventValues[i].Should().Be(i + 1);
        }
    }

    [Fact]
    public void AllEnums_ShouldBeDefinedProperly()
    {
        // Test that all enum values are properly defined (no gaps or duplicates)
        TestEnumIntegrity<DeliveryTerms>();
        TestEnumIntegrity<ContractPaymentMethod>();
        TestEnumIntegrity<ShippingStatus>();
        TestEnumIntegrity<PricingEventType>();
    }

    [Fact]
    public void AllEnums_ShouldSupportIsDefined()
    {
        // Test valid values
        Enum.IsDefined(typeof(DeliveryTerms), DeliveryTerms.FOB).Should().BeTrue();
        Enum.IsDefined(typeof(ContractPaymentMethod), ContractPaymentMethod.LC).Should().BeTrue();
        Enum.IsDefined(typeof(ShippingStatus), ShippingStatus.InTransit).Should().BeTrue();
        Enum.IsDefined(typeof(PricingEventType), PricingEventType.BL).Should().BeTrue();

        // Test invalid values
        Enum.IsDefined(typeof(DeliveryTerms), 999).Should().BeFalse();
        Enum.IsDefined(typeof(ContractPaymentMethod), 999).Should().BeFalse();
        Enum.IsDefined(typeof(ShippingStatus), 999).Should().BeFalse();
        Enum.IsDefined(typeof(PricingEventType), 999).Should().BeFalse();
    }

    private static void TestEnumIntegrity<TEnum>() where TEnum : struct, Enum
    {
        var enumValues = Enum.GetValues<TEnum>();
        var intValues = enumValues.Select(e => Convert.ToInt32(e)).ToArray();

        // Should have unique values
        intValues.Should().OnlyHaveUniqueItems();

        // Should all be positive
        intValues.Should().AllSatisfy(value => value.Should().BeGreaterThan(0));

        // Should have at least one value
        intValues.Should().NotBeEmpty();

        // All values should be defined
        foreach (var enumValue in enumValues)
        {
            Enum.IsDefined(typeof(TEnum), enumValue).Should().BeTrue();
        }
    }

    #endregion

    #region Integration with Business Logic Tests

    [Fact]
    public void DeliveryTerms_ShouldSupportCommonOilTradingTerms()
    {
        // Verify that we have the most common oil trading delivery terms
        var commonTerms = new[]
        {
            DeliveryTerms.FOB,  // Very common for oil trading
            DeliveryTerms.CFR,  // Common for international shipping
            DeliveryTerms.CIF,  // Very common internationally
            DeliveryTerms.DES   // Common for oil deliveries
        };

        var allTerms = Enum.GetValues<DeliveryTerms>();
        
        foreach (var term in commonTerms)
        {
            allTerms.Should().Contain(term, $"Oil trading should support {term} delivery terms");
        }
    }

    [Fact]
    public void ContractPaymentMethod_ShouldSupportCommonOilTradingMethods()
    {
        // Verify that we have the most common oil trading payment methods
        var commonMethods = new[]
        {
            ContractPaymentMethod.LC,   // Very common for international oil trading
            ContractPaymentMethod.TT,   // Common for spot transactions
            ContractPaymentMethod.SBLC  // Common for large transactions
        };

        var allMethods = Enum.GetValues<ContractPaymentMethod>();
        
        foreach (var method in commonMethods)
        {
            allMethods.Should().Contain(method, $"Oil trading should support {method} payment method");
        }
    }

    [Fact]
    public void ShippingStatus_ShouldSupportCompleteShippingLifecycle()
    {
        // Verify that we can represent the complete shipping lifecycle
        var requiredStatuses = new[]
        {
            ShippingStatus.Planned,     // Initial state
            ShippingStatus.Loading,     // Active loading
            ShippingStatus.InTransit,   // Vessel sailing
            ShippingStatus.Discharged,  // Final state
            ShippingStatus.Cancelled    // Error state
        };

        var allStatuses = Enum.GetValues<ShippingStatus>();
        
        foreach (var status in requiredStatuses)
        {
            allStatuses.Should().Contain(status, $"Shipping lifecycle should support {status} status");
        }
    }

    [Fact]
    public void PricingEventType_ShouldSupportOilTradingPricingEvents()
    {
        // Verify that we have the key pricing events for oil trading
        var requiredEvents = new[]
        {
            PricingEventType.BL,   // Bill of Lading - very important for pricing
            PricingEventType.NOR,  // Notice of Readiness - timing matters
            PricingEventType.COD   // Certificate of Discharge - final pricing
        };

        var allEvents = Enum.GetValues<PricingEventType>();
        
        foreach (var eventType in requiredEvents)
        {
            allEvents.Should().Contain(eventType, $"Oil trading pricing should support {eventType} events");
        }
    }

    #endregion
}