using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

/// <summary>
/// Service interface for managing shipping operation splits
/// Handles scenarios where cargo is split across multiple shipments/deliveries
/// </summary>
public interface IShippingOperationSplitService
{
    /// <summary>
    /// Split an existing shipping operation into multiple child operations
    /// </summary>
    /// <param name="parentShippingOperationId">The shipping operation to split</param>
    /// <param name="splitQuantities">List of quantities for each split</param>
    /// <param name="splitReason">Reason for the split</param>
    /// <param name="splitReasonNotes">Additional notes about the split</param>
    /// <param name="createdBy">User performing the split</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of created child shipping operations</returns>
    Task<IReadOnlyList<ShippingOperation>> SplitShippingOperationAsync(
        Guid parentShippingOperationId,
        IReadOnlyList<Quantity> splitQuantities,
        SplitReason splitReason,
        string? splitReasonNotes = null,
        string createdBy = "System",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all child shipping operations for a parent
    /// </summary>
    Task<IReadOnlyList<ShippingOperation>> GetChildShippingOperationsAsync(
        Guid parentShippingOperationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the parent shipping operation for a split child
    /// </summary>
    Task<ShippingOperation?> GetParentShippingOperationAsync(
        Guid childShippingOperationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that a shipping operation can be split
    /// </summary>
    Task<(bool CanSplit, string? ValidationMessage)> CanSplitAsync(
        Guid shippingOperationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the complete split tree for a shipping operation (all ancestors and descendants)
    /// </summary>
    Task<ShippingOperationSplitTree> GetSplitTreeAsync(
        Guid shippingOperationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that split quantities sum correctly to parent quantity
    /// </summary>
    Task<(bool IsValid, string? ValidationMessage)> ValidateSplitQuantitiesAsync(
        Guid parentShippingOperationId,
        IReadOnlyList<Quantity> splitQuantities,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the complete split tree for a shipping operation
/// </summary>
public class ShippingOperationSplitTree
{
    /// <summary>
    /// The root shipping operation (original, unsplit parent)
    /// </summary>
    public required ShippingOperation RootOperation { get; init; }

    /// <summary>
    /// All operations in the tree, organized hierarchically
    /// </summary>
    public required IReadOnlyList<ShippingOperationSplitNode> Nodes { get; init; }

    /// <summary>
    /// Total quantity across all leaf nodes
    /// </summary>
    public decimal TotalLeafQuantity { get; init; }

    /// <summary>
    /// Number of split generations
    /// </summary>
    public int MaxDepth { get; init; }
}

/// <summary>
/// A node in the shipping operation split tree
/// </summary>
public class ShippingOperationSplitNode
{
    /// <summary>
    /// The shipping operation at this node
    /// </summary>
    public required ShippingOperation Operation { get; init; }

    /// <summary>
    /// Parent node (null for root)
    /// </summary>
    public ShippingOperationSplitNode? Parent { get; init; }

    /// <summary>
    /// Child nodes (empty for leaves)
    /// </summary>
    public IReadOnlyList<ShippingOperationSplitNode> Children { get; init; } = Array.Empty<ShippingOperationSplitNode>();

    /// <summary>
    /// Depth in the tree (0 for root)
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    /// Whether this is a leaf node (no children)
    /// </summary>
    public bool IsLeaf => Children.Count == 0;
}
