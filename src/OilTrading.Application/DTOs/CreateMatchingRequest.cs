namespace OilTrading.Application.DTOs
{
    public class CreateMatchingRequest
    {
        public Guid PurchaseContractId { get; set; }
        public Guid SalesContractId { get; set; }
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
        public string? MatchedBy { get; set; }
    }
}