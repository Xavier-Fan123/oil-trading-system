using OilTrading.Core.Entities;
using OilTrading.Core.Enums;

namespace OilTrading.Tests.TestBuilders;

/// <summary>
/// Fluent builder for creating ContractSettlement test instances
/// </summary>
public class ContractSettlementBuilder
{
    private Guid _contractId = Guid.NewGuid();
    private string _contractNumber = "PC-2025-001";
    private string _externalContractNumber = "EXT-001";
    private string? _documentNumber = "BL-001";
    private DocumentType _documentType = DocumentType.BillOfLading;
    private DateTime _documentDate = DateTime.UtcNow;
    private string _createdBy = "TestUser";

    // Data Lineage fields
    private string? _dealReferenceId;
    private bool _initializeAsOriginal = true;
    private Guid? _previousSettlementId;
    private Guid? _originalSettlementId;
    private int _previousSequence;
    private SettlementAmendmentType _amendmentType = SettlementAmendmentType.Initial;
    private string? _amendmentReason;

    // Quantity fields
    private decimal _actualQuantityMT = 1000m;
    private decimal _actualQuantityBBL = 7600m;

    public ContractSettlementBuilder()
    {
    }

    public ContractSettlementBuilder WithContractId(Guid contractId)
    {
        _contractId = contractId;
        return this;
    }

    public ContractSettlementBuilder WithContractNumber(string contractNumber)
    {
        _contractNumber = contractNumber;
        return this;
    }

    public ContractSettlementBuilder WithExternalContractNumber(string externalContractNumber)
    {
        _externalContractNumber = externalContractNumber;
        return this;
    }

    public ContractSettlementBuilder WithDocumentNumber(string documentNumber)
    {
        _documentNumber = documentNumber;
        return this;
    }

    public ContractSettlementBuilder WithDocumentType(DocumentType documentType)
    {
        _documentType = documentType;
        return this;
    }

    public ContractSettlementBuilder WithDocumentDate(DateTime documentDate)
    {
        _documentDate = documentDate;
        return this;
    }

    public ContractSettlementBuilder WithCreatedBy(string createdBy)
    {
        _createdBy = createdBy;
        return this;
    }

    public ContractSettlementBuilder WithDealReferenceId(string dealReferenceId)
    {
        _dealReferenceId = dealReferenceId;
        return this;
    }

    public ContractSettlementBuilder AsOriginal()
    {
        _initializeAsOriginal = true;
        _previousSettlementId = null;
        _originalSettlementId = null;
        _amendmentType = SettlementAmendmentType.Initial;
        _amendmentReason = null;
        return this;
    }

    public ContractSettlementBuilder AsAmendment(
        Guid previousSettlementId,
        Guid originalSettlementId,
        int previousSequence,
        SettlementAmendmentType amendmentType,
        string amendmentReason)
    {
        _initializeAsOriginal = false;
        _previousSettlementId = previousSettlementId;
        _originalSettlementId = originalSettlementId;
        _previousSequence = previousSequence;
        _amendmentType = amendmentType;
        _amendmentReason = amendmentReason;
        return this;
    }

    public ContractSettlementBuilder AsCorrection(
        Guid previousSettlementId,
        Guid originalSettlementId,
        int previousSequence,
        string correctionReason)
    {
        return AsAmendment(
            previousSettlementId,
            originalSettlementId,
            previousSequence,
            SettlementAmendmentType.Correction,
            correctionReason);
    }

    public ContractSettlementBuilder AsSecondaryPricing(
        Guid previousSettlementId,
        Guid originalSettlementId,
        int previousSequence)
    {
        return AsAmendment(
            previousSettlementId,
            originalSettlementId,
            previousSequence,
            SettlementAmendmentType.SecondaryPricing,
            "Secondary pricing event");
    }

    public ContractSettlementBuilder AsFinalSettlement(
        Guid previousSettlementId,
        Guid originalSettlementId,
        int previousSequence)
    {
        return AsAmendment(
            previousSettlementId,
            originalSettlementId,
            previousSequence,
            SettlementAmendmentType.FinalSettlement,
            "Final settlement closing provisional");
    }

    public ContractSettlementBuilder WithActualQuantities(decimal mt, decimal bbl)
    {
        _actualQuantityMT = mt;
        _actualQuantityBBL = bbl;
        return this;
    }

    public ContractSettlement Build()
    {
        var settlement = new ContractSettlement(
            _contractId,
            _contractNumber,
            _externalContractNumber,
            _documentNumber,
            _documentType,
            _documentDate,
            _createdBy);

        // Apply Deal Reference ID if specified
        if (!string.IsNullOrWhiteSpace(_dealReferenceId))
        {
            settlement.SetDealReferenceId(_dealReferenceId, _createdBy);
        }

        // Initialize amendment chain
        if (_initializeAsOriginal)
        {
            settlement.InitializeAsOriginal();
        }
        else if (_previousSettlementId.HasValue && _originalSettlementId.HasValue)
        {
            settlement.InitializeAsAmendment(
                _previousSettlementId.Value,
                _originalSettlementId.Value,
                _previousSequence,
                _amendmentType,
                _amendmentReason ?? "Amendment",
                _createdBy);
        }

        // Set quantities
        settlement.UpdateActualQuantities(_actualQuantityMT, _actualQuantityBBL, _createdBy);

        return settlement;
    }

    /// <summary>
    /// Create a basic original settlement for testing
    /// </summary>
    public static ContractSettlement CreateBasicOriginal() =>
        new ContractSettlementBuilder()
            .WithContractNumber("PC-TEST-001")
            .WithExternalContractNumber("EXT-TEST-001")
            .WithDocumentNumber("BL-TEST-001")
            .WithDealReferenceId("DEAL-2025-0001")
            .AsOriginal()
            .Build();

    /// <summary>
    /// Create an original settlement and its first amendment
    /// </summary>
    public static (ContractSettlement Original, ContractSettlement Amendment) CreateWithAmendment()
    {
        var original = CreateBasicOriginal();

        var amendment = new ContractSettlementBuilder()
            .WithContractId(original.ContractId)
            .WithContractNumber(original.ContractNumber)
            .WithExternalContractNumber(original.ExternalContractNumber)
            .WithDocumentNumber("BL-TEST-001-A1")
            .WithDealReferenceId("DEAL-2025-0001")
            .AsAmendment(
                original.Id,
                original.Id,
                1,
                SettlementAmendmentType.Amendment,
                "Quantity correction after B/L verification")
            .Build();

        // Mark original as superseded
        original.MarkAsSuperseded("System");

        return (original, amendment);
    }

    /// <summary>
    /// Create a chain of settlements: Original -> Amendment -> Correction
    /// </summary>
    public static List<ContractSettlement> CreateAmendmentChain()
    {
        var contractId = Guid.NewGuid();
        var dealReferenceId = "DEAL-2025-CHAIN";

        var original = new ContractSettlementBuilder()
            .WithContractId(contractId)
            .WithContractNumber("PC-CHAIN-001")
            .WithExternalContractNumber("EXT-CHAIN-001")
            .WithDocumentNumber("BL-CHAIN-001")
            .WithDealReferenceId(dealReferenceId)
            .AsOriginal()
            .Build();

        var amendment = new ContractSettlementBuilder()
            .WithContractId(contractId)
            .WithContractNumber("PC-CHAIN-001")
            .WithExternalContractNumber("EXT-CHAIN-001")
            .WithDocumentNumber("BL-CHAIN-001-A1")
            .WithDealReferenceId(dealReferenceId)
            .AsAmendment(
                original.Id,
                original.Id,
                1,
                SettlementAmendmentType.Amendment,
                "Price adjustment")
            .Build();

        var correction = new ContractSettlementBuilder()
            .WithContractId(contractId)
            .WithContractNumber("PC-CHAIN-001")
            .WithExternalContractNumber("EXT-CHAIN-001")
            .WithDocumentNumber("BL-CHAIN-001-C1")
            .WithDealReferenceId(dealReferenceId)
            .AsCorrection(
                amendment.Id,
                original.Id,
                2,
                "Calculation error fix")
            .Build();

        // Mark superseded settlements
        original.MarkAsSuperseded("System");
        amendment.MarkAsSuperseded("System");

        return new List<ContractSettlement> { original, amendment, correction };
    }
}
