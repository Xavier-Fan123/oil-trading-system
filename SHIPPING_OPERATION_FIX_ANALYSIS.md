# Shipping Operation 400 Bad Request 错误 - 深度分析与修复报告

## 问题概述
在创建新的 Shipping Operation 时，收到 HTTP 400 (Bad Request) 错误，错误消息显示 "One or more validation errors occurred"。

```
Failed to load resource: the server responded with a status of 400 (Bad Request)
URL: http://localhost:5000/api/shipping-operations
```

---

## 根本原因分析

### 🔴 **严重问题 #1：前后端 DTO 字段名称完全不匹配**

**后端期望的字段结构** (`CreateShippingOperationDto.cs`):
```csharp
public class CreateShippingOperationDto
{
    public Guid ContractId { get; set; }              // ✅
    public string VesselName { get; set; }            // ✅
    public string? ImoNumber { get; set; }            // ✅
    public decimal PlannedQuantity { get; set; }      // ✅
    public string PlannedQuantityUnit { get; set; }   // ✅ (重要！)
    public DateTime? LaycanStart { get; set; }        // ✅
    public DateTime? LaycanEnd { get; set; }          // ✅
    public string? Notes { get; set; }                // ✅
}
```

**前端错误地发送的字段** (修复前):
```typescript
const createData: CreateShippingOperationDto = {
  contractId: formData.contractId,                    // ❌ 应该是 contractId (camelCase)
  vesselName: formData.vesselName,                    // ❌ 应该是 vesselName (camelCase)
  imoNumber: formData.imoNumber || undefined,         // ❌ 应该是 imoNumber (camelCase)
  plannedQuantity: Number(formData.plannedQuantity),  // ❌ 应该是 plannedQuantity (camelCase)
  quantityUnit: formData.quantityUnit,                // ❌❌❌ 后端期望 plannedQuantityUnit！
  loadPort: formData.loadPort,                        // ❌❌❌ 后端完全没有这个字段！
  dischargePort: formData.dischargePort,              // ❌❌❌ 后端完全没有这个字段！
  loadPortETA: formData.loadPortETA || undefined,     // ❌ 应该是 laycanStart
  dischargePortETA: formData.dischargePortETA || undefined, // ❌ 应该是 laycanEnd
  charterParty: formData.charterParty || undefined,   // ❌❌❌ 后端完全没有这个字段！
  notes: formData.notes || undefined,                 // ✅ 这个是对的
};
```

### 🔴 **严重问题 #2：ASP.NET Core 默认拒绝额外字段**
当 JSON 请求包含后端不期望的字段时，ASP.NET Core 的 Model Binding 会：
1. 检测到额外的字段（loadPort, dischargePort, charterParty）
2. 返回 400 Bad Request 错误
3. 拒绝整个请求

这是 ASP.NET Core 的安全机制，用于防止不预期的字段被接受。

### 🔴 **严重问题 #3：前端 DTO 定义包含不必要的字段**
前端的 `types/shipping.ts` 中的 `CreateShippingOperationDto` 定义包含：
- `quantityUnit` (后端期望 `plannedQuantityUnit`)
- `loadPort` (后端完全没有)
- `dischargePort` (后端完全没有)
- `loadPortETA` (应该是 `laycanStart`)
- `dischargePortETA` (应该是 `laycanEnd`)

### 🔴 **严重问题 #4：日期时间格式转换**
前端表单中的日期时间字段：
- 格式：`YYYY-MM-DDTHH:mm` (HTML datetime-local 格式)
- 需要转换为 ISO 8601 格式：`YYYY-MM-DDTHH:mm:ssZ` (JavaScript 的 `toISOString()`)
- 后端期望 `DateTime` 对象

---

## 修复方案

### ✅ **修复 #1：更新前端 DTO 定义**
**文件**: `frontend/src/types/shipping.ts`

```typescript
// 修复前后的对比
export interface CreateShippingOperationDto {
  contractId: string;
  vesselName: string;
  imoNumber?: string;
  plannedQuantity: number;
  plannedQuantityUnit: string;        // ✅ 修正：quantityUnit → plannedQuantityUnit
  laycanStart?: string;               // ✅ 修正：loadPortETA → laycanStart
  laycanEnd?: string;                 // ✅ 修正：dischargePortETA → laycanEnd
  notes?: string;
  // ✅ 删除：loadPort, dischargePort, charterParty（后端不需要）
}

export interface UpdateShippingOperationDto {
  vesselName?: string;
  imoNumber?: string;
  plannedQuantity?: number;
  plannedQuantityUnit?: string;       // ✅ 修正：quantityUnit → plannedQuantityUnit
  actualQuantity?: number;
  actualQuantityUnit?: string;
  laycanStart?: string;               // ✅ 修正：loadPortETA → laycanStart
  laycanEnd?: string;                 // ✅ 修正：dischargePortETA → laycanEnd
  norDate?: string;
  billOfLadingDate?: string;
  dischargeDate?: string;
  notes?: string;
  // ✅ 删除：loadPort, dischargePort, charterParty, loadPortATA, loadPortATD, dischargePortATA, dischargePortATD, demurrageDays
}
```

### ✅ **修复 #2：更新表单 handleSubmit 方法**
**文件**: `frontend/src/components/Shipping/ShippingOperationForm.tsx` (第 162-204 行)

```typescript
const handleSubmit = async () => {
  if (!validateForm()) {
    return;
  }

  try {
    if (isEditing && initialData?.id) {
      const updateData: UpdateShippingOperationDto = {
        vesselName: formData.vesselName || undefined,
        imoNumber: formData.imoNumber || undefined,
        plannedQuantity: Number(formData.plannedQuantity) || undefined,
        plannedQuantityUnit: formData.quantityUnit || undefined,  // ✅ 字段名修正
        laycanStart: formData.loadPortETA ? new Date(formData.loadPortETA).toISOString() : undefined,  // ✅ 字段名修正 + 格式转换
        laycanEnd: formData.dischargePortETA ? new Date(formData.dischargePortETA).toISOString() : undefined,  // ✅ 字段名修正 + 格式转换
        notes: formData.notes || undefined,
      };

      await updateMutation.mutateAsync({
        id: initialData.id,
        operation: updateData
      });
    } else {
      const createData: CreateShippingOperationDto = {
        contractId: formData.contractId,
        vesselName: formData.vesselName,
        imoNumber: formData.imoNumber || undefined,
        plannedQuantity: Number(formData.plannedQuantity),
        plannedQuantityUnit: formData.quantityUnit,  // ✅ 字段名修正
        laycanStart: formData.loadPortETA ? new Date(formData.loadPortETA).toISOString() : undefined,  // ✅ 字段名修正 + 格式转换
        laycanEnd: formData.dischargePortETA ? new Date(formData.dischargePortETA).toISOString() : undefined,  // ✅ 字段名修正 + 格式转换
        notes: formData.notes || undefined,
      };

      await createMutation.mutateAsync(createData);
    }

    onSubmit();
    onClose();
  } catch (error) {
    console.error('Form submission error:', error);
  }
};
```

### ✅ **修复 #3：更新表单验证规则**
**文件**: `frontend/src/components/Shipping/ShippingOperationForm.tsx` (第 133-152 行)

```typescript
const validateForm = (): boolean => {
  const errors: Record<string, string> = {};

  if (!formData.vesselName.trim()) {
    errors.vesselName = 'Vessel name is required';
  }

  if (!formData.contractId.trim()) {
    errors.contractId = 'Contract ID is required';
  }

  if (!formData.plannedQuantity.trim()) {
    errors.plannedQuantity = 'Planned quantity is required';
  } else if (isNaN(Number(formData.plannedQuantity)) || Number(formData.plannedQuantity) <= 0) {
    errors.plannedQuantity = 'Planned quantity must be a positive number';
  }

  // ✅ 删除：loadPort 和 dischargePort 验证（这些字段现在是可选的）

  setValidationErrors(errors);
  return Object.keys(errors).length === 0;
};
```

### ✅ **修复 #4：更新 UI 字段标签**
**文件**: `frontend/src/components/Shipping/ShippingOperationForm.tsx` (第 305-343 行)

```typescript
// Load Port - 改为可选字段
<Grid item xs={12} sm={6}>
  <Autocomplete
    freeSolo
    options={COMMON_PORTS}
    value={formData.loadPort}
    onChange={(_, value) => handleInputChange('loadPort', value || '')}
    onInputChange={(_, value) => handleInputChange('loadPort', value)}
    renderInput={(params) => (
      <TextField
        {...params}
        fullWidth
        label="Load Port"                              // ✅ 移除 * (不再是必需)
        helperText="Optional - Port information"       // ✅ 添加说明
        disabled={isSubmitting}
      />
    )}
    disabled={isSubmitting}
  />
</Grid>

// Discharge Port - 改为可选字段
<Grid item xs={12} sm={6}>
  <Autocomplete
    freeSolo
    options={COMMON_PORTS}
    value={formData.dischargePort}
    onChange={(_, value) => handleInputChange('dischargePort', value || '')}
    onInputChange={(_, value) => handleInputChange('dischargePort', value)}
    renderInput={(params) => (
      <TextField
        {...params}
        fullWidth
        label="Discharge Port"                         // ✅ 移除 * (不再是必需)
        helperText="Optional - Port information"       // ✅ 添加说明
        disabled={isSubmitting}
      />
    )}
    disabled={isSubmitting}
  />
</Grid>
```

---

## 前后端对应关系表

| 前端表单字段 | 前端 DTO 字段 | 后端 DTO 字段 | 数据类型 | 必需 | 备注 |
|------------|-------------|-------------|--------|------|------|
| vesselName | vesselName | VesselName | string | ✅ | 船舶名称 |
| imoNumber | imoNumber | ImoNumber | string | ❌ | 国际海事组织号 |
| contractId | contractId | ContractId | Guid | ✅ | 合同 ID |
| plannedQuantity | plannedQuantity | PlannedQuantity | decimal | ✅ | 计划数量 |
| quantityUnit | plannedQuantityUnit | PlannedQuantityUnit | string | ✅ | 数量单位 - 重要修正！ |
| loadPortETA | laycanStart | LaycanStart | DateTime | ❌ | 装货港 ETA → Laycan Start |
| dischargePortETA | laycanEnd | LaycanEnd | DateTime | ❌ | 卸货港 ETA → Laycan End |
| loadPort | ❌ 已删除 | ❌ 不存在 | - | - | 已移除（UI 展示用） |
| dischargePort | ❌ 已删除 | ❌ 不存在 | - | - | 已移除（UI 展示用） |
| charterParty | ❌ 已删除 | ❌ 不存在 | - | - | 已移除（UI 展示用） |
| notes | notes | Notes | string | ❌ | 备注 |

---

## 测试步骤

1. **清空浏览器缓存**
   ```
   Ctrl+Shift+Delete
   ```

2. **重启前端开发服务器**
   ```
   npm run dev
   ```

3. **创建新的 Shipping Operation**
   - 选择一个有效的合同 ID（例如：ITGR-2025-DEL-S2071）
   - 输入 Vessel Name（例如：YUE YOU 906）
   - 输入 Planned Quantity（例如：370）
   - 选择 Unit（例如：Metric Tons）
   - 点击 Create

4. **验证成功**
   - 不应该看到 400 Bad Request 错误
   - Shipping Operation 应该成功创建
   - 列表中应该显示新的 Shipping Operation

---

## 关键要点总结

1. **字段名称必须完全匹配** - ASP.NET Core 的 Model Binding 对字段名称大小写敏感（使用 camelCase）
2. **不要发送后端不期望的字段** - 这会导致 400 Bad Request 错误
3. **日期时间必须转换为 ISO 8601 格式** - 使用 `toISOString()`
4. **前端 DTO 定义必须与后端 DTO 保持同步** - 这是系统整体稳定性的关键

---

## 修复验证清单

- ✅ 更新了 `frontend/src/types/shipping.ts` 中的 CreateShippingOperationDto
- ✅ 更新了 `frontend/src/types/shipping.ts` 中的 UpdateShippingOperationDto
- ✅ 修正了 `ShippingOperationForm.tsx` 中的 handleSubmit 方法
- ✅ 更新了表单验证规则（移除了不必要的验证）
- ✅ 更新了 UI 字段标签（移除了 * 号，添加了 "Optional" 标记）
- ✅ 前端编译通过，无 TypeScript 错误

**状态：完成 ✅**

---

**修复日期**: 2025-10-29
**修复版本**: v2.6.7
**影响范围**: Shipping Operations 模块
