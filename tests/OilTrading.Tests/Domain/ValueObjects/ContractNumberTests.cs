using FluentAssertions;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using Xunit;

namespace OilTrading.Tests.Domain.ValueObjects;

public class ContractNumberTests
{
    [Theory]
    [InlineData(2024, ContractType.CARGO, 1, "ITGR-2024-CARGO-B0001")]
    [InlineData(2025, ContractType.EXW, 123, "ITGR-2025-EXW-B0123")]
    [InlineData(2023, ContractType.DEL, 9999, "ITGR-2023-DEL-B9999")]
    [InlineData(2000, ContractType.CARGO, 5, "ITGR-2000-CARGO-B0005")]
    [InlineData(3000, ContractType.EXW, 1000, "ITGR-3000-EXW-B1000")]
    public void Create_ShouldGenerateCorrectFormat_WhenValidInputProvided(int year, ContractType type, int serialNumber, string expectedValue)
    {
        // Act
        var contractNumber = ContractNumber.Create(year, type, serialNumber);

        // Assert
        contractNumber.Value.Should().Be(expectedValue);
        contractNumber.Year.Should().Be(year.ToString());
        contractNumber.Type.Should().Be(type);
        contractNumber.SerialNumber.Should().Be(serialNumber);
        contractNumber.ToString().Should().Be(expectedValue);
        
        // Test implicit string conversion
        string implicitValue = contractNumber;
        implicitValue.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(1999)] // Too early
    [InlineData(3001)] // Too late
    [InlineData(0)]    // Invalid
    [InlineData(-2024)] // Negative
    public void Create_ShouldThrowDomainException_WhenYearIsInvalid(int invalidYear)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            ContractNumber.Create(invalidYear, ContractType.CARGO, 1));
        
        exception.Message.Should().Be("Year must be between 2000 and 3000");
    }

    [Theory]
    [InlineData(0)]     // Too small
    [InlineData(10000)] // Too large
    [InlineData(-1)]    // Negative
    [InlineData(-100)]  // Very negative
    public void Create_ShouldThrowDomainException_WhenSerialNumberIsInvalid(int invalidSerial)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            ContractNumber.Create(2024, ContractType.CARGO, invalidSerial));
        
        exception.Message.Should().Be("Serial number must be between 1 and 9999");
    }

    [Theory]
    [InlineData("ITGR-2024-CARGO-B0001")]
    [InlineData("ITGR-2025-EXW-B0123")]
    [InlineData("ITGR-2023-DEL-B9999")]
    [InlineData("  ITGR-2024-CARGO-B0001  ")] // With whitespace
    [InlineData("itgr-2024-cargo-b0001")]     // Lowercase
    public void Parse_ShouldParseCorrectly_WhenValidFormatProvided(string input)
    {
        // Act
        var contractNumber = ContractNumber.Parse(input);

        // Assert
        contractNumber.Should().NotBeNull();
        contractNumber.Value.Should().Be(input.Trim().ToUpper());
        contractNumber.Year.Should().MatchRegex(@"^\d{4}$");
        contractNumber.Type.Should().BeOneOf(ContractType.CARGO, ContractType.EXW, ContractType.DEL);
        contractNumber.SerialNumber.Should().BeInRange(1, 9999);
    }

    [Theory]
    [InlineData("")]                           // Empty
    [InlineData("   ")]                        // Whitespace only
    [InlineData(null)]                         // Null
    public void Parse_ShouldThrowDomainException_WhenNullOrEmpty(string invalidInput)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => ContractNumber.Parse(invalidInput));
        exception.Message.Should().Contain("Contract number cannot be null or empty");
    }

    [Theory]
    [InlineData("INVALID")]                    // External format
    [InlineData("PC-2024-001")]               // External format
    [InlineData("ITGR-2024-INVALID-B0001")]   // External format with invalid type
    [InlineData("ITGR-XXXX-CARGO-B0001")]     // External format with invalid year
    [InlineData("ITGR-2024-CARGO-BXXXX")]     // External format with invalid serial
    [InlineData("2024-CARGO-B0001")]          // External format missing prefix
    [InlineData("ITGR-2024-CARGO-0001")]      // External format missing B prefix
    [InlineData("ITGR-24-CARGO-B0001")]       // External format short year
    [InlineData("ITGR-2024-CARGO-B00001")]    // External format too many digits
    [InlineData("ITGR_2024_CARGO_B0001")]     // External format wrong separators
    [InlineData("ABC123")]                     // Simple alphanumeric
    [InlineData("CONTRACT-2024-XYZ")]         // External format
    public void Parse_ShouldAcceptAsExternalContractNumber_WhenNotMatchingInternalFormat(string externalInput)
    {
        // Act
        var contractNumber = ContractNumber.Parse(externalInput);

        // Assert
        contractNumber.Should().NotBeNull();
        contractNumber.Value.Should().Be(externalInput);
        contractNumber.Year.Should().Be(string.Empty); // Placeholder for external
        contractNumber.Type.Should().Be(ContractType.CARGO); // Default for external
        contractNumber.SerialNumber.Should().Be(0); // Placeholder for external
    }

    [Theory]
    [InlineData("ITGR-2024-CARGO-B0001", true)]  // Internal format
    [InlineData("ITGR-2025-EXW-B0123", true)]    // Internal format
    [InlineData("INVALID", true)]                 // External format - now accepted
    [InlineData("PC-2024-001", true)]            // External format - now accepted
    [InlineData("ITGR-2024-INVALID-B0001", true)] // External format - now accepted
    [InlineData("ABC123", true)]                  // External format - now accepted
    [InlineData("", false)]                       // Empty - rejected
    [InlineData(null, false)]                     // Null - rejected
    [InlineData("   ", false)]                    // Whitespace - rejected
    public void TryParse_ShouldReturnCorrectResult_ForValidAndInvalidInputs(string input, bool expectedSuccess)
    {
        // Act
        var success = ContractNumber.TryParse(input, out var contractNumber);

        // Assert
        success.Should().Be(expectedSuccess);

        if (expectedSuccess)
        {
            contractNumber.Should().NotBeNull();
            contractNumber!.Value.Should().Be(input?.Trim().ToUpper() ?? input);
        }
        else
        {
            contractNumber.Should().BeNull();
        }
    }

    [Theory]
    [InlineData(2024, ContractType.CARGO, 1)]
    [InlineData(2025, ContractType.EXW, 5555)]
    [InlineData(2023, ContractType.DEL, 9998)]
    public void NextSerial_ShouldGenerateNextSerialNumber_WhenNotAtMaximum(int year, ContractType type, int currentSerial)
    {
        // Arrange
        var original = ContractNumber.Create(year, type, currentSerial);

        // Act
        var next = original.NextSerial();

        // Assert
        next.Year.Should().Be(original.Year);
        next.Type.Should().Be(original.Type);
        next.SerialNumber.Should().Be(currentSerial + 1);
        next.Value.Should().Contain($"B{(currentSerial + 1):D4}");
    }

    [Fact]
    public void NextSerial_ShouldThrowDomainException_WhenAtMaximumSerial()
    {
        // Arrange
        var maxSerial = ContractNumber.Create(2024, ContractType.CARGO, 9999);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => maxSerial.NextSerial());
        exception.Message.Should().Be("Cannot generate next serial number: maximum reached");
    }

    [Fact]
    public void NextSerial_ShouldThrowDomainException_WhenCalledOnExternalContractNumber()
    {
        // Arrange
        var externalContract = ContractNumber.Parse("PC-2024-001");

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => externalContract.NextSerial());
        exception.Message.Should().Be("Cannot generate next serial number for external contract number");
    }

    [Fact]
    public void Equality_ShouldWork_ForSameContractNumbers()
    {
        // Arrange
        var contract1 = ContractNumber.Create(2024, ContractType.CARGO, 1);
        var contract2 = ContractNumber.Parse("ITGR-2024-CARGO-B0001");

        // Assert
        contract1.Should().Be(contract2);
        contract1.Equals(contract2).Should().BeTrue();
        contract1.GetHashCode().Should().Be(contract2.GetHashCode());
    }

    [Fact]
    public void Equality_ShouldFail_ForDifferentContractNumbers()
    {
        // Arrange
        var contract1 = ContractNumber.Create(2024, ContractType.CARGO, 1);
        var contract2 = ContractNumber.Create(2024, ContractType.CARGO, 2);
        var contract3 = ContractNumber.Create(2024, ContractType.EXW, 1);
        var contract4 = ContractNumber.Create(2025, ContractType.CARGO, 1);

        // Assert
        contract1.Should().NotBe(contract2);
        contract1.Should().NotBe(contract3);
        contract1.Should().NotBe(contract4);
    }

    [Theory]
    [InlineData(ContractType.CARGO)]
    [InlineData(ContractType.EXW)]
    [InlineData(ContractType.DEL)]
    public void ContractType_ShouldHandleAllEnumValues(ContractType type)
    {
        // Act
        var contractNumber = ContractNumber.Create(2024, type, 1);

        // Assert
        contractNumber.Type.Should().Be(type);
        contractNumber.Value.Should().Contain(type.ToString());
    }

    [Fact]
    public void Parse_ShouldHandleCaseInsensitive_ContractTypes()
    {
        // Arrange & Act
        var cargoLower = ContractNumber.Parse("ITGR-2024-cargo-B0001");
        var exwMixed = ContractNumber.Parse("ITGR-2024-Exw-B0001");
        var delUpper = ContractNumber.Parse("ITGR-2024-DEL-B0001");

        // Assert
        cargoLower.Type.Should().Be(ContractType.CARGO);
        exwMixed.Type.Should().Be(ContractType.EXW);
        delUpper.Type.Should().Be(ContractType.DEL);
    }

    [Fact]
    public void ContractNumber_ShouldMaintainValueObjectSemantics()
    {
        // Arrange
        var contract1 = ContractNumber.Create(2024, ContractType.CARGO, 1);
        var contract2 = ContractNumber.Create(2024, ContractType.CARGO, 1);

        // Assert - Value object equality
        contract1.Should().Be(contract2);
        (contract1 == contract2).Should().BeFalse(); // Reference equality should be false
        contract1.Equals(contract2).Should().BeTrue(); // Value equality should be true
        
        // HashCode should be same for equal value objects
        contract1.GetHashCode().Should().Be(contract2.GetHashCode());
    }

    [Theory]
    [InlineData(2024, ContractType.CARGO, 1, 2024, ContractType.CARGO, 1, true)]
    [InlineData(2024, ContractType.CARGO, 1, 2024, ContractType.CARGO, 2, false)]
    [InlineData(2024, ContractType.CARGO, 1, 2024, ContractType.EXW, 1, false)]
    [InlineData(2024, ContractType.CARGO, 1, 2025, ContractType.CARGO, 1, false)]
    public void ValueObjectEquality_ShouldWorkCorrectly(
        int year1, ContractType type1, int serial1,
        int year2, ContractType type2, int serial2,
        bool expectedEqual)
    {
        // Arrange
        var contract1 = ContractNumber.Create(year1, type1, serial1);
        var contract2 = ContractNumber.Create(year2, type2, serial2);

        // Act & Assert
        contract1.Equals(contract2).Should().Be(expectedEqual);
        
        if (expectedEqual)
        {
            contract1.GetHashCode().Should().Be(contract2.GetHashCode());
        }
    }

    [Fact]
    public void SerialNumber_ShouldBePaddedWithZeros()
    {
        // Test that serial numbers are properly zero-padded
        var tests = new[]
        {
            (1, "B0001"),
            (12, "B0012"),
            (123, "B0123"),
            (1234, "B1234"),
            (9999, "B9999")
        };

        foreach (var (serial, expectedPadding) in tests)
        {
            var contract = ContractNumber.Create(2024, ContractType.CARGO, serial);
            contract.Value.Should().EndWith(expectedPadding);
        }
    }
}