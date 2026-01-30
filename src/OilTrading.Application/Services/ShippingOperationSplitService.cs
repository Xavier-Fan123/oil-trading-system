using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

/// <summary>
/// Service for managing shipping operation splits
/// Enables tracking when cargo is split across multiple shipments/deliveries
/// </summary>
public class ShippingOperationSplitService : IShippingOperationSplitService
{
    private readonly IRepository<ShippingOperation> _shippingOperationRepository;
    private readonly ILogger<ShippingOperationSplitService> _logger;

    public ShippingOperationSplitService(
        IRepository<ShippingOperation> shippingOperationRepository,
        ILogger<ShippingOperationSplitService> logger)
    {
        _shippingOperationRepository = shippingOperationRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingOperation>> SplitShippingOperationAsync(
        Guid parentShippingOperationId,
        IReadOnlyList<Quantity> splitQuantities,
        SplitReason splitReason,
        string? splitReasonNotes = null,
        string createdBy = "System",
        CancellationToken cancellationToken = default)
    {
        var parent = await _shippingOperationRepository.GetByIdAsync(parentShippingOperationId, cancellationToken);
        if (parent == null)
        {
            throw new InvalidOperationException($"Shipping operation {parentShippingOperationId} not found");
        }

        // Validate split
        var (canSplit, validationMessage) = await CanSplitAsync(parentShippingOperationId, cancellationToken);
        if (!canSplit)
        {
            throw new InvalidOperationException(validationMessage ?? "Cannot split this shipping operation");
        }

        // Validate quantities
        var (isValid, quantityMessage) = await ValidateSplitQuantitiesAsync(
            parentShippingOperationId, splitQuantities, cancellationToken);
        if (!isValid)
        {
            throw new InvalidOperationException(quantityMessage ?? "Invalid split quantities");
        }

        // Mark parent as split
        parent.MarkAsSplitParent(createdBy);
        await _shippingOperationRepository.UpdateAsync(parent, cancellationToken);

        // Create child operations
        var children = new List<ShippingOperation>();
        var sequence = 1;

        foreach (var quantity in splitQuantities)
        {
            var child = CreateChildShippingOperation(parent, quantity, sequence, splitReason, splitReasonNotes, createdBy);
            await _shippingOperationRepository.AddAsync(child, cancellationToken);
            children.Add(child);

            _logger.LogInformation(
                "Created split child {ChildId} (sequence {Sequence}) from parent {ParentId} with quantity {Quantity}",
                child.Id, sequence, parentShippingOperationId, quantity.Value);

            sequence++;
        }

        _logger.LogInformation(
            "Split shipping operation {ParentId} into {Count} children. Reason: {Reason}",
            parentShippingOperationId, children.Count, splitReason);

        return children;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingOperation>> GetChildShippingOperationsAsync(
        Guid parentShippingOperationId,
        CancellationToken cancellationToken = default)
    {
        var allOperations = await _shippingOperationRepository.GetAllAsync(cancellationToken);
        return allOperations
            .Where(op => op.ParentShippingOperationId == parentShippingOperationId)
            .OrderBy(op => op.SplitSequence)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ShippingOperation?> GetParentShippingOperationAsync(
        Guid childShippingOperationId,
        CancellationToken cancellationToken = default)
    {
        var child = await _shippingOperationRepository.GetByIdAsync(childShippingOperationId, cancellationToken);
        if (child?.ParentShippingOperationId == null)
        {
            return null;
        }

        return await _shippingOperationRepository.GetByIdAsync(
            child.ParentShippingOperationId.Value, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(bool CanSplit, string? ValidationMessage)> CanSplitAsync(
        Guid shippingOperationId,
        CancellationToken cancellationToken = default)
    {
        var operation = await _shippingOperationRepository.GetByIdAsync(shippingOperationId, cancellationToken);
        if (operation == null)
        {
            return (false, "Shipping operation not found");
        }

        if (!operation.CanBeSplit())
        {
            if (operation.HasBeenSplit())
            {
                return (false, "Shipping operation has already been split");
            }

            if (operation.Status == ShippingStatus.Discharged)
            {
                return (false, "Cannot split a discharged shipping operation");
            }

            if (operation.Status == ShippingStatus.Cancelled)
            {
                return (false, "Cannot split a cancelled shipping operation");
            }

            return (false, "Shipping operation cannot be split in its current state");
        }

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<ShippingOperationSplitTree> GetSplitTreeAsync(
        Guid shippingOperationId,
        CancellationToken cancellationToken = default)
    {
        var operation = await _shippingOperationRepository.GetByIdAsync(shippingOperationId, cancellationToken);
        if (operation == null)
        {
            throw new InvalidOperationException($"Shipping operation {shippingOperationId} not found");
        }

        // Find the root operation
        var root = await FindRootOperationAsync(operation, cancellationToken);

        // Build the tree
        var allOperations = await _shippingOperationRepository.GetAllAsync(cancellationToken);
        var operationsByParent = allOperations
            .Where(op => op.ParentShippingOperationId != null)
            .GroupBy(op => op.ParentShippingOperationId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(op => op.SplitSequence).ToList());

        var rootNode = BuildTreeNode(root, operationsByParent, null, 0);
        var allNodes = FlattenTree(rootNode);

        return new ShippingOperationSplitTree
        {
            RootOperation = root,
            Nodes = allNodes,
            TotalLeafQuantity = allNodes.Where(n => n.IsLeaf).Sum(n => n.Operation.PlannedQuantity?.Value ?? 0),
            MaxDepth = allNodes.Max(n => n.Depth)
        };
    }

    /// <inheritdoc />
    public async Task<(bool IsValid, string? ValidationMessage)> ValidateSplitQuantitiesAsync(
        Guid parentShippingOperationId,
        IReadOnlyList<Quantity> splitQuantities,
        CancellationToken cancellationToken = default)
    {
        var parent = await _shippingOperationRepository.GetByIdAsync(parentShippingOperationId, cancellationToken);
        if (parent == null)
        {
            return (false, "Parent shipping operation not found");
        }

        if (splitQuantities.Count < 2)
        {
            return (false, "Split must result in at least 2 child operations");
        }

        var parentQuantity = parent.PlannedQuantity?.Value ?? 0;
        var splitTotal = splitQuantities.Sum(q => q.Value);

        // Allow small tolerance for rounding (0.01%)
        var tolerance = parentQuantity * 0.0001m;
        if (Math.Abs(splitTotal - parentQuantity) > tolerance)
        {
            return (false, $"Split quantities ({splitTotal:N4}) do not sum to parent quantity ({parentQuantity:N4})");
        }

        // Validate all quantities are positive
        if (splitQuantities.Any(q => q.Value <= 0))
        {
            return (false, "All split quantities must be positive");
        }

        return (true, null);
    }

    private ShippingOperation CreateChildShippingOperation(
        ShippingOperation parent,
        Quantity quantity,
        int sequence,
        SplitReason splitReason,
        string? splitReasonNotes,
        string createdBy)
    {
        // Generate a unique shipping number for the split
        var splitShippingNumber = $"{parent.ShippingNumber}-S{sequence:D2}";

        // Create a new shipping operation using the constructor with required parameters
        var child = new ShippingOperation(
            shippingNumber: splitShippingNumber,
            contractId: parent.ContractId,
            vesselName: parent.VesselName,
            plannedQuantity: quantity,
            loadPortETA: parent.LoadPortETA,
            dischargePortETA: parent.DischargePortETA,
            loadPort: parent.LoadPort,
            dischargePort: parent.DischargePort);

        // Initialize as split with lineage tracking
        child.InitializeAsSplit(
            parent.Id,
            parent.DealReferenceId ?? "",
            parent.PlannedQuantity,
            sequence,
            splitReason,
            splitReasonNotes,
            createdBy);

        return child;
    }

    private async Task<ShippingOperation> FindRootOperationAsync(
        ShippingOperation operation,
        CancellationToken cancellationToken)
    {
        var current = operation;
        while (current.ParentShippingOperationId != null)
        {
            var parent = await _shippingOperationRepository.GetByIdAsync(
                current.ParentShippingOperationId.Value, cancellationToken);
            if (parent == null)
            {
                break;
            }
            current = parent;
        }
        return current;
    }

    private ShippingOperationSplitNode BuildTreeNode(
        ShippingOperation operation,
        Dictionary<Guid, List<ShippingOperation>> operationsByParent,
        ShippingOperationSplitNode? parentNode,
        int depth)
    {
        var node = new ShippingOperationSplitNode
        {
            Operation = operation,
            Parent = parentNode,
            Depth = depth
        };

        if (operationsByParent.TryGetValue(operation.Id, out var children))
        {
            var childNodes = children
                .Select(child => BuildTreeNode(child, operationsByParent, node, depth + 1))
                .ToList();

            // Use reflection to set Children since it's init-only
            var childrenProperty = typeof(ShippingOperationSplitNode).GetProperty(nameof(ShippingOperationSplitNode.Children));
            childrenProperty?.SetValue(node, childNodes);
        }

        return node;
    }

    private List<ShippingOperationSplitNode> FlattenTree(ShippingOperationSplitNode root)
    {
        var result = new List<ShippingOperationSplitNode> { root };
        foreach (var child in root.Children)
        {
            result.AddRange(FlattenTree(child));
        }
        return result;
    }
}
