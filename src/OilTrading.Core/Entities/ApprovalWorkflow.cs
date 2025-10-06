using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

public class ApprovalWorkflow : BaseEntity
{
    public string WorkflowName { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty; // TradingOrder, Contract, RiskLimit
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public new string CreatedBy { get; set; } = string.Empty;
    
    // Workflow conditions
    public WorkflowConditions Conditions { get; set; } = new();
    
    // Navigation properties
    public ICollection<ApprovalWorkflowLevel> Levels { get; set; } = new List<ApprovalWorkflowLevel>();
}

public class ApprovalWorkflowLevel : BaseEntity
{
    public int ApprovalWorkflowId { get; set; }
    public ApprovalWorkflow ApprovalWorkflow { get; set; } = null!;
    public int Level { get; set; }
    public string LevelName { get; set; } = string.Empty;
    public string RequiredRole { get; set; } = string.Empty;
    public int RequiredApprovers { get; set; } = 1;
    public bool IsParallel { get; set; } // Parallel or sequential approval
    public int TimeoutHours { get; set; } = 24;
    public bool IsOptional { get; set; }
    public string? EscalationRole { get; set; }
    public int EscalationHours { get; set; } = 48;
}

public class WorkflowConditions
{
    public Money? MinAmount { get; set; }
    public Money? MaxAmount { get; set; }
    public string[]? ProductTypes { get; set; }
    public string[]? TradingPartners { get; set; }
    public string[]? Locations { get; set; }
    public bool RequireRiskCheck { get; set; }
    public bool RequireComplianceCheck { get; set; }
}

public class CounterpartyProfile : BaseEntity
{
    public int TradingPartnerId { get; set; }
    public TradingPartner TradingPartner { get; set; } = null!;
    public CounterpartyRiskRating RiskRating { get; set; }
    public Money CreditLimit { get; set; } = new(0, "USD");
    public Money CurrentExposure { get; set; } = new(0, "USD");
    public Money AvailableCredit => new(CreditLimit.Amount - CurrentExposure.Amount, CreditLimit.Currency);
    public DateTime LastReviewDate { get; set; }
    public DateTime NextReviewDate { get; set; }
    public string? ReviewedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime? BlockedAt { get; set; }
    public string? BlockedBy { get; set; }
    
    // KYC and compliance
    public bool KycCompleted { get; set; }
    public DateTime? KycCompletedDate { get; set; }
    public DateTime? KycExpiryDate { get; set; }
    public string? ComplianceNotes { get; set; }
    public CounterpartyComplianceStatus ComplianceStatus { get; set; }
    
    // Documentation
    public ICollection<CounterpartyDocument> Documents { get; set; } = new List<CounterpartyDocument>();
    public ICollection<CounterpartyContact> Contacts { get; set; } = new List<CounterpartyContact>();
}

public class CounterpartyDocument : BaseEntity
{
    public int CounterpartyProfileId { get; set; }
    public CounterpartyProfile CounterpartyProfile { get; set; } = null!;
    public string DocumentType { get; set; } = string.Empty; // Contract, Insurance, License, etc.
    public string DocumentName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

public class CounterpartyContact : BaseEntity
{
    public int CounterpartyProfileId { get; set; }
    public CounterpartyProfile CounterpartyProfile { get; set; } = null!;
    public string ContactType { get; set; } = string.Empty; // Trading, Operations, Credit, Compliance
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum CounterpartyRiskRating
{
    AAA = 1,
    AA = 2,
    A = 3,
    BBB = 4,
    BB = 5,
    B = 6,
    CCC = 7,
    CC = 8,
    C = 9,
    D = 10 // Default
}

public enum CounterpartyComplianceStatus
{
    Compliant = 1,
    UnderReview = 2,
    NonCompliant = 3,
    Expired = 4
}