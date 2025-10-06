namespace OilTrading.Core.Events;

public class PurchaseContractSubmittedForApprovalEvent : DomainEvent
{
    public Guid ContractId { get; }
    public string ContractNumber { get; }

    public PurchaseContractSubmittedForApprovalEvent(Guid contractId, string contractNumber)
    {
        ContractId = contractId;
        ContractNumber = contractNumber;
    }
}