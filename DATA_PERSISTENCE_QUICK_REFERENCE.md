# 数据持久化问题 - 快速参考指南

## 🔴 问题概述

您的系统中存在**数据无法保存**的问题。原因是某些 CQRS 命令处理器在修改数据后**没有显式调用 `SaveChangesAsync`**。

## ✅ 修复状态

**已修复**: 2 个关键处理器
```
✅ ApproveSalesContractCommandHandler
✅ RejectSalesContractCommandHandler
```

**构建状态**: ✅ 0 错误, 0 警告

---

## 📋 问题原理

### ❌ 错误的方式 (数据丢失)
```csharp
public async Task Handle(SomeCommand request, CancellationToken cancellationToken)
{
    var entity = await _repository.GetByIdAsync(request.Id);
    entity.DoSomething();  // 修改实体

    await _repository.UpdateAsync(entity);  // EF Core 标记为已修改
    // ❌ 缺少这一行 → 数据不会保存到数据库!
    // await _unitOfWork.SaveChangesAsync(cancellationToken);
}
```

### ✅ 正确的方式 (数据保存)
```csharp
public async Task Handle(SomeCommand request, CancellationToken cancellationToken)
{
    var entity = await _repository.GetByIdAsync(request.Id);
    entity.DoSomething();  // 修改实体

    await _repository.UpdateAsync(entity);
    await _unitOfWork.SaveChangesAsync(cancellationToken);  // ✅ 必须有!
}
```

---

## 📝 标准模板 (复制粘贴)

所有命令处理器都应该遵循这个模板:

```csharp
using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.YourModule;

public class YourCommandHandler : IRequestHandler<YourCommand>
{
    // ✅ 总是注入这两个依赖
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
        // 1. 获取实体
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
            throw new NotFoundException($"Entity not found");

        // 2. 修改实体
        entity.DoSomething(request.Parameter);

        // 3. 更新 Repository
        await _repository.UpdateAsync(entity, cancellationToken);

        // 4. ✅ 关键: 持久化到数据库
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

---

## 🔍 检查清单

使用这个清单检查是否遗漏了 SaveChangesAsync:

```
在任何命令处理器中:
□ 是否调用了 _repository.AddAsync()?
□ 是否调用了 _repository.UpdateAsync()?
□ 是否调用了 _repository.DeleteAsync()?

如果上面任何一个是 Yes,那么:
□ 后面是否有 await _unitOfWork.SaveChangesAsync()?
  - YES ✅ 正确
  - NO ❌ 缺陷! 需要添加
```

---

## 🧪 测试验证方法

### 1. 快速手动测试

```bash
# 1. 启动后端
cd src/OilTrading.Api
dotnet run

# 2. 在另一个终端启动前端
cd frontend
npm run dev

# 3. 执行操作:
# - 创建销售合同
# - 批准合同
# - 刷新页面
# ✅ 合同应该仍然显示为 "已批准" (数据被保存)
# ❌ 如果刷新后返回 "待批准", 说明有 SaveChangesAsync 缺失
```

### 2. 数据库检查

```sql
-- 查看合同状态
SELECT Id, ContractNumber, Status, UpdatedAt
FROM SalesContracts
ORDER BY UpdatedAt DESC
LIMIT 5;

-- 刷新前: Status = PendingApproval
-- 批准后: Status = Active
-- ✅ 如果状态改变,说明 SaveChangesAsync 工作正常
```

---

## 🛠️ 常见问题

### Q1: 为什么 UpdateAsync 不自动保存数据?
**A**: Repository 只标记实体为已修改,实际的数据库写入需要 `SaveChangesAsync` 来执行。这遵循"单一职责"原则 - Repository 只处理对象,UnitOfWork 处理事务。

### Q2: 为什么有些处理器没有这个问题?
**A**: 因为其他大多数处理器都正确调用了 `SaveChangesAsync`。只有 2 个处理器漏掉了。

### Q3: 如果我忘记添加 SaveChangesAsync 会怎样?
**A**: 修改会在内存中,但永远不会写入数据库。用户会看到"保存成功"的假消息,但刷新后修改消失。

### Q4: SaveChangesAsync vs SaveChanges 有区别吗?
**A**: 是的! 总是使用 `SaveChangesAsync` (异步版本),因为:
- 不阻塞线程
- 性能更好
- 符合 ASP.NET Core 异步最佳实践

### Q5: 一个方法中可以调用多次 SaveChangesAsync 吗?
**A**: 可以,但不推荐。最佳实践是在方法末尾调用一次。如果需要多个事务,使用 `BeginTransactionAsync`。

---

## 🚨 要立即执行的操作

### 步骤 1: 验证修复
```bash
# 检查已修复的文件
git diff src/OilTrading.Application/Commands/SalesContracts/
```

### 步骤 2: 构建确认
```bash
dotnet build OilTrading.sln
# 应该显示: "Build succeeded. 0 warnings, 0 errors"
```

### 步骤 3: 运行测试 (可选)
```bash
dotnet test OilTrading.sln
# 所有测试应该通过
```

### 步骤 4: 手动端到端测试
1. 启动后端和前端
2. 创建销售合同
3. 批准合同
4. 刷新页面
5. ✅ 合同应该仍然显示为已批准状态

---

## 📚 相关文件

| 文件 | 说明 | 状态 |
|------|------|------|
| ApproveSalesContractCommandHandler.cs | 批准销售合同处理器 | ✅ 已修复 |
| RejectSalesContractCommandHandler.cs | 拒绝销售合同处理器 | ✅ 已修复 |
| UnitOfWork.cs | 事务协调器 | ✅ 正常 |
| ApplicationDbContext.cs | 数据上下文 | ✅ 正常 |

---

## 💡 预防类似问题

### 编码规则
```
对于所有修改数据的 CQRS 处理器:
1. 注入 IUnitOfWork
2. 在最后调用 await _unitOfWork.SaveChangesAsync()
3. 就完成了!
```

### 代码审查检查表
```
□ 处理器中有数据修改操作吗?
  □ 如果是: 是否有 SaveChangesAsync?
    □ 是: ✅ 通过
    □ 否: ❌ 要求修改

□ 是否注入了所有必要的依赖?
□ 是否有适当的异常处理?
□ 是否有日志记录?
```

---

## 📞 更多帮助

完整的分析报告: `DATA_PERSISTENCE_ROOT_CAUSE_ANALYSIS.md`

该文件包含:
- 详细的根本原因分析
- 系统架构图
- 修复前后的代码对比
- 最佳实践建议
- 测试策略

---

**最后更新**: 2025-11-04
**状态**: ✅ 修复完成
