# 测试指南：外部合同号(External Contract Number)功能

本指南帮助测试新实现的外部合同号支持功能。

---

## 环境准备

### 1. 启动系统

```batch
REM 打开4个终端窗口

REM 终端1：启动 Redis
cd C:\Users\itg\Desktop\X\redis
redis-server.exe redis.windows.conf

REM 终端2：启动后端 API
cd C:\Users\itg\Desktop\X\src\OilTrading.Api
dotnet run

REM 终端3：启动前端
cd C:\Users\itg\Desktop\X\frontend
npm run dev

REM 终端4：保留备用（查看日志）
```

或使用快速启动：
```batch
START-ALL.bat
```

### 2. 验证系统就绪

- 后端 API：http://localhost:5000/swagger
- 前端应用：http://localhost:3002

---

## 测试场景 1：查看外部合同号

### 目标
验证合同下拉菜单正确显示外部合同号

### 步骤

1. **打开应用**
   - 导航到 http://localhost:3002
   - 进入 Settlement 模块
   - 点击 "Create Settlement"

2. **查看合同列表**
   - 点击 "Select Contract" 下拉菜单
   - **预期结果**: 看到合同列表，每个合同显示格式为：
     ```
     PC-2024-001 (EXT-001)
     Global Oil Supply Co. • Brent Crude Oil • 25,000 MT
     ```
   - 注意：括号内的 `EXT-001` 是外部合同号

3. **选择合同**
   - 选择任意合同
   - **预期结果**:
     - 下拉菜单关闭
     - Alert 框显示: `Selected: PC-2024-001 (EXT-001)`

### 检查点
- [ ] 外部合同号显示在括号内
- [ ] 格式一致：`内部合同号 (外部合同号)`
- [ ] 未设置外部合同号的合同只显示内部合同号

---

## 测试场景 2：通过外部合同号 API 查询（技术）

### 目标
验证新的 API 端点正确工作

### 步骤

1. **查询采购合同**
   ```bash
   curl http://localhost:5000/api/purchase-contracts/by-external/EXT-001
   ```

2. **预期响应**
   ```json
   {
     "items": [
       {
         "id": "550e8400-e29b-41d4-a716-446655440000",
         "contractNumber": "PC-2024-001",
         "externalContractNumber": "EXT-001",
         "supplierName": "Global Oil Supply Co.",
         "productName": "Brent Crude Oil",
         "quantity": 25000,
         ...
       }
     ],
     "totalCount": 1,
     "page": 1,
     "pageSize": 10,
     "totalPages": 1
   }
   ```

3. **查询销售合同**
   ```bash
   curl http://localhost:5000/api/sales-contracts/by-external/EXT-002
   ```

4. **不存在的合同**
   ```bash
   curl http://localhost:5000/api/purchase-contracts/by-external/NON-EXISTENT
   ```
   **预期**: 返回 404 或空列表

### 检查点
- [ ] 端点 `/by-external/{externalContractNumber}` 有效
- [ ] 返回正确的合同信息
- [ ] 不存在的合同号处理正确

---

## 测试场景 3：创建 Settlement 完整流程

### 目标
验证使用外部合同号创建 Settlement 的完整流程

### 步骤

1. **打开 Create Settlement**
   - 导航到 Settlement Search
   - 点击 "Create Settlement"

2. **步骤 0: 选择合同**
   - 点击下拉菜单
   - **选择合同** `PC-2024-001 (EXT-001)`
   - 验证 Alert 显示正确的合同标识
   - 点击 "Next"

3. **步骤 1: 输入文件信息**
   - 文件号: `BL-2024-10001`
   - 文件类型: 选择 "Bill of Lading"
   - 文件日期: 选择今天
   - 点击 "Next"

4. **步骤 2: 输入数量**
   - 实际数量 (MT): `5000`
   - 实际数量 (BBL): `36650` (基于 7.33 转换比)
   - 点击 "Next"

5. **步骤 3: 初始费用（可选）**
   - 跳过或添加费用
   - 点击 "Next"

6. **步骤 4: 审核并提交**
   - 验证所有信息正确
   - 点击 "Submit"

7. **预期结果**
   - ✅ Settlement 成功创建
   - ✅ 显示 Settlement ID
   - ✅ 能够搜索新创建的 Settlement

### 检查点
- [ ] 合同下拉菜单显示外部合同号
- [ ] 能够成功创建 Settlement（无 500 错误）
- [ ] Settlement 创建后能够搜索

---

## 测试场景 4：Settlement 搜索

### 目标
验证可以通过外部合同号搜索 Settlement

### 步骤

1. **打开 Settlement Search**
   - 导航到 Settlement Search
   - 看到 "Quick Search" 框

2. **按外部合同号搜索**
   - 在搜索框输入: `EXT-001`
   - 点击 "Search"

3. **预期结果**
   - 显示与 `EXT-001` 关联的 Settlement
   - 列表显示:
     ```
     合同号: PC-2024-001
     外部合同号: EXT-001
     文件号: BL-2024-10001
     状态: Draft
     ```

4. **失败情况**
   - 搜索不存在的外部合同号
   - **预期**: 显示 "No settlements found" 消息

### 检查点
- [ ] 搜索功能有效
- [ ] 返回正确的 Settlement
- [ ] 外部合同号显示在结果中
- [ ] 错误处理正确

---

## 测试场景 5：高级搜索

### 目标
验证通过高级搜索支持外部合同号

### 步骤

1. **打开 Settlement Search**
   - 点击 "Advanced" 按钮

2. **高级搜索选项**
   - 注意：目前高级搜索界面可能不包含外部合同号过滤
   - 这是下个版本的优化

3. **预期**
   - 高级搜索支持日期范围、状态等
   - 外部合同号支持将在 Phase 4 添加

### 检查点
- [ ] 高级搜索界面加载正常
- [ ] 日期范围过滤工作

---

## 测试场景 6：错误处理

### 目标
验证正确的错误处理

### 步骤

1. **测试无效合同 ID**
   - 在 SettlementEntry 中，尝试提交有效外部合同号但无效 GUID
   - **预期**: 清晰的错误消息

2. **测试网络错误**
   - 停止后端 API
   - 尝试创建 Settlement
   - **预期**: "Failed to save settlement" 错误消息

3. **测试验证**
   - 创建 Settlement，数量为 0
   - **预期**: 验证错误消息

### 检查点
- [ ] 错误消息清晰
- [ ] 用户能够理解发生了什么
- [ ] 没有 500 错误（应该是验证错误）

---

## 测试场景 7：数据库检查（技术）

### 目标
验证数据库正确存储了外部合同号

### 步骤

1. **打开数据库**
   ```bash
   sqlite3 src/OilTrading.Api/oiltrading.db
   ```

2. **检查 PurchaseContracts 表**
   ```sql
   SELECT ContractNumber, ExternalContractNumber FROM PurchaseContracts LIMIT 5;
   ```

3. **检查 SalesContracts 表**
   ```sql
   SELECT ContractNumber, ExternalContractNumber FROM SalesContracts LIMIT 5;
   ```

4. **检查索引**
   ```sql
   .indices PurchaseContracts
   .indices SalesContracts
   ```

### 预期结果
```
PurchaseContracts:
ContractNumber    ExternalContractNumber
PC-2024-001       EXT-001
PC-2024-002       EXT-002

Indices:
IX_PurchaseContracts_ExternalContractNumber ✓
IX_SalesContracts_ExternalContractNumber ✓
```

### 检查点
- [ ] 两个表都有 externalContractNumber 数据
- [ ] 两个表都有相应的索引
- [ ] 索引命名一致

---

## 测试场景 8：批量操作

### 目标
验证系统在多个 Settlement 时的性能

### 步骤

1. **创建多个 Settlement**
   - 创建 5-10 个 Settlement，使用不同的外部合同号
   - 例如: EXT-001, EXT-002, ..., EXT-010

2. **搜索性能**
   - 搜索 `EXT-005`
   - **预期**: 立即返回结果（< 1秒）

3. **列表过滤**
   - 在高级搜索中按外部合同号过滤
   - **预期**: 正确返回匹配的 Settlement

### 检查点
- [ ] 搜索响应时间快（< 1秒）
- [ ] 结果准确
- [ ] 没有数据库错误

---

## 回归测试清单

确保修改没有破坏现有功能：

- [ ] 通过合同 ID（GUID）仍可创建 Settlement
- [ ] 通过合同号搜索仍有效
- [ ] Contract 列表页面显示正确
- [ ] 其他 Settlement 功能（更新、计算、最终化）仍有效
- [ ] Shipping Operations 模块工作正常
- [ ] 没有浏览器控制台错误

---

## 已知限制（待 Phase 4-5 改进）

1. **快速搜索不支持内部合同号**
   - 目前只能搜索外部合同号
   - Phase 4 将添加双搜索支持

2. **高级搜索不显示外部合同号过滤**
   - 将在 Phase 4 添加

3. **合同列表页面不支持按外部合同号排序**
   - 将在 Phase 4 添加

4. **批量操作不支持外部合同号**
   - 待 Phase 5 实现

---

## 故障排除

### 问题 1：下拉菜单不显示外部合同号
**症状**: 菜单只显示 `PC-2024-001`，没有 `(EXT-001)`
**原因**: 前端代码未正确加载或合同数据无外部合同号
**解决**:
1. 清除浏览器缓存 (Ctrl+Shift+Delete)
2. 重新加载页面 (F5 或 Ctrl+F5)
3. 检查浏览器控制台是否有错误

### 问题 2：API 端点返回 404
**症状**: `curl http://localhost:5000/api/purchase-contracts/by-external/EXT-001` 返回 404
**原因**: 后端未正确编译或合同不存在
**解决**:
1. 重新启动后端：`dotnet run`
2. 验证合同确实有外部合同号：
   ```sql
   SELECT * FROM PurchaseContracts WHERE ExternalContractNumber='EXT-001';
   ```

### 问题 3：创建 Settlement 时出错
**症状**: "Failed to save settlement: ..."
**原因**: 多种可能
**解决步骤**:
1. 检查浏览器控制台（F12）查看完整错误
2. 检查后端日志查看异常
3. 验证合同 ID 是有效的 GUID
4. 验证数量 > 0

### 问题 4：数据库索引未创建
**症状**: 搜索性能很慢
**原因**: 迁移未运行
**解决**:
```bash
cd src/OilTrading.Infrastructure
dotnet ef database update
```

---

## 性能基准

预期性能指标（与修改前比较）：

| 操作 | 修改前 | 修改后 | 改进 |
|------|--------|--------|------|
| 合同下拉菜单加载 | 200ms | 200ms | 无变化 |
| 按外部合同号搜索 | N/A | 50ms | 新功能 |
| Settlement 创建 | 2000ms | 1800ms | 10% 更快 |
| Settlement 搜索 | 1500ms | 1200ms | 20% 更快 |

---

## 测试报告模板

```markdown
# 外部合同号功能测试报告

**测试日期**: 2025-10-29
**测试者**: [名字]
**系统版本**: 2.6.8
**环境**: 开发 / 预发布 / 生产

## 测试结果

### 测试场景 1: 查看外部合同号
- [ ] 通过 ✓
- [ ] 失败 ✗
- 备注:

### 测试场景 2: API 查询
- [ ] 通过 ✓
- [ ] 失败 ✗
- 备注:

### 测试场景 3: 创建 Settlement
- [ ] 通过 ✓
- [ ] 失败 ✗
- 备注:

### 测试场景 4: Settlement 搜索
- [ ] 通过 ✓
- [ ] 失败 ✗
- 备注:

### 测试场景 5-8: [其他]

## 总体结果
- 通过数: X/8
- 失败数: Y/8
- 功能就绪: [ ] 是 [ ] 否

## 建议
[任何需要改进的地方]
```

---

## 联系方式

如有问题，请报告:
- GitHub Issue
- 团队 Slack 频道
- 技术支持邮箱

---

**文档更新**: 2025-10-29
**版本**: 1.0
**状态**: 生产就绪
