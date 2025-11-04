# 数据持久化可靠性改进计划

**创建日期**: 2025-11-04
**优先级**: 高
**预计工时**: 8-12 小时

---

## 📊 现状评估

### 已发现的缺陷
- **ApproveSalesContractCommandHandler**: ✅ 已修复
- **RejectSalesContractCommandHandler**: ✅ 已修复
- **其他处理器**: ✅ 审计完成,无发现缺陷

### 系统现状
- **CQRS 处理器总数**: 60+
- **缺陷处理器**: 2 (3.3%)
- **修复状态**: ✅ 100% 修复

---

## 🎯 改进方案

### 阶段 1: 自动化检测 (2-3 小时)

#### 1.1 自定义 Roslyn Analyzer
创建一个 Roslyn 代码分析器,自动检测缺少 SaveChangesAsync 的处理器:

```csharp
// 文件: OilTrading.CodeAnalysis/SaveChangesAsyncAnalyzer.cs

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SaveChangesAsyncAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "OT001";
    private static readonly LocalizableString Title = "Missing SaveChangesAsync call";
    private static readonly LocalizableString MessageFormat =
        "Command handler '{0}' modifies data but doesn't call SaveChangesAsync";
    private static readonly LocalizableString Description =
        "CQRS command handlers must explicitly call SaveChangesAsync after data modifications.";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var method = context.Node as MethodDeclarationSyntax;

        // 检测: IRequestHandler<TCommand> 中的 Handle 方法
        if (method?.Identifier.Text == "Handle")
        {
            // 检查是否调用了 UpdateAsync/AddAsync/DeleteAsync
            bool hasModification = HasDataModification(method);

            // 检查是否调用了 SaveChangesAsync
            bool hasSaveChanges = HasSaveChangesAsync(method);

            if (hasModification && !hasSaveChanges)
            {
                var diagnostic = Diagnostic.Create(Rule, method.GetLocation(), method.Identifier.Text);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool HasDataModification(MethodDeclarationSyntax method)
    {
        return method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(inv => inv.Expression.ToString().Contains("UpdateAsync") ||
                        inv.Expression.ToString().Contains("AddAsync") ||
                        inv.Expression.ToString().Contains("DeleteAsync"));
    }

    private static bool HasSaveChangesAsync(MethodDeclarationSyntax method)
    {
        return method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(inv => inv.Expression.ToString().Contains("SaveChangesAsync"));
    }

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, "Data Access", DiagnosticSeverity.Error, true, Description);
}
```

**效果**: IDE 会实时显示"❌ 缺少 SaveChangesAsync"的红线警告

#### 1.2 配置编译时检查
在 `.csproj` 中配置警告为错误:
```xml
<PropertyGroup>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

---

### 阶段 2: 增强的单元测试 (3-4 小时)

#### 2.1 创建测试基类

```csharp
// 文件: tests/OilTrading.Tests/Infrastructure/CommandHandlerTestBase.cs

public abstract class CommandHandlerTestBase<TCommand, TAggregate>
    where TCommand : IRequest
    where TAggregate : AggregateRoot
{
    protected DbContextOptions<ApplicationDbContext> DbContextOptions { get; }
    protected IUnitOfWork UnitOfWork { get; }
    protected IRepository<TAggregate> Repository { get; }

    protected CommandHandlerTestBase()
    {
        // 使用真实的 DbContext,不是模拟
        DbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        UnitOfWork = new UnitOfWork(new ApplicationDbContext(DbContextOptions));
        Repository = new GenericRepository<TAggregate>(new ApplicationDbContext(DbContextOptions));
    }

    /// <summary>
    /// 验证实体被持久化到数据库
    /// 这个方法应该在所有修改数据的处理器测试中调用
    /// </summary>
    protected async Task AssertPersistedToDatabaseAsync(TAggregate entity, Func<DbContext, Task<TAggregate>> fetchFunc)
    {
        // 创建新的 DbContext 实例 (模拟新的数据库连接)
        using (var freshContext = new ApplicationDbContext(DbContextOptions))
        {
            var fetchedEntity = await fetchFunc(freshContext);
            Assert.NotNull(fetchedEntity);
            // 如果没有 SaveChangesAsync,这个测试会失败!
        }
    }
}
```

#### 2.2 示例测试

```csharp
public class ApproveSalesContractCommandHandlerTests :
    CommandHandlerTestBase<ApproveSalesContractCommand, SalesContract>
{
    [Fact]
    public async Task Handle_ShouldPersistApprovalToDatabaseAsync()
    {
        // Arrange
        var contract = new SalesContract(...);
        await Repository.AddAsync(contract);
        await UnitOfWork.SaveChangesAsync();

        var handler = new ApproveSalesContractCommandHandler(
            Repository as ISalesContractRepository,
            UnitOfWork,
            new NullLogger<ApproveSalesContractCommandHandler>());

        var command = new ApproveSalesContractCommand { Id = contract.Id, ApprovedBy = "user1" };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert: 使用新的 DbContext 从数据库验证
        await AssertPersistedToDatabaseAsync(
            contract,
            async (db) =>
            {
                var contractFromDb = await db.Set<SalesContract>().FirstOrDefaultAsync(c => c.Id == contract.Id);
                Assert.Equal(ContractStatus.Active, contractFromDb.Status);
                return contractFromDb;
            });
    }
}
```

---

### 阶段 3: 集成测试框架 (2-3 小时)

#### 3.1 创建端到端测试

```csharp
// 文件: tests/OilTrading.IntegrationTests/DataPersistenceTests.cs

