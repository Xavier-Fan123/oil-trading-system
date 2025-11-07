# 快速参考卡片 - 90秒诊断指南

## 问题：API返回"字段缺失"错误

```
Error: "Contract validation failed: Valid X is required"
```

## 按顺序执行这5个Step

### Step 1: 检查数据库 (20秒)
```bash
curl http://localhost:5000/api/purchase-contracts?pageSize=1 | python3 -m json.tool | grep contractValue
```

**结果**:
- 字段存在且有值 → Step 2
- 字段缺失或为null → Step 3

---

### Step 2: 检查DTO (30秒)
打开: `src/OilTrading.Application/DTOs/PurchaseContractDto.cs`

搜索该字段：
- 存在 → Step 4
- 不存在 → 添加到DTO

---

### Step 3: 检查Seeding (60秒) ⚠️ 最可能是这里！
打开: `src/OilTrading.Infrastructure/Data/DataSeeder.cs`

**检查清单**:
```
[ ] 搜索 SeedPurchaseContractsAsync()
[ ] 是否有 contract.UpdatePricing(...)?
[ ] 是否有 contract.UpdatePaymentTerms(...)?
[ ] SeedAsync()顶部是否有 if (data.Any()) { return; }?
```

如果有短路逻辑 → 改为:
```csharp
await _context.PurchaseContracts.ExecuteDeleteAsync();
await _context.SalesContracts.ExecuteDeleteAsync();
await _context.Products.ExecuteDeleteAsync();
```

---

### Step 4: 清除数据 (20秒)
```bash
del C:\Users\itg\Desktop\X\src\OilTrading.Api\oiltrading.db*
dotnet run
```

---

### Step 5: 验证修复 (10秒)
```bash
curl http://localhost:5000/api/purchase-contracts?pageSize=1
```

检查:
- 新时间戳？ ✅
- contractValue有值？ ✅
- paymentTerms有值？ ✅

---

## 如果仍然失败

1. 检查 Program.cs:
   ```bash
   grep "AddScoped.*DataSeeder" Program.cs
   ```
   确保修改的是正确的文件

2. 检查日志:
   ```bash
   dotnet run
   # 查看是否有 "Skipping seeding" 消息
   ```

3. 仅在前4步都通过后，才考虑修改验证规则

---

## 关键记住

❌ **不要做这些**:
- 立即修改验证规则
- 假设你知道哪个seeder被使用
- 忽视 `if (data.Any()) { return; }` 短路逻辑
- 忘记删除数据库文件

✅ **总是做这些**:
- 先检查数据库中是否真的有数据
- 检查 Program.cs 的 DI 注册
- 改为 ExecuteDeleteAsync()
- 删除旧db文件并重启

---

## 最常见的根本原因

| 症状 | 原因 | 解决方案 |
|-----|------|--------|
| 字段为null | Seeding未调用Update* | 添加Update*调用 |
| 修改后仍有问题 | 旧db文件未删除 | `del *.db*` |
| 某个字段缺失 | DTO中未定义 | 添加到DTO |
| 验证仍然失败 | 代码有短路逻辑 | 改为ExecuteDeleteAsync |

---

## 完整分析文档

想要更多细节？查看:
- `ANALYSIS_SUMMARY.md` - 为什么花这么长时间
- `LESSONS_LEARNED.md` - 案例分析和学到教训
- `DEBUGGING_STRATEGY.md` - 详细诊断策略
- `CLAUDE.md` - 项目文档 (新增诊断章节)

---

## 时间对比

❌ **旧方法**: 修改验证规则
- 时间: 数小时
- 结果: 问题未解决

✅ **新方法**: 按Step 1-5诊断
- 时间: ~90秒
- 结果: 问题解决

