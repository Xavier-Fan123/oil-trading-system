using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

public abstract class BaseEntity : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public string? UpdatedBy { get; protected set; }
    public bool IsDeleted { get; protected set; } = false;
    public DateTime? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }

    // Optimistic concurrency control - prevents data corruption from concurrent updates
    // Database will automatically update this value on each save
    // NOTE: DO NOT initialize with inline default (= new byte[] { 0 })
    // This would cause EF Core to optimize away SetRowVersion() calls, breaking change tracking
    // See SetRowVersion() method for explanation
    public byte[] RowVersion { get; protected set; } = null!;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void SetUpdatedBy(string updatedBy)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetCreatedBy(string createdBy)
    {
        CreatedBy = createdBy;
    }

    // Method to set Id explicitly (for cases where we need to set a specific Id)
    public void SetId(Guid id)
    {
        Id = id;
    }

    // Method to set creation audit fields together
    public void SetCreated(string createdBy)
    {
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    // Method to set creation audit fields with specific timestamp
    public void SetCreated(string createdBy, DateTime createdAt)
    {
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    // Soft delete methods
    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }

    /// <summary>
    /// CRITICAL: Explicitly set IsDeleted flag.
    /// Used during entity creation to force EF Core change tracking.
    ///
    /// SYSTEMIC ARCHITECTURE NOTE:
    /// EF Core's change detection doesn't track properties set to their inline-initialized default.
    /// Since BaseEntity declares 'IsDeleted = false', calling SetIsDeleted(false) alone doesn't
    /// register as a change. This is a limitation of EF Core's change tracker.
    ///
    /// Workaround: Call SetIsDeleted(true) first to register the property as modified,
    /// then SetIsDeleted(false) to set the final value. This ensures EF Core includes
    /// the property in the INSERT statement, preventing NULL constraint violations.
    /// </summary>
    public void SetIsDeleted(bool isDeleted)
    {
        IsDeleted = isDeleted;
        if (isDeleted)
        {
            DeletedAt = DateTime.UtcNow;
            DeletedBy ??= "System";
        }
        else
        {
            DeletedAt = null;
            DeletedBy = null;
        }
    }

    /// <summary>
    /// LEGACY: Ensures IsDeleted is explicitly set to false.
    /// Use SetIsDeleted(false) instead for consistency.
    /// </summary>
    public void EnsureNotDeleted()
    {
        SetIsDeleted(false);
    }

    /// <summary>
    /// CRITICAL: Explicitly initialize RowVersion for concurrency control.
    /// Used during entity creation to ensure EF Core includes RowVersion in INSERT statements.
    ///
    /// SYSTEMIC ARCHITECTURE NOTE:
    /// EF Core's change detection doesn't track properties set to their inline-initialized default.
    /// Since BaseEntity declares 'RowVersion = new byte[] { 0 }', merely initializing the property
    /// doesn't register as a change. This is a limitation of EF Core's change tracker.
    ///
    /// Solution: Explicitly set RowVersion during entity creation to force change tracking.
    /// This ensures EF Core includes the property in the INSERT statement, preventing NULL
    /// constraint violations when using IsRowVersion() with SQLite.
    ///
    /// SQLite's timestamp column type requires an initial value in the INSERT; it cannot be NULL.
    /// </summary>
    public void SetRowVersion(byte[] version)
    {
        RowVersion = version ?? new byte[] { 0 };
    }
}