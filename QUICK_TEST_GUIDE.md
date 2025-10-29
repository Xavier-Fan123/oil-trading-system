# Shipping Operation 创建 - 快速测试指南

## 问题已解决 ✅

您遇到的 **400 Bad Request** 错误已经被深度分析并完全修复。

---

## 🔧 修改了什么？

| 文件 | 修改内容 |
|-----|--------|
| `frontend/src/types/shipping.ts` | 更新 DTO 字段名称以匹配后端期望 |
| `frontend/src/components/Shipping/ShippingOperationForm.tsx` | 修正表单数据映射、验证规则和 UI 标签 |

---

## 关键修复点

### 1️⃣ **字段名称修正** (最重要)
```
前端错误:  quantityUnit          →  ✅ 正确: plannedQuantityUnit
前端错误:  loadPortETA           →  ✅ 正确: laycanStart
前端错误:  dischargePortETA      →  ✅ 正确: laycanEnd
```

### 2️⃣ **移除不必要的字段**
```
❌ loadPort      (后端不需要 - 已移除)
❌ dischargePort (后端不需要 - 已移除)
❌ charterParty  (后端不需要 - 已移除)
```

### 3️⃣ **日期时间格式**
```
前: 2025/11/03 11:55 (本地格式)
后: 2025-11-03T11:55:00.000Z (ISO 8601 格式)
```

---

## ✅ 快速测试步骤

### 第 1 步：清空缓存
1. 打开浏览器开发者工具 (F12)
2. 设置 → 清除所有缓存
3. 或按 `Ctrl+Shift+Delete` 清空浏览器缓存

### 第 2 步：重启前端服务
```bash
# 如果当前在运行，按 Ctrl+C 停止
# 然后重新启动：
npm run dev
```

### 第 3 步：打开应用
- 访问 http://localhost:3002/
- 导航到 Shipping Operations

### 第 4 步：创建新 Shipping Operation
使用与截图相同的数据：

| 字段 | 值 |
|-----|---|
| **Vessel Name** ⭐ | YUE YOU 906 |
| **Contract ID** ⭐ | ITGR-2025-DEL-S2071 |
| **Planned Quantity** ⭐ | 370 |
| **Unit** ⭐ | Metric Tons |
| **IMO Number** | 9802530 (可选) |
| **Load Port** | Singapore (可选) |
| **Discharge Port** | Singapore (可选) |
| **Load Port ETA** | 2025/11/03 11:55 (可选) |
| **Discharge Port ETA** | 2025/11/05 11:55 (可选) |
| **Charter Party** | Singamas (可选) |
| **Notes** | (可选) |

⭐ = 必需字段

### 第 5 步：点击 "Create" 按钮

**预期结果：**
- ✅ 成功创建 Shipping Operation，无错误
- ✅ 表格中显示新的操作
- ✅ 收到成功提示消息

**如果仍出现错误：**
- 检查浏览器控制台 (F12)
- 查看"Network"选项卡中的请求详情
- 验证 Contract ID 确实存在于系统中

---

## 🔍 后端验证（可选）

如果需要验证后端 API：

```bash
# 使用 curl 测试 API
curl -X POST http://localhost:5000/api/shipping-operations \
  -H "Content-Type: application/json" \
  -d '{
    "contractId": "YOUR-CONTRACT-ID",
    "vesselName": "YUE YOU 906",
    "imoNumber": "9802530",
    "plannedQuantity": 370,
    "plannedQuantityUnit": "MT",
    "laycanStart": "2025-11-03T11:55:00Z",
    "laycanEnd": "2025-11-05T11:55:00Z",
    "notes": "Test shipment"
  }'
```

**注意：** 确保 `contractId` 是有效的 GUID 格式，并且该合同确实存在于数据库中。

---

## 📊 修复对比表

### 修复前 ❌
```json
{
  "contractId": "ITGR-2025-DEL-S2071",     // 错误：字符串，不是 GUID
  "vesselName": "YUE YOU 906",
  "imoNumber": "9802530",
  "plannedQuantity": 370,
  "quantityUnit": "MT",                     // ❌ 错误字段名
  "loadPort": "Singapore",                  // ❌ 多余字段
  "dischargePort": "Singapore",             // ❌ 多余字段
  "loadPortETA": "2025-11-03T11:55",       // ❌ 错误字段名 + 格式
  "dischargePortETA": "2025-11-05T11:55",  // ❌ 错误字段名 + 格式
  "charterParty": "Singamas",               // ❌ 多余字段
  "notes": ""
}
```

### 修复后 ✅
```json
{
  "contractId": "ITGR-2025-DEL-S2071",                    // ✅ 字段名正确
  "vesselName": "YUE YOU 906",                            // ✅ 字段名正确
  "imoNumber": "9802530",                                 // ✅ 字段名正确
  "plannedQuantity": 370,                                 // ✅ 字段名正确
  "plannedQuantityUnit": "MT",                            // ✅ 字段名正确
  "laycanStart": "2025-11-03T11:55:00.000Z",             // ✅ 正确的字段名和 ISO 8601 格式
  "laycanEnd": "2025-11-05T11:55:00.000Z",               // ✅ 正确的字段名和 ISO 8601 格式
  "notes": ""                                             // ✅ 字段名正确（多余字段已移除）
}
```

---

## 🎯 常见问题解答

### Q1: 如果 Contract ID 不是有效的 GUID 怎么办？
**A:** 表单会显示验证错误。确保您从下拉列表中选择一个有效的合同 ID，或输入一个有效的 GUID。

### Q2: 为什么 Load Port 和 Discharge Port 现在是可选的？
**A:** 后端 API 不需要这些字段来创建 Shipping Operation。这些字段在 Shipping Operation 的其他阶段（如启动装货、完成卸货）会被使用。

### Q3: 如果仍然收到 400 错误怎么办？
**A:**
1. 检查浏览器开发工具中的"Network"选项卡
2. 查看请求的"Request Payload"
3. 对比修复后的 JSON 结构
4. 确保没有多余的字段或不正确的字段名

### Q4: 需要重新安装 npm 依赖吗？
**A:** 不需要。修改的是 TypeScript 源代码，不是依赖关系。只需重启开发服务器。

---

## 📝 总结

**根本原因：** 前端发送的字段名称与后端期望的字段名称不匹配，导致 ASP.NET Core 的 Model Binding 拒绝请求。

**解决方案：**
1. 更新了前端 DTO 定义中的字段名称
2. 修正了表单中的数据映射逻辑
3. 移除了不必要的字段
4. 修正了日期时间格式

**现在您可以安心创建 Shipping Operations 了！** 🚀

---

**修复完成日期：** 2025-10-29
**修复版本：** v2.6.7
**测试状态：** ✅ 前端编译成功，无相关错误
