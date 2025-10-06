using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities
{
    public class ContractMatching : BaseEntity
    {
        private ContractMatching() { } // For EF Core

        public ContractMatching(
            Guid purchaseContractId,
            Guid salesContractId,
            decimal matchedQuantity,
            string matchedBy,
            string? notes = null)
        {
            if (purchaseContractId == Guid.Empty)
                throw new DomainException("Purchase contract ID cannot be empty");
            if (salesContractId == Guid.Empty)
                throw new DomainException("Sales contract ID cannot be empty");
            if (matchedQuantity <= 0)
                throw new DomainException("Matched quantity must be greater than zero");
            if (string.IsNullOrWhiteSpace(matchedBy))
                throw new DomainException("MatchedBy is required");

            PurchaseContractId = purchaseContractId;
            SalesContractId = salesContractId;
            MatchedQuantity = matchedQuantity;
            MatchedDate = DateTime.UtcNow;
            MatchedBy = matchedBy.Trim();
            Notes = notes?.Trim();
        }

        public Guid PurchaseContractId { get; private set; }
        public Guid SalesContractId { get; private set; }
        public decimal MatchedQuantity { get; private set; }
        public DateTime MatchedDate { get; private set; }
        public string MatchedBy { get; private set; } = string.Empty;
        public string? Notes { get; private set; }

        // Navigation properties
        public virtual PurchaseContract PurchaseContract { get; private set; } = null!;
        public virtual SalesContract SalesContract { get; private set; } = null!;

        // Business methods
        public void UpdateQuantity(decimal newQuantity, string updatedBy)
        {
            if (newQuantity <= 0)
                throw new DomainException("Matched quantity must be greater than zero");
            if (string.IsNullOrWhiteSpace(updatedBy))
                throw new DomainException("UpdatedBy is required");

            MatchedQuantity = newQuantity;
            SetUpdatedBy(updatedBy);
        }

        public void UpdateNotes(string notes, string updatedBy)
        {
            if (string.IsNullOrWhiteSpace(updatedBy))
                throw new DomainException("UpdatedBy is required");

            Notes = notes?.Trim();
            SetUpdatedBy(updatedBy);
        }
    }
}