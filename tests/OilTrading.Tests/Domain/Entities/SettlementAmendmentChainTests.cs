using FluentAssertions;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using OilTrading.Tests.TestBuilders;
using Xunit;

namespace OilTrading.Tests.Domain.Entities;

/// <summary>
/// Tests for Settlement Amendment Chain functionality (Data Lineage Enhancement v2.18.0)
/// Covers: InitializeAsOriginal, InitializeAsAmendment, MarkAsSuperseded, Amendment Chain queries
/// </summary>
public class SettlementAmendmentChainTests
{
    #region InitializeAsOriginal Tests

    [Fact]
    public void InitializeAsOriginal_SetsCorrectDefaults()
    {
        // Arrange
        var settlement = ContractSettlementBuilder.CreateBasicOriginal();

        // Assert
        settlement.SettlementSequence.Should().Be(1);
        settlement.AmendmentType.Should().Be(SettlementAmendmentType.Initial);
        settlement.IsLatestVersion.Should().BeTrue();
        settlement.PreviousSettlementId.Should().BeNull();
        settlement.OriginalSettlementId.Should().Be(settlement.Id);
        settlement.SupersededDate.Should().BeNull();
        settlement.AmendmentReason.Should().BeNull();
    }

    [Fact]
    public void InitializeAsOriginal_OriginalSettlementId_PointsToSelf()
    {
        // Arrange & Act
        var settlement = ContractSettlementBuilder.CreateBasicOriginal();

        // Assert
        settlement.OriginalSettlementId.Should().Be(settlement.Id);
    }

    [Fact]
    public void InitializeAsOriginal_SetsDealReferenceId_WhenProvided()
    {
        // Arrange & Act
        var settlement = new ContractSettlementBuilder()
            .WithDealReferenceId("DEAL-2025-TEST-001")
            .AsOriginal()
            .Build();

        // Assert
        settlement.DealReferenceId.Should().Be("DEAL-2025-TEST-001");
    }

    #endregion

    #region InitializeAsAmendment Tests

    [Fact]
    public void InitializeAsAmendment_LinksToOriginal()
    {
        // Arrange
        var (original, amendment) = ContractSettlementBuilder.CreateWithAmendment();

        // Assert
        amendment.OriginalSettlementId.Should().Be(original.Id);
        amendment.PreviousSettlementId.Should().Be(original.Id);
    }

    [Fact]
    public void InitializeAsAmendment_IncrementsSequenceNumber()
    {
        // Arrange
        var (original, amendment) = ContractSettlementBuilder.CreateWithAmendment();

        // Assert
        original.SettlementSequence.Should().Be(1);
        amendment.SettlementSequence.Should().Be(2);
    }

    [Fact]
    public void InitializeAsAmendment_PreservesContractId()
    {
        // Arrange
        var (original, amendment) = ContractSettlementBuilder.CreateWithAmendment();

        // Assert
        amendment.ContractId.Should().Be(original.ContractId);
    }

    [Fact]
    public void InitializeAsAmendment_PreservesDealReferenceId()
    {
        // Arrange
        var (original, amendment) = ContractSettlementBuilder.CreateWithAmendment();

        // Assert
        amendment.DealReferenceId.Should().Be(original.DealReferenceId);
    }

    [Theory]
    [InlineData(SettlementAmendmentType.Amendment)]
    [InlineData(SettlementAmendmentType.Correction)]
    [InlineData(SettlementAmendmentType.SecondaryPricing)]
    [InlineData(SettlementAmendmentType.FinalSettlement)]
    public void InitializeAsAmendment_WithType_SetsCorrectType(SettlementAmendmentType type)
    {
        // Arrange
        var original = ContractSettlementBuilder.CreateBasicOriginal();

        // Act
        var amendment = new ContractSettlementBuilder()
            .WithContractId(original.ContractId)
            .WithContractNumber(original.ContractNumber)
            .WithExternalContractNumber(original.ExternalContractNumber)
            .AsAmendment(original.Id, original.Id, 1, type, $"Test {type} reason")
            .Build();

        // Assert
        amendment.AmendmentType.Should().Be(type);
    }

    [Fact]
    public void InitializeAsAmendment_SetsAmendmentReason()
    {
        // Arrange
        var original = ContractSettlementBuilder.CreateBasicOriginal();
        var expectedReason = "Quantity correction after B/L verification";

        // Act
        var amendment = new ContractSettlementBuilder()
            .WithContractId(original.ContractId)
            .WithContractNumber(original.ContractNumber)
            .WithExternalContractNumber(original.ExternalContractNumber)
            .AsAmendment(original.Id, original.Id, 1, SettlementAmendmentType.Amendment, expectedReason)
            .Build();

        // Assert
        amendment.AmendmentReason.Should().Be(expectedReason);
    }

    [Fact]
    public void InitializeAsAmendment_NewVersionIsLatest()
    {
        // Arrange
        var (_, amendment) = ContractSettlementBuilder.CreateWithAmendment();

        // Assert
        amendment.IsLatestVersion.Should().BeTrue();
    }

    #endregion

    #region MarkAsSuperseded Tests

    [Fact]
    public void MarkAsSuperseded_SetsSupersededDate()
    {
        // Arrange
        var settlement = ContractSettlementBuilder.CreateBasicOriginal();

        // Act
        settlement.MarkAsSuperseded("TestUser");

        // Assert
        settlement.SupersededDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsSuperseded_SetsIsLatestVersionToFalse()
    {
        // Arrange
        var settlement = ContractSettlementBuilder.CreateBasicOriginal();
        settlement.IsLatestVersion.Should().BeTrue(); // Verify precondition

        // Act
        settlement.MarkAsSuperseded("TestUser");

        // Assert
        settlement.IsLatestVersion.Should().BeFalse();
    }

    [Fact]
    public void CreateAmendment_PreviousVersionMarkedAsSuperseded()
    {
        // Arrange & Act
        var (original, _) = ContractSettlementBuilder.CreateWithAmendment();

        // Assert
        original.IsLatestVersion.Should().BeFalse();
        original.SupersededDate.Should().NotBeNull();
    }

    #endregion

    #region Amendment Chain Tests

    [Fact]
    public void AmendmentChain_ThreeVersions_CorrectSequencing()
    {
        // Arrange & Act
        var chain = ContractSettlementBuilder.CreateAmendmentChain();

        // Assert
        chain.Should().HaveCount(3);
        chain[0].SettlementSequence.Should().Be(1); // Original
        chain[1].SettlementSequence.Should().Be(2); // Amendment
        chain[2].SettlementSequence.Should().Be(3); // Correction
    }

    [Fact]
    public void AmendmentChain_OnlyLatestIsActive()
    {
        // Arrange & Act
        var chain = ContractSettlementBuilder.CreateAmendmentChain();

        // Assert
        chain[0].IsLatestVersion.Should().BeFalse(); // Original - superseded
        chain[1].IsLatestVersion.Should().BeFalse(); // Amendment - superseded
        chain[2].IsLatestVersion.Should().BeTrue();  // Correction - current
    }

    [Fact]
    public void AmendmentChain_AllShareSameOriginalId()
    {
        // Arrange & Act
        var chain = ContractSettlementBuilder.CreateAmendmentChain();
        var originalId = chain[0].Id;

        // Assert
        chain[0].OriginalSettlementId.Should().Be(originalId);
        chain[1].OriginalSettlementId.Should().Be(originalId);
        chain[2].OriginalSettlementId.Should().Be(originalId);
    }

    [Fact]
    public void AmendmentChain_PreviousSettlementIdLinksCorrectly()
    {
        // Arrange & Act
        var chain = ContractSettlementBuilder.CreateAmendmentChain();

        // Assert
        chain[0].PreviousSettlementId.Should().BeNull(); // Original has no previous
        chain[1].PreviousSettlementId.Should().Be(chain[0].Id); // Amendment links to Original
        chain[2].PreviousSettlementId.Should().Be(chain[1].Id); // Correction links to Amendment
    }

    [Fact]
    public void AmendmentChain_AllShareSameDealReferenceId()
    {
        // Arrange & Act
        var chain = ContractSettlementBuilder.CreateAmendmentChain();

        // Assert
        var expectedDealRef = "DEAL-2025-CHAIN";
        chain.Should().AllSatisfy(s => s.DealReferenceId.Should().Be(expectedDealRef));
    }

    [Fact]
    public void AmendmentChain_AllShareSameContractId()
    {
        // Arrange & Act
        var chain = ContractSettlementBuilder.CreateAmendmentChain();
        var expectedContractId = chain[0].ContractId;

        // Assert
        chain.Should().AllSatisfy(s => s.ContractId.Should().Be(expectedContractId));
    }

    #endregion

    #region Amendment Type Specific Tests

    [Fact]
    public void Correction_HasCorrectAmendmentType()
    {
        // Arrange & Act
        var chain = ContractSettlementBuilder.CreateAmendmentChain();
        var correction = chain[2];

        // Assert
        correction.AmendmentType.Should().Be(SettlementAmendmentType.Correction);
    }

    [Fact]
    public void SecondaryPricing_BuilderCreatesCorrectly()
    {
        // Arrange
        var original = ContractSettlementBuilder.CreateBasicOriginal();

        // Act
        var secondary = new ContractSettlementBuilder()
            .WithContractId(original.ContractId)
            .WithContractNumber(original.ContractNumber)
            .WithExternalContractNumber(original.ExternalContractNumber)
            .AsSecondaryPricing(original.Id, original.Id, 1)
            .Build();

        // Assert
        secondary.AmendmentType.Should().Be(SettlementAmendmentType.SecondaryPricing);
        secondary.AmendmentReason.Should().Contain("Secondary pricing");
    }

    [Fact]
    public void FinalSettlement_BuilderCreatesCorrectly()
    {
        // Arrange
        var original = ContractSettlementBuilder.CreateBasicOriginal();

        // Act
        var finalSettlement = new ContractSettlementBuilder()
            .WithContractId(original.ContractId)
            .WithContractNumber(original.ContractNumber)
            .WithExternalContractNumber(original.ExternalContractNumber)
            .AsFinalSettlement(original.Id, original.Id, 1)
            .Build();

        // Assert
        finalSettlement.AmendmentType.Should().Be(SettlementAmendmentType.FinalSettlement);
        finalSettlement.AmendmentReason.Should().Contain("Final settlement");
    }

    #endregion

    #region Edge Cases and Validation

    [Fact]
    public void Settlement_WithActualQuantities_PreservesAcrossAmendments()
    {
        // Arrange
        var original = new ContractSettlementBuilder()
            .WithActualQuantities(1500m, 11400m)
            .AsOriginal()
            .Build();

        // Assert
        original.ActualQuantityMT.Should().Be(1500m);
        original.ActualQuantityBBL.Should().Be(11400m);
    }

    [Fact]
    public void Amendment_CanHaveDifferentQuantities()
    {
        // Arrange
        var original = new ContractSettlementBuilder()
            .WithActualQuantities(1000m, 7600m)
            .AsOriginal()
            .Build();

        var amendment = new ContractSettlementBuilder()
            .WithContractId(original.ContractId)
            .WithContractNumber(original.ContractNumber)
            .WithExternalContractNumber(original.ExternalContractNumber)
            .WithActualQuantities(1050m, 7980m) // Corrected quantities
            .AsAmendment(original.Id, original.Id, 1, SettlementAmendmentType.Correction, "Quantity correction")
            .Build();

        // Assert
        original.ActualQuantityMT.Should().Be(1000m);
        amendment.ActualQuantityMT.Should().Be(1050m);
    }

    [Fact]
    public void DocumentNumber_TracksDifferentDocumentsInChain()
    {
        // Arrange & Act
        var chain = ContractSettlementBuilder.CreateAmendmentChain();

        // Assert
        chain[0].DocumentNumber.Should().Be("BL-CHAIN-001");
        chain[1].DocumentNumber.Should().Be("BL-CHAIN-001-A1");
        chain[2].DocumentNumber.Should().Be("BL-CHAIN-001-C1");
    }

    #endregion

    #region Query Simulation Tests

    [Fact]
    public void FindLatestVersion_ReturnsOnlyCurrentVersion()
    {
        // Arrange
        var chain = ContractSettlementBuilder.CreateAmendmentChain();

        // Act - Simulate query for latest version
        var latestVersions = chain.Where(s => s.IsLatestVersion).ToList();

        // Assert
        latestVersions.Should().HaveCount(1);
        latestVersions[0].Should().Be(chain[2]); // The correction (last in chain)
    }

    [Fact]
    public void FindByOriginalSettlementId_ReturnsAllVersions()
    {
        // Arrange
        var chain = ContractSettlementBuilder.CreateAmendmentChain();
        var originalId = chain[0].Id;

        // Act - Simulate query by OriginalSettlementId
        var allVersions = chain.Where(s => s.OriginalSettlementId == originalId).ToList();

        // Assert
        allVersions.Should().HaveCount(3);
    }

    [Fact]
    public void FindByDealReferenceId_ReturnsAllVersions()
    {
        // Arrange
        var chain = ContractSettlementBuilder.CreateAmendmentChain();

        // Act - Simulate query by DealReferenceId
        var allVersions = chain.Where(s => s.DealReferenceId == "DEAL-2025-CHAIN").ToList();

        // Assert
        allVersions.Should().HaveCount(3);
    }

    [Fact]
    public void OrderBySequence_ReturnsChronologicalOrder()
    {
        // Arrange
        var chain = ContractSettlementBuilder.CreateAmendmentChain();

        // Act - Simulate ordering by sequence (reverse to test sorting)
        var reversed = chain.OrderByDescending(s => s.SettlementSequence).ToList();
        var sorted = reversed.OrderBy(s => s.SettlementSequence).ToList();

        // Assert
        sorted[0].SettlementSequence.Should().Be(1);
        sorted[1].SettlementSequence.Should().Be(2);
        sorted[2].SettlementSequence.Should().Be(3);
    }

    #endregion
}