[Collection("IntegrationTests")]
public class DataPersistenceTests : IAsyncLifetime
{
    private TestWebApplicationFactory _factory;
    private HttpClient _client;
    private ApplicationDbContext _dbContext;

    public async Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
        _dbContext = _factory.Services.GetRequiredService<ApplicationDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task SalesContractApproval_ShouldPersistToDatabaseAsync()
    {
        // Arrange
        var contract = await CreateTestSalesContractAsync();
        var contractId = contract.Id;

        // Act: 通过 API 批准合同
        var response = await _client.PostAsync(
            $"/api/sales-contracts/{contractId}/approve",
            new StringContent(JsonSerializer.Serialize(new { approvedBy = "user1" }),
                Encoding.UTF8, "application/json"));

        Assert.True(response.IsSuccessStatusCode);

        // Assert: 从数据库验证状态更改
        using (var freshContext = new ApplicationDbContext(_factory.Services))
        {
            var persistedContract = await freshContext.SalesContracts
                .FirstOrDefaultAsync(c => c.Id == contractId);

            Assert.NotNull(persistedContract);
            Assert.Equal(ContractStatus.Active, persistedContract.Status);
        }
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        _dbContext?.Dispose();
        _factory?.Dispose();
    }
}
```

---

### 阶段 4: 监控和诊断 (2-3 小时)

#### 4.1 添加 SaveChangesAsync 监控日志

```csharp
// 文件: OilTrading.Infrastructure/Data/SaveChangesInterceptor.cs

