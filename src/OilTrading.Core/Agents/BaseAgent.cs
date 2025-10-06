using Microsoft.Extensions.Logging;
using OilTrading.Core.Common;

namespace OilTrading.Core.Agents;

/// <summary>
/// 代理基础抽象类
/// </summary>
public abstract class BaseAgent : IAgent
{
    protected readonly ILogger Logger;
    protected readonly Dictionary<string, object> _state = new();
    protected readonly SemaphoreSlim _processingLock = new(1, 1);
    
    private AgentStatus _status = AgentStatus.Inactive;
    private int _processedMessages = 0;
    private int _errorCount = 0;
    private DateTime _lastActivityTime = DateTime.UtcNow;

    protected BaseAgent(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        AgentId = $"{GetType().Name}_{Guid.NewGuid():N}";
    }

    public string AgentId { get; }
    
    public abstract string Name { get; }
    
    public abstract AgentType Type { get; }
    
    public AgentStatus Status 
    { 
        get => _status;
        protected set
        {
            if (_status != value)
            {
                Logger.LogInformation("Agent {AgentId} status changed from {OldStatus} to {NewStatus}",
                    AgentId, _status, value);
                _status = value;
                _lastActivityTime = DateTime.UtcNow;
            }
        }
    }
    
    public abstract int Priority { get; }

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Status = AgentStatus.Initializing;
        
        try
        {
            Logger.LogInformation("Initializing agent {AgentId} ({Name})", AgentId, Name);
            
            await OnInitializeAsync(cancellationToken);
            
            Status = AgentStatus.Active;
            Logger.LogInformation("Agent {AgentId} initialized successfully", AgentId);
        }
        catch (Exception ex)
        {
            Status = AgentStatus.Error;
            Logger.LogError(ex, "Failed to initialize agent {AgentId}", AgentId);
            throw;
        }
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Status == AgentStatus.Inactive)
        {
            await InitializeAsync(cancellationToken);
        }
        
        if (Status != AgentStatus.Active)
        {
            throw new InvalidOperationException($"Cannot start agent {AgentId} in status {Status}");
        }
        
        Logger.LogInformation("Starting agent {AgentId}", AgentId);
        await OnStartAsync(cancellationToken);
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Stopping agent {AgentId}", AgentId);
        
        await _processingLock.WaitAsync(cancellationToken);
        try
        {
            await OnStopAsync(cancellationToken);
            Status = AgentStatus.Inactive;
            Logger.LogInformation("Agent {AgentId} stopped", AgentId);
        }
        finally
        {
            _processingLock.Release();
        }
    }

    public virtual async Task<AgentResponse> ProcessMessageAsync(AgentMessage message, CancellationToken cancellationToken = default)
    {
        if (Status != AgentStatus.Active)
        {
            return new AgentResponse
            {
                OriginalMessageId = message.MessageId,
                AgentId = AgentId,
                Status = ResponseStatus.Rejected,
                ErrorMessage = $"Agent is not active (Status: {Status})"
            };
        }

        await _processingLock.WaitAsync(cancellationToken);
        var startTime = DateTime.UtcNow;
        Status = AgentStatus.Processing;
        
        try
        {
            Logger.LogInformation("Agent {AgentId} processing message {MessageId} of type {MessageType}",
                AgentId, message.MessageId, message.Type);

            var response = await OnProcessMessageAsync(message, cancellationToken);
            response.OriginalMessageId = message.MessageId;
            response.AgentId = AgentId;
            response.ProcessingTime = DateTime.UtcNow - startTime;
            
            Interlocked.Increment(ref _processedMessages);
            _lastActivityTime = DateTime.UtcNow;
            
            Logger.LogInformation("Agent {AgentId} completed processing message {MessageId} in {ProcessingTime}ms",
                AgentId, message.MessageId, response.ProcessingTime.TotalMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _errorCount);
            Logger.LogError(ex, "Agent {AgentId} failed to process message {MessageId}", AgentId, message.MessageId);
            
            return new AgentResponse
            {
                OriginalMessageId = message.MessageId,
                AgentId = AgentId,
                Status = ResponseStatus.Error,
                ErrorMessage = ex.Message,
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
        finally
        {
            Status = AgentStatus.Active;
            _processingLock.Release();
        }
    }

    public virtual async Task<AgentDecision> MakeDecisionAsync(DecisionContext context, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Agent {AgentId} making decision for context {ContextId}", AgentId, context.ContextId);
        
        try
        {
            var decision = await OnMakeDecisionAsync(context, cancellationToken);
            decision.AgentId = AgentId;
            
            Logger.LogInformation("Agent {AgentId} made decision {DecisionId} with confidence {Confidence}",
                AgentId, decision.DecisionId, decision.Confidence);
            
            return decision;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Agent {AgentId} failed to make decision for context {ContextId}", AgentId, context.ContextId);
            throw;
        }
    }

    public virtual async Task<AgentHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new AgentHealthStatus
        {
            IsHealthy = Status == AgentStatus.Active && _errorCount < 10,
            Status = Status.ToString(),
            LastCheckTime = DateTime.UtcNow,
            ResponseTime = DateTime.UtcNow - _lastActivityTime,
            ProcessedMessages = _processedMessages,
            ErrorCount = _errorCount,
            SuccessRate = _processedMessages > 0 ? (double)(_processedMessages - _errorCount) / _processedMessages : 1.0,
            Metrics = new Dictionary<string, object>
            {
                ["LastActivityTime"] = _lastActivityTime,
                ["State"] = _state.ToList()
            }
        });
    }

    #region Protected Methods

    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected abstract Task<AgentResponse> OnProcessMessageAsync(AgentMessage message, CancellationToken cancellationToken = default);

    protected abstract Task<AgentDecision> OnMakeDecisionAsync(DecisionContext context, CancellationToken cancellationToken = default);

    protected void SetState(string key, object value)
    {
        _state[key] = value;
    }

    protected T? GetState<T>(string key)
    {
        return _state.TryGetValue(key, out var value) && value is T typedValue ? typedValue : default;
    }

    #endregion

    public virtual void Dispose()
    {
        _processingLock?.Dispose();
    }
}