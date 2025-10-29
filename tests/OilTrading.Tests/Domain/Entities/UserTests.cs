using FluentAssertions;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using Xunit;
using UserRoleEnum = OilTrading.Core.Entities.UserRole;

namespace OilTrading.Tests.Domain.Entities;

public class UserTests
{
    #region Constructor and Property Tests

    [Fact]
    public void User_ShouldHaveDefaultValues_WhenCreated()
    {
        // Act
        var user = new User();

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.Email.Should().BeEmpty();
        user.FirstName.Should().BeEmpty();
        user.LastName.Should().BeEmpty();
        user.PasswordHash.Should().BeEmpty();
        // Role defaults to 0 since it's not explicitly initialized
        user.Role.Should().Be((UserRoleEnum)0);
        user.IsActive.Should().BeTrue();
        user.LastLoginAt.Should().BeNull();
        user.FullName.Should().Be(" "); // FirstName + " " + LastName when both empty
        user.PurchaseContracts.Should().NotBeNull().And.BeEmpty();
        user.SalesContracts.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void User_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var user = new User();
        const string email = "john.doe@oiltrading.com";
        const string firstName = "John";
        const string lastName = "Doe";
        const string passwordHash = "hashed_password_123";
        const UserRole role = UserRoleEnum.RiskManager;
        var lastLoginAt = DateTime.UtcNow.AddDays(-1);

        // Act
        user.Email = email;
        user.FirstName = firstName;
        user.LastName = lastName;
        user.PasswordHash = passwordHash;
        user.Role = role;
        user.IsActive = false;
        user.LastLoginAt = lastLoginAt;

        // Assert
        user.Email.Should().Be(email);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.PasswordHash.Should().Be(passwordHash);
        user.Role.Should().Be(role);
        user.IsActive.Should().BeFalse();
        user.LastLoginAt.Should().Be(lastLoginAt);
        user.FullName.Should().Be($"{firstName} {lastName}");
    }

    #endregion

    #region FullName Property Tests

    [Theory]
    [InlineData("John", "Doe", "John Doe")]
    [InlineData("Mary", "Smith", "Mary Smith")]
    [InlineData("", "Johnson", " Johnson")]
    [InlineData("Alice", "", "Alice ")]
    [InlineData("", "", " ")]
    [InlineData("Jean-Pierre", "van der Berg", "Jean-Pierre van der Berg")]
    public void FullName_ShouldCombineFirstAndLastNames(string firstName, string lastName, string expectedFullName)
    {
        // Arrange
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName
        };

        // Act & Assert
        user.FullName.Should().Be(expectedFullName);
    }

    [Fact]
    public void FullName_ShouldUpdateDynamically_WhenNamesChange()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Initial state
        user.FullName.Should().Be("John Doe");

        // Act - Change first name
        user.FirstName = "Jane";

        // Assert
        user.FullName.Should().Be("Jane Doe");

        // Act - Change last name
        user.LastName = "Smith";

        // Assert
        user.FullName.Should().Be("Jane Smith");
    }

    #endregion

    #region UserRole Enum Tests

    [Theory]
    [InlineData(UserRoleEnum.Trader, 1)]
    [InlineData(UserRoleEnum.RiskManager, 2)]
    [InlineData(UserRoleEnum.Administrator, 3)]
    [InlineData(UserRoleEnum.Viewer, 4)]
    public void UserRole_ShouldHaveCorrectValues(UserRole role, int expectedValue)
    {
        // Act & Assert
        ((int)role).Should().Be(expectedValue);
    }

    [Fact]
    public void UserRole_ShouldContainAllExpectedValues()
    {
        // Arrange
        var expectedRoles = new[]
        {
            UserRoleEnum.Trader,
            UserRoleEnum.RiskManager,
            UserRoleEnum.Administrator,
            UserRoleEnum.Viewer
        };

        // Act
        var allRoles = Enum.GetValues<UserRole>();

        // Assert
        allRoles.Should().HaveCount(4);
        allRoles.Should().Contain(expectedRoles);
    }

    [Theory]
    [InlineData("Trader", UserRoleEnum.Trader)]
    [InlineData("RiskManager", UserRoleEnum.RiskManager)]
    [InlineData("Administrator", UserRoleEnum.Administrator)]
    [InlineData("Viewer", UserRoleEnum.Viewer)]
    [InlineData("trader", UserRoleEnum.Trader)]
    [InlineData("riskmanager", UserRoleEnum.RiskManager)]
    public void UserRole_ShouldParseFromString(string roleString, UserRole expectedRole)
    {
        // Act
        var success = Enum.TryParse<UserRole>(roleString, true, out var parsedRole);

        // Assert
        success.Should().BeTrue();
        parsedRole.Should().Be(expectedRole);
    }

    [Fact]
    public void UserRole_ToString_ShouldReturnCorrectNames()
    {
        // Test all user roles
        UserRoleEnum.Trader.ToString().Should().Be("Trader");
        UserRoleEnum.RiskManager.ToString().Should().Be("RiskManager");
        UserRoleEnum.Administrator.ToString().Should().Be("Administrator");
        UserRoleEnum.Viewer.ToString().Should().Be("Viewer");
    }

    [Fact]
    public void UserRole_AllValues_ShouldBeUnique()
    {
        // Arrange
        var allRoles = Enum.GetValues<UserRole>();
        var roleValues = allRoles.Cast<int>().ToArray();

        // Assert
        roleValues.Should().OnlyHaveUniqueItems();
        roleValues.Should().AllSatisfy(value => value.Should().BeGreaterThan(0));
    }

    #endregion

    #region Navigation Properties Tests

    [Fact]
    public void PurchaseContracts_ShouldInitializeAsEmptyCollection()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.PurchaseContracts.Should().NotBeNull();
        user.PurchaseContracts.Should().BeEmpty();
        user.PurchaseContracts.Should().BeAssignableTo<ICollection<PurchaseContract>>();
    }

    [Fact]
    public void SalesContracts_ShouldInitializeAsEmptyCollection()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.SalesContracts.Should().NotBeNull();
        user.SalesContracts.Should().BeEmpty();
        user.SalesContracts.Should().BeAssignableTo<ICollection<SalesContract>>();
    }

    [Fact]
    public void PurchaseContracts_ShouldAllowAddingContracts()
    {
        // Arrange
        var user = new User();
        var contract = CreateMockPurchaseContract();

        // Act
        user.PurchaseContracts.Add(contract);

        // Assert
        user.PurchaseContracts.Should().ContainSingle();
        user.PurchaseContracts.Should().Contain(contract);
    }

    [Fact]
    public void SalesContracts_ShouldAllowAddingContracts()
    {
        // Arrange
        var user = new User();
        var contract = CreateMockSalesContract();

        // Act
        user.SalesContracts.Add(contract);

        // Assert
        user.SalesContracts.Should().ContainSingle();
        user.SalesContracts.Should().Contain(contract);
    }

    #endregion

    #region Business Logic Tests

    [Theory]
    [InlineData(UserRoleEnum.Trader, "trader@company.com", "Oil", "Trader")]
    [InlineData(UserRoleEnum.RiskManager, "risk@company.com", "Risk", "Manager")]
    [InlineData(UserRoleEnum.Administrator, "admin@company.com", "System", "Admin")]
    [InlineData(UserRoleEnum.Viewer, "viewer@company.com", "Report", "Viewer")]
    public void User_ShouldRepresentDifferentUserTypes(UserRole role, string email, string firstName, string lastName)
    {
        // Arrange & Act
        var user = new User
        {
            Role = role,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true
        };

        // Assert
        user.Role.Should().Be(role);
        user.Email.Should().Be(email);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.FullName.Should().Be($"{firstName} {lastName}");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void User_ShouldSupportActiveInactiveStates()
    {
        // Arrange
        var activeUser = new User { Email = "active@test.com", IsActive = true };
        var inactiveUser = new User { Email = "inactive@test.com", IsActive = false };

        // Assert
        activeUser.IsActive.Should().BeTrue();
        inactiveUser.IsActive.Should().BeFalse();
    }

    [Fact]
    public void User_ShouldTrackLastLoginTime()
    {
        // Arrange
        var user = new User();
        var loginTime = DateTime.UtcNow;

        // Initially no login
        user.LastLoginAt.Should().BeNull();

        // Act - Record login
        user.LastLoginAt = loginTime;

        // Assert
        user.LastLoginAt.Should().Be(loginTime);
    }

    [Fact]
    public void User_ShouldSupportPasswordHashing()
    {
        // Arrange
        var user = new User();
        const string passwordHash = "bcrypt$2a$10$abcdefghijklmnopqrstuvwxyz";

        // Act
        user.PasswordHash = passwordHash;

        // Assert
        user.PasswordHash.Should().Be(passwordHash);
        user.PasswordHash.Should().NotBeEmpty();
    }

    #endregion

    #region Email Validation Tests

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("john.doe@oiltrading.co.uk")]
    [InlineData("trader123@energy-company.com")]
    [InlineData("risk.manager@trading-house.org")]
    public void User_ShouldAcceptValidEmailFormats(string validEmail)
    {
        // Arrange & Act
        var user = new User { Email = validEmail };

        // Assert
        user.Email.Should().Be(validEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user..double.dot@example.com")]
    public void User_ShouldStoreInvalidEmails_ButValidationShouldBeHandledElsewhere(string invalidEmail)
    {
        // Note: This entity doesn't perform validation - that should be handled at the application layer
        // This test verifies that the entity can store the value, regardless of validity
        
        // Arrange & Act
        var user = new User { Email = invalidEmail };

        // Assert
        user.Email.Should().Be(invalidEmail);
    }

    #endregion

    #region Security Tests

    [Fact]
    public void User_ShouldNotExposePasswordInPlainText()
    {
        // This test ensures that we only store password hashes, never plain passwords
        
        // Arrange
        var user = new User();
        const string passwordHash = "hashed_password_value";

        // Act
        user.PasswordHash = passwordHash;

        // Assert
        user.PasswordHash.Should().Be(passwordHash);
        // No Password property should exist - only PasswordHash
        typeof(User).GetProperty("Password").Should().BeNull("User should not have a plain text Password property");
    }

    [Fact]
    public void User_ShouldAllowEmptyPasswordHash_ForNewUsers()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.PasswordHash.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases and Validation Tests

    [Fact]
    public void User_ShouldHandleEmptyStrings()
    {
        // Arrange & Act
        var user = new User
        {
            Email = "",
            FirstName = "",
            LastName = "",
            PasswordHash = ""
        };

        // Assert
        user.Email.Should().BeEmpty();
        user.FirstName.Should().BeEmpty();
        user.LastName.Should().BeEmpty();
        user.PasswordHash.Should().BeEmpty();
        user.FullName.Should().Be(" "); // Space between empty strings
    }

    [Fact]
    public void User_ShouldHandleLongStrings()
    {
        // Arrange
        var longString = new string('A', 500);
        
        // Act
        var user = new User
        {
            Email = longString,
            FirstName = longString,
            LastName = longString,
            PasswordHash = longString
        };

        // Assert
        user.Email.Should().Be(longString);
        user.FirstName.Should().Be(longString);
        user.LastName.Should().Be(longString);
        user.PasswordHash.Should().Be(longString);
    }

    [Fact]
    public void User_ShouldHandleSpecialCharactersInNames()
    {
        // Arrange & Act
        var user = new User
        {
            FirstName = "José-María",
            LastName = "O'Connor-Smith",
            Email = "jose.maria@example.com"
        };

        // Assert
        user.FirstName.Should().Be("José-María");
        user.LastName.Should().Be("O'Connor-Smith");
        user.FullName.Should().Be("José-María O'Connor-Smith");
        user.Email.Should().Be("jose.maria@example.com");
    }

    [Theory]
    [InlineData(null)]
    public void User_ShouldHandleNullLastLoginAt(DateTime? lastLoginAt)
    {
        // Arrange & Act
        var user = new User { LastLoginAt = lastLoginAt };

        // Assert
        user.LastLoginAt.Should().Be(lastLoginAt);
    }

    #endregion

    #region Entity Inheritance Tests

    [Fact]
    public void User_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Should().BeAssignableTo<BaseEntity>();
        user.Id.Should().NotBeEmpty();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void User_ShouldHaveUniqueId()
    {
        // Arrange & Act
        var user1 = new User();
        var user2 = new User();

        // Assert
        user1.Id.Should().NotBe(user2.Id);
    }

    #endregion

    #region Role-Based Business Logic Tests

    [Fact]
    public void User_WithTraderRole_ShouldBeTrader()
    {
        // Arrange & Act
        var trader = new User
        {
            Email = "trader@company.com",
            FirstName = "Trading",
            LastName = "User",
            Role = UserRoleEnum.Trader
        };

        // Assert
        trader.Role.Should().Be(UserRoleEnum.Trader);
    }

    [Fact]
    public void User_WithRiskManagerRole_ShouldBeRiskManager()
    {
        // Arrange & Act
        var riskManager = new User
        {
            Email = "risk@company.com",
            FirstName = "Risk",
            LastName = "Manager",
            Role = UserRoleEnum.RiskManager
        };

        // Assert
        riskManager.Role.Should().Be(UserRoleEnum.RiskManager);
    }

    [Fact]
    public void User_WithAdministratorRole_ShouldBeAdministrator()
    {
        // Arrange & Act
        var admin = new User
        {
            Email = "admin@company.com",
            FirstName = "System",
            LastName = "Administrator",
            Role = UserRoleEnum.Administrator
        };

        // Assert
        admin.Role.Should().Be(UserRoleEnum.Administrator);
    }

    [Fact]
    public void User_WithViewerRole_ShouldBeViewer()
    {
        // Arrange & Act
        var viewer = new User
        {
            Email = "viewer@company.com",
            FirstName = "Report",
            LastName = "Viewer",
            Role = UserRoleEnum.Viewer
        };

        // Assert
        viewer.Role.Should().Be(UserRoleEnum.Viewer);
    }

    #endregion

    #region Helper Methods

    private static PurchaseContract CreateMockPurchaseContract()
    {
        var contractNumber = ContractNumber.Create(2024, ContractType.CARGO, 1);
        var quantity = Quantity.MetricTons(1000);
        
        return new PurchaseContract(
            contractNumber,
            ContractType.CARGO,
            Guid.NewGuid(), // supplierId
            Guid.NewGuid(), // productId
            Guid.NewGuid(), // traderId
            quantity);
    }

    private static SalesContract CreateMockSalesContract()
    {
        var contractNumber = ContractNumber.Create(2024, ContractType.CARGO, 1);
        var quantity = Quantity.MetricTons(1000);
        
        return new SalesContract(
            contractNumber,
            ContractType.CARGO,
            Guid.NewGuid(), // customerId
            Guid.NewGuid(), // productId
            Guid.NewGuid(), // traderId
            quantity);
    }

    #endregion

    #region Real-World Scenarios Tests

    [Fact]
    public void User_ShouldSupportTypicalTradingTeamStructure()
    {
        // Test that the User entity can represent a typical oil trading team
        var teamMembers = new[]
        {
            new User { Email = "head.trader@company.com", Role = UserRoleEnum.Trader, FirstName = "Senior", LastName = "Trader" },
            new User { Email = "junior.trader@company.com", Role = UserRoleEnum.Trader, FirstName = "Junior", LastName = "Trader" },
            new User { Email = "risk.manager@company.com", Role = UserRoleEnum.RiskManager, FirstName = "Risk", LastName = "Manager" },
            new User { Email = "operations@company.com", Role = UserRoleEnum.Viewer, FirstName = "Operations", LastName = "Team" },
            new User { Email = "sysadmin@company.com", Role = UserRoleEnum.Administrator, FirstName = "System", LastName = "Admin" }
        };

        foreach (var member in teamMembers)
        {
            member.Should().NotBeNull();
            member.Email.Should().NotBeEmpty();
            member.Role.Should().BeOneOf(UserRoleEnum.Trader, UserRoleEnum.RiskManager, UserRoleEnum.Viewer, UserRoleEnum.Administrator);
            member.FullName.Should().NotBeEmpty();
            member.IsActive.Should().BeTrue(); // Default value
        }
    }

    #endregion
}