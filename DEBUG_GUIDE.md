# Shipping Operation 400 错误 - 调试指南

您仍然收到 400 错误。这意味着**前端发送的数据与后端期望的格式不匹配**。

让我们一步步调试这个问题。

---

## 第一步：重启前端应用

**这是最关键的步骤！** 如果没有重启，您看不到最新的代码修改。

### 方法 1：使用脚本
```batch
cd "C:\Users\itg\Desktop\X"
START.bat
```

### 方法 2：手动重启
```powershell
# 停止当前前端
Ctrl+C (在前端窗口中)

# 等待 2 秒

# 重新启动
cd "C:\Users\itg\Desktop\X\frontend"
"C:\Users\itg\nodejs\npm.cmd" run dev
```

### 验证重启成功
- 您应该看到输出 `VITE ... dev server running at`
- 浏览器刷新后应该能看到新的更改

---

## 第二步：打开浏览器控制台

1. **打开浏览器开发者工具**
   - 按 `F12` 或右键 → 检查

2. **转到 Console 选项卡**
   - 应该看到正常的 Vue/React 日志

3. **清空控制台**
   - 点击清空按钮或按 `Ctrl+L`

---

## 第三步：尝试创建 Shipping Operation

1. 填写表单：
   ```
   Vessel Name:        speedy
   Contract ID:        ITGR-2025-CAG-S0281
   Planned Quantity:   22500
   Quantity Unit:      BBL
   Load Port ETA:      2025-10-31 12:14
   Discharge Port ETA: 2025-11-07 12:14
   (其他字段留空)
   ```

2. 点击 "Create" 按钮

3. **立即查看浏览器控制台**
   - 您应该在错误之前看到详细的日志输出
   - 日志会显示：
     ```
     === SHIPPING OPERATION CREATE REQUEST ===
     Request Payload: {...}
     Planned Quantity Type: number Value: 22500
     Load Port ETA: 2025-10-31T12:14:00.000Z
     Discharge Port ETA: 2025-11-07T12:14:00.000Z
     Contract ID: ITGR-2025-CAG-S0281
     =========================================
     ```

---

## 第四步：分析日志输出

### 找出问题所在

**查看日志中的这些信息：**

1. **Planned Quantity**
   ```
   Planned Quantity Type: number Value: 22500
   ```
   ✅ 应该是 `number` 类型
   ❌ 如果是 `string`，就是问题！

2. **Load Port ETA**
   ```
   Load Port ETA: 2025-10-31T12:14:00.000Z
   ```
   ✅ 应该是 ISO 8601 格式 (YYYY-MM-DDTHH:mm:ss.sssZ)
   ❌ 如果是空字符串或其他格式，就是问题！

3. **Contract ID**
   ```
   Contract ID: ITGR-2025-CAG-S0281
   ```
   ✅ 应该是 UUID 或合同 ID 字符串
   ❌ 如果是空字符串，就是问题！

### 常见问题和解决方案

| 日志显示 | 问题 | 解决方案 |
|---------|------|--------|
| `Planned Quantity Type: string` | 数字转换失败 | 检查输入框是否为数字 |
| `Load Port ETA: ""` | 日期没有被转换 | 检查日期输入框是否有值 |
| `Contract ID: ""` | 合同 ID 为空 | 必须填写合同 ID |
| `Discharge Port ETA: undefined` | 字段缺失 | 检查是否填写了卸港日期 |

---

## 第五步：查看 Network 选项卡（高级）

如果日志看起来正确，问题可能在后端验证。

1. **打开 Network 选项卡**
   - F12 → Network

2. **清空网络日志**
   - 刷新页面或清除日志

3. **再次尝试创建**

4. **找到 POST 请求**
   - 查找 `POST shipping-operations`
   - 状态应该是 `400`

5. **查看 Request Payload**
   - 点击这个请求
   - 转到 "Request" 或 "Payload" 选项卡
   - 查看实际发送的 JSON

6. **查看 Response**
   - 转到 "Response" 选项卡
   - 后端应该返回详细的错误信息
   - 例如：`"Quantity unit must be either MT or BBL"`

---

## 第六步：报告错误

当您看到错误时，请告诉我：

### 必需的信息：

1. **浏览器控制台的完整日志输出**
   ```
   === SHIPPING OPERATION CREATE REQUEST ===
   Request Payload: {
     ...
   }
   Planned Quantity Type: ...
   Load Port ETA: ...
   Discharge Port ETA: ...
   Contract ID: ...
   =========================================
   ```

2. **Network 选项卡中的响应**
   ```
   {
     "message": "One or more validation errors occurred.",
     "errors": {
       ...
     }
   }
   ```

3. **您填写的表单值**
   ```
   Vessel Name: ...
   Contract ID: ...
   等等
   ```

---

## 常见错误信息和解决方案

### 错误 1：Quantity unit must be either MT or BBL
```
"One or more validation errors occurred."
"Quantity unit must be either MT or BBL"
```
**原因：** 发送的单位不是 MT 或 BBL
**解决方案：** 确保下拉框只显示 MT 和 BBL，检查是否已重启前端

### 错误 2：IMO number must be 7 digits
```
"IMO number must be 7 digits"
```
**原因：** 如果填写 IMO，必须是 7 位数字
**解决方案：** 输入 7 位数字或留空

### 错误 3：Contract ID is required / not found
```
"Contract ID is required"
或
"Contract with ID ... not found"
```
**原因：** 合同不存在或没有填写
**解决方案：** 确保使用有效的合同 ID

### 错误 4：Discharge port ETA must be after load port ETA
```
"Discharge port ETA must be after load port ETA"
```
**原因：** 卸港日期早于或等于装港日期
**解决方案：** 确保卸港日期晚于装港日期

---

## 重启检查清单

在调试之前，确保已经完成以下步骤：

- [ ] 停止前端应用 (Ctrl+C)
- [ ] 清空浏览器缓存 (Ctrl+Shift+Delete)
- [ ] 重启前端应用 (`npm run dev`)
- [ ] 等待编译完成（看到 "dev server running at"）
- [ ] 刷新浏览器页面
- [ ] 打开浏览器控制台 (F12)
- [ ] 清空控制台日志 (Ctrl+L)

---

## 逐步调试流程图

```
1. 重启前端
   ↓
2. 打开浏览器控制台
   ↓
3. 清空日志
   ↓
4. 尝试创建 Shipping Operation
   ↓
5. 检查日志输出
   ↓
   ├→ 看到 "SHIPPING OPERATION CREATE REQUEST" 日志？
   │  ├→ 是 → 查看字段类型和值
   │  │   ├→ 所有值都正确？
   │  │   │  ├→ 是 → 转到步骤 6（查看 Network）
   │  │   │  └→ 否 → 找出哪个值不对
   │  └→ 否 → 前端没有重启成功（重新开始）
   ↓
6. 查看 Network 选项卡
   ↓
7. 看到 Response 中的错误消息
   ↓
8. 告诉我具体的错误信息
```

---

## 最可能的原因

根据多次尝试，我怀疑问题可能是：

1. **前端没有重启** → 代码修改没有生效
2. **Contract ID 格式** → 可能需要是 UUID 而不是字符串
3. **日期格式** → 可能有其他格式问题
4. **字段序列化** → JSON 序列化可能有问题

---

## 我需要您的帮助

请按以下步骤操作，然后告诉我看到的日志和错误：

1. 重启前端
2. 打开控制台
3. 尝试创建
4. **复制并粘贴这两个内容：**
   - 浏览器控制台的日志输出
   - Network 选项卡的响应内容

这样我就能准确定位问题并修复它。

---

**版本：** v2.6.9+
**状态：** 调试中
**下一步：** 等待您的日志信息