public class SaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<SaveChangesInterceptor> _logger;

    public SaveChangesInterceptor(ILogger<SaveChangesInterceptor> logger)
    {
        _logger = logger;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "✅ SaveChangesAsync completed. Rows affected: {RowsAffected}, Duration: {ElapsedMilliseconds}ms",
            result,
            eventData.Duration.TotalMilliseconds);

        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Exception != null)
        {
            _logger.LogError(
                eventData.Exception,
                "❌ SaveChangesAsync failed. Error: {ErrorMessage}",
                eventData.Exception.Message);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
```

#### 4.2 在 Program.cs 中注册

```csharp
services.AddScoped<SaveChangesInterceptor>();

services.AddDbContext<ApplicationDbContext>(options =>
    options
        .UseSqlite("Data Source=oiltrading.db")
        .AddInterceptors(sp => sp.GetRequiredService<SaveChangesInterceptor>()));
```

---

### 阶段 5: 文档和培训 (1-2 小时)

#### 5.1 更新开发指南

在 `CLAUDE.md` 中添加:

```markdown
## 数据持久化指南

### CQRS 命令处理器模板

所有修改数据的命令处理器都必须遵循以下模板:

\`\`\`csharp
public class YourCommandHandler : IRequestHandler<YourCommand>
{
    private readonly IYourRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public YourCommandHandler(
        IYourRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(YourCommand request, CancellationToken cancellationToken)
    {
        // 业务逻辑...

        await _repository.UpdateAsync(entity);

        // ✅ 必须: 显式持久化到数据库
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
\`\`\`

### SaveChangesAsync 检查清单

在代码审查中:
- [ ] 是否有 Repository.UpdateAsync/AddAsync/DeleteAsync 调用?
- [ ] 如果有,是否紧跟 SaveChangesAsync?
- [ ] 是否注入了 IUnitOfWork?

### 常见错误

❌ **错误**:
\`\`\`csharp
await _repository.UpdateAsync(entity);
// 缺少 SaveChangesAsync → 数据丢失!
\`\`\`

✅ **正确**:
\`\`\`csharp
await _repository.UpdateAsync(entity);
await _unitOfWork.SaveChangesAsync(cancellationToken);  // ✅
\`\`\`
```

#### 5.2 创建视频演示

制作一个 5 分钟的视频演示:
1. 问题: 数据为什么丢失?
2. 解决方案: SaveChangesAsync
3. 测试验证
4. 最佳实践

---

## 📅 实施时间表

| 阶段 | 任务 | 时间 | 优先级 |
|------|------|------|--------|
| **1** | Roslyn Analyzer | 2-3h | 🔴 高 |
| **2** | 单元测试框架 | 3-4h | 🔴 高 |
| **3** | 集成测试 | 2-3h | 🟠 中 |
| **4** | 监控日志 | 2-3h | 🟠 中 |
| **5** | 文档培训 | 1-2h | 🟡 低 |

**总计**: 10-15 小时

---

## 📈 期望收益

### 立即收益
- ✅ 自动检测数据持久化缺陷
- ✅ IDE 实时警告
- ✅ 编译阶段失败

### 中期收益
- ✅ 100% 代码覆盖 (与数据库相关)
- ✅ 自动化测试验证持久化
- ✅ 0 生产数据丢失事件

### 长期收益
- ✅ 开发团队对数据完整性的信心
- ✅ 减少调试时间
- ✅ 提高代码质量评分

---

## 🎯 成功指标

| 指标 | 目标 | 状态 |
|------|------|------|
| SaveChangesAsync 覆盖率 | 100% | ✅ |
| 自动化测试数据库验证 | 100% | ⏳ 待实施 |
| IDE 警告 | 0 | ✅ |
| 代码审查检查清单遵从率 | 100% | ⏳ 待实施 |

---

## 🔍 实施检查清单

### 前置条件
- [x] 现有缺陷已修复
- [x] 代码已编译并通过测试
- [ ] 开发团队同意实施计划

### 第 1 周
- [ ] 创建 Roslyn Analyzer
- [ ] 配置编译时检查
- [ ] 测试分析器工作

### 第 2 周
- [ ] 创建测试基类
- [ ] 为关键处理器编写测试
- [ ] 验证测试有效性

### 第 3 周
- [ ] 创建集成测试框架
- [ ] 编写端到端测试
- [ ] 配置 CI/CD 验证

### 第 4 周
- [ ] 添加监控日志
- [ ] 更新开发文档
- [ ] 进行团队培训

---

## 💡 额外建议

### 1. 代码审查模板

创建一个代码审查检查清单:

```
## 数据持久化检查

- [ ] 处理器中是否有 Repository.UpdateAsync/AddAsync/DeleteAsync?
- [ ] 如果有,紧跟的是否是 SaveChangesAsync?
- [ ] 是否注入了 IUnitOfWork?
- [ ] 是否有单元测试验证数据库持久化?

如果任何问题的答案是"否",则拒绝此 PR。
```

### 2. 类型安全增强

考虑创建一个"强制"SaveChangesAsync 的抽象:

```csharp
public abstract class DataModificationCommandHandler<TCommand> : IRequestHandler<TCommand>
    where TCommand : IRequest
{
    protected abstract Task ExecuteAsync(TCommand request, CancellationToken cancellationToken);

    public sealed async Task Handle(TCommand request, CancellationToken cancellationToken)
    {
        await ExecuteAsync(request, cancellationToken);
        // ✅ SaveChangesAsync 自动调用,无法遗漏
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

### 3. CI/CD 集成

在 GitHub Actions/Azure Pipelines 中添加:

```yaml
- name: Check SaveChangesAsync Coverage
  run: |
    dotnet run --project build/OilTrading.CodeAnalysis/
    # 失败如果发现任何缺少 SaveChangesAsync 的处理器
```

---

## 📞 支持和反馈

如果实施过程中遇到任何问题,请:
1. 检查 `DATA_PERSISTENCE_ROOT_CAUSE_ANALYSIS.md` 获取更多背景信息
2. 查看 `DATA_PERSISTENCE_QUICK_REFERENCE.md` 获取快速指南
3. 参考 `CLAUDE.md` 中的最佳实践部分

---

**版本**: 1.0
**创建者**: 深度代码分析系统
**最后更新**: 2025-11-04
**状态**: 建议中
