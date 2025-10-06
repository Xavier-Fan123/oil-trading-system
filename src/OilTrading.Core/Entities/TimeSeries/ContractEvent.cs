namespace OilTrading.Core.Entities.TimeSeries;

public class ContractEvent : BaseEntity
{
    public DateTime Timestamp { get; set; }
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public decimal? OldValue { get; set; }
    public decimal? NewValue { get; set; }
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public string AdditionalData { get; set; } = string.Empty; // JSON data
}