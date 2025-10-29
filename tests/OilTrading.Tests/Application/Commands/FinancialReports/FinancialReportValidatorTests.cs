using FluentAssertions;
using FluentValidation.TestHelper;
using OilTrading.Application.Commands.FinancialReports;
using Xunit;

namespace OilTrading.Tests.Application.Commands.FinancialReports;

public class FinancialReportValidatorTests
{
    private readonly CreateFinancialReportCommandValidator _validator;
    private readonly Guid _validTradingPartnerId = Guid.NewGuid();

    public FinancialReportValidatorTests()
    {
        _validator = new CreateFinancialReportCommandValidator();
    }

    #region Trading Partner Validation

    [Fact]
    public void TradingPartnerId_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TradingPartnerId = Guid.Empty;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TradingPartnerId)
            .WithErrorMessage("Trading partner is required");
    }

    [Fact]
    public void TradingPartnerId_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TradingPartnerId);
    }

    #endregion

    #region Report Date Validation

    [Fact]
    public void ReportStartDate_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.ReportStartDate = default;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReportStartDate)
            .WithErrorMessage("Report start date is required");
    }

    [Fact]
    public void ReportEndDate_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.ReportEndDate = default;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReportEndDate)
            .WithErrorMessage("Report end date is required");
    }

    [Fact]
    public void ReportStartDate_WhenAfterEndDate_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.ReportStartDate = DateTime.UtcNow.AddDays(-10);
        command.ReportEndDate = DateTime.UtcNow.AddDays(-20);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReportStartDate)
            .WithErrorMessage("Report start date must be before end date");
    }

    [Fact]
    public void ReportEndDate_WhenInFuture_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.ReportStartDate = DateTime.UtcNow.AddDays(1);
        command.ReportEndDate = DateTime.UtcNow.AddDays(2);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReportEndDate)
            .WithErrorMessage("Report end date cannot be in the future");
    }

    [Fact]
    public void ReportPeriod_WhenExceeds366Days_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.ReportStartDate = DateTime.UtcNow.AddDays(-400);
        command.ReportEndDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ReportPeriod_WhenLessThanOneDay_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        var date = DateTime.UtcNow.AddDays(-1).Date;
        command.ReportStartDate = date;
        command.ReportEndDate = date; // Same day = 0 days

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(2)] // 2 days (at least 1 day difference)
    [InlineData(30)] // 1 month
    [InlineData(90)] // 1 quarter
    [InlineData(365)] // 1 year
    [InlineData(366)] // Leap year
    public void ReportPeriod_WithValidDays_ShouldNotHaveValidationError(int days)
    {
        // Arrange
        var command = CreateValidCommand();
        command.ReportStartDate = DateTime.UtcNow.AddDays(-days).Date;
        command.ReportEndDate = DateTime.UtcNow.AddDays(-1).Date;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Financial Position Data Validation

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public void TotalAssets_WhenNegative_ShouldHaveValidationError(decimal negativeValue)
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = negativeValue;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TotalAssets)
            .WithErrorMessage("Total assets cannot be negative");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(1000000)]
    public void TotalAssets_WhenNonNegative_ShouldNotHaveValidationError(decimal value)
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = value;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TotalAssets);
    }

    [Fact]
    public void TotalAssets_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = null;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TotalAssets);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void TotalLiabilities_WhenNegative_ShouldHaveValidationError(decimal negativeValue)
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalLiabilities = negativeValue;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TotalLiabilities)
            .WithErrorMessage("Total liabilities cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CurrentAssets_WhenNegative_ShouldHaveValidationError(decimal negativeValue)
    {
        // Arrange
        var command = CreateValidCommand();
        command.CurrentAssets = negativeValue;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentAssets)
            .WithErrorMessage("Current assets cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CurrentLiabilities_WhenNegative_ShouldHaveValidationError(decimal negativeValue)
    {
        // Arrange
        var command = CreateValidCommand();
        command.CurrentLiabilities = negativeValue;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentLiabilities)
            .WithErrorMessage("Current liabilities cannot be negative");
    }

    #endregion

    #region Logical Relationships Validation

    [Fact]
    public void CurrentAssets_WhenExceedsTotalAssets_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = 1000m;
        command.CurrentAssets = 1500m;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentAssets)
            .WithErrorMessage("Current assets cannot exceed total assets");
    }

    [Fact]
    public void CurrentAssets_WhenEqualToTotalAssets_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = 1000m;
        command.CurrentAssets = 1000m;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CurrentAssets_WhenLessThanTotalAssets_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = 1000m;
        command.CurrentAssets = 800m;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CurrentLiabilities_WhenExceedsTotalLiabilities_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalLiabilities = 500m;
        command.CurrentLiabilities = 800m;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentLiabilities)
            .WithErrorMessage("Current liabilities cannot exceed total liabilities");
    }

    [Fact]
    public void CurrentLiabilities_WhenEqualToTotalLiabilities_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalLiabilities = 500m;
        command.CurrentLiabilities = 500m;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LogicalRelationships_WhenEitherValueIsNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = null;
        command.CurrentAssets = 1500m; // Would exceed total if total wasn't null

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region CreatedBy Validation

    [Fact]
    public void CreatedBy_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.CreatedBy = "";

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatedBy);
    }

    [Fact]
    public void CreatedBy_WhenWhitespace_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.CreatedBy = "   ";

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatedBy);
    }

    [Fact]
    public void CreatedBy_WhenNull_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.CreatedBy = null!;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatedBy);
    }

    [Fact]
    public void CreatedBy_WhenExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.CreatedBy = new string('A', 101); // 101 characters

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatedBy)
            .WithErrorMessage("Created by is required and must not exceed 100 characters");
    }

    [Theory]
    [InlineData("user")]
    [InlineData("test.user@company.com")]
    [InlineData("A")]
    public void CreatedBy_WhenValidLength_ShouldNotHaveValidationError(string createdBy)
    {
        // Arrange
        var command = CreateValidCommand();
        command.CreatedBy = createdBy;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CreatedBy);
    }

    [Fact]
    public void CreatedBy_WithMaxLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.CreatedBy = new string('A', 100); // Exactly 100 characters

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CreatedBy);
    }

    #endregion

    #region Performance Data Validation

    [Theory]
    [InlineData(-1000)]
    [InlineData(-0.01)]
    public void Revenue_WhenNegative_ShouldNotHaveValidationError(decimal negativeRevenue)
    {
        // Note: Revenue can be negative (rare but possible in loss scenarios)
        
        // Arrange
        var command = CreateValidCommand();
        command.Revenue = negativeRevenue;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Revenue);
    }

    [Theory]
    [InlineData(-1000)]
    [InlineData(-0.01)]
    public void NetProfit_WhenNegative_ShouldNotHaveValidationError(decimal negativeProfit)
    {
        // Note: Net profit can be negative (losses)
        
        // Arrange
        var command = CreateValidCommand();
        command.NetProfit = negativeProfit;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.NetProfit);
    }

    [Theory]
    [InlineData(-1000)]
    [InlineData(-0.01)]
    public void OperatingCashFlow_WhenNegative_ShouldNotHaveValidationError(decimal negativeCashFlow)
    {
        // Note: Operating cash flow can be negative
        
        // Arrange
        var command = CreateValidCommand();
        command.OperatingCashFlow = negativeCashFlow;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OperatingCashFlow);
    }

    [Fact]
    public void PerformanceData_WhenAllNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.Revenue = null;
        command.NetProfit = null;
        command.OperatingCashFlow = null;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Revenue);
        result.ShouldNotHaveValidationErrorFor(x => x.NetProfit);
        result.ShouldNotHaveValidationErrorFor(x => x.OperatingCashFlow);
    }

    #endregion

    #region Complete Validation Scenarios

    [Fact]
    public void CompleteValidCommand_ShouldPassAllValidation()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MinimalValidCommand_ShouldPassAllValidation()
    {
        // Arrange
        var command = new CreateFinancialReportCommand
        {
            TradingPartnerId = _validTradingPartnerId,
            ReportStartDate = DateTime.UtcNow.AddDays(-30),
            ReportEndDate = DateTime.UtcNow.AddDays(-1),
            CreatedBy = "test.user"
            // All financial data null - should be valid
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CommandWithAllErrors_ShouldHaveMultipleValidationErrors()
    {
        // Arrange
        var command = new CreateFinancialReportCommand
        {
            TradingPartnerId = Guid.Empty,
            ReportStartDate = default,
            ReportEndDate = default,
            TotalAssets = -100,
            TotalLiabilities = -50,
            CurrentAssets = -25,
            CurrentLiabilities = -10,
            CreatedBy = ""
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TradingPartnerId);
        result.ShouldHaveValidationErrorFor(x => x.ReportStartDate);
        result.ShouldHaveValidationErrorFor(x => x.ReportEndDate);
        result.ShouldHaveValidationErrorFor(x => x.TotalAssets);
        result.ShouldHaveValidationErrorFor(x => x.TotalLiabilities);
        result.ShouldHaveValidationErrorFor(x => x.CurrentAssets);
        result.ShouldHaveValidationErrorFor(x => x.CurrentLiabilities);
        result.ShouldHaveValidationErrorFor(x => x.CreatedBy);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ReportPeriod_ExactlyOneDay_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        var startDate = DateTime.UtcNow.AddDays(-2).Date;
        var endDate = startDate.AddDays(1); // Exactly 1 day difference
        command.ReportStartDate = startDate;
        command.ReportEndDate = endDate;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReportPeriod_Exactly366Days_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.ReportStartDate = DateTime.UtcNow.AddDays(-366);
        command.ReportEndDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReportEndDate_ExactlyToday_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.ReportStartDate = DateTime.UtcNow.Date.AddDays(-1);
        command.ReportEndDate = DateTime.UtcNow.Date; // Today is valid per validator

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReportEndDate);
    }

    [Fact]
    public void FinancialValues_ZeroValues_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = 0;
        command.TotalLiabilities = 0;
        command.NetAssets = 0;
        command.CurrentAssets = 0;
        command.CurrentLiabilities = 0;
        command.Revenue = 0;
        command.NetProfit = 0;
        command.OperatingCashFlow = 0;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FinancialValues_VeryLargeNumbers_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = decimal.MaxValue;
        command.TotalLiabilities = decimal.MaxValue / 2;
        command.NetAssets = decimal.MaxValue / 2;
        command.CurrentAssets = decimal.MaxValue / 2;
        command.CurrentLiabilities = decimal.MaxValue / 4;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Helper Methods

    private CreateFinancialReportCommand CreateValidCommand()
    {
        return new CreateFinancialReportCommand
        {
            TradingPartnerId = _validTradingPartnerId,
            ReportStartDate = DateTime.UtcNow.AddDays(-90),
            ReportEndDate = DateTime.UtcNow.AddDays(-1),
            TotalAssets = 10000m,
            TotalLiabilities = 6000m,
            NetAssets = 4000m,
            CurrentAssets = 5000m,
            CurrentLiabilities = 3000m,
            Revenue = 15000m,
            NetProfit = 1500m,
            OperatingCashFlow = 2000m,
            CreatedBy = "test.user"
        };
    }

    #endregion
}