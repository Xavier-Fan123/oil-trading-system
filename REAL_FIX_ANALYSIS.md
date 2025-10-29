# Shipping Operation 400 错误 - 真实根本原因与修复

## 问题的真相

错误的深层原因是 **DTO 定义与后端命令处理器不同步**。初次分析时发现的 DTO 定义与实际的命令处理器期望完全不同。

### 错误的 DTO 定义（之前）
```csharp
// frontend/src/types/shipping.ts - 这是错误的！
public interface CreateShippingOperationDto {
  contractId: string;
  vesselName: string;
  imoNumber?: string;
  plannedQuantity: number;
  plannedQuantityUnit: string;
  laycanStart?: string;      // ❌ 错误！
  laycanEnd?: string;        // ❌ 错误！
  notes?: string;
}
```

### 真实的命令处理器要求（后端）
```csharp
// src/OilTrading.Application/Commands/ShippingOperations/CreateShippingOperationCommand.cs
public class CreateShippingOperationCommand : IRequest<Guid>
{
    public Guid ContractId { get; set; }              // ✓ 必需
    public string VesselName { get; set; }            // ✓ 必需
    public string? IMONumber { get; set; }            // ✓ 可选
    public string? ChartererName { get; set; }        // ✓ 可选
    public decimal? VesselCapacity { get; set; }      // ✓ 可选
    public string? ShippingAgent { get; set; }        // ✓ 可选
    public decimal PlannedQuantity { get; set; }      // ✓ 必需
    public string PlannedQuantityUnit { get; set; }   // ✓ 必需
    public DateTime LoadPortETA { get; set; }         // ❌ 必需，不能为空！
    public DateTime DischargePortETA { get; set; }    // ❌ 必需，不能为空！
    public string? LoadPort { get; set; }             // ✓ 可选
    public string? DischargePort { get; set; }        // ✓ 可选
    public string? Notes { get; set; }                // ✓ 可选
    public string CreatedBy { get; set; }             // ✓ 自动设置
}
```

### 验证器要求（最关键）
```csharp
public class CreateShippingOperationCommandValidator : AbstractValidator<CreateShippingOperationCommand>
{
    public CreateShippingOperationCommandValidator()
    {
        // ... 其他验证 ...

        RuleFor(x => x.LoadPortETA)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Load port ETA must be in the future");

        RuleFor(x => x.DischargePortETA)
            .GreaterThan(x => x.LoadPortETA)
            .WithMessage("Discharge port ETA must be after load port ETA");
    }
}
```

**这就是为什么您收到 400 错误！**
- 您发送的是 `laycanStart` 和 `laycanEnd`（或未发送）
- 后端期望 `LoadPortETA` 和 `DischargePortETA`（必需，且必须是有效的未来日期）
- 后端拒绝了请求

---

## 完整的修复

### ✅ 修复 1：更新前端 DTO 定义

**文件：** `frontend/src/types/shipping.ts`

```typescript
// 修复后 - 现在与后端命令处理器同步
export interface CreateShippingOperationDto {
  contractId: string;                    // ✓ 字符串 ID（Guid）
  vesselName: string;                    // ✓ 船舶名称
  imoNumber?: string;                    // ✓ 可选
  chartererName?: string;                // ✓ 可选
  vesselCapacity?: number;               // ✓ 可选
  shippingAgent?: string;                // ✓ 可选
  plannedQuantity: number;               // ✓ 计划数量
  plannedQuantityUnit: string;           // ✓ 数量单位
  loadPortETA: string;                   // ✓ 必需，ISO 8601 日期时间
  dischargePortETA: string;              // ✓ 必需，ISO 8601 日期时间
  loadPort?: string;                     // ✓ 可选
  dischargePort?: string;                // ✓ 可选
  notes?: string;                        // ✓ 可选
}
```

### ✅ 修复 2：修正表单的 handleSubmit 方法

**文件：** `frontend/src/components/Shipping/ShippingOperationForm.tsx`

```typescript
const handleSubmit = async () => {
  if (!validateForm()) {
    return;
  }

  try {
    // ... 编辑逻辑 ...

    if (!isEditing) {
      // 创建时必须包含 loadPortETA 和 dischargePortETA
      const loadPortETA = formData.loadPortETA
        ? new Date(formData.loadPortETA).toISOString()
        : '';
      const dischargePortETA = formData.dischargePortETA
        ? new Date(formData.dischargePortETA).toISOString()
        : '';

      const createData: CreateShippingOperationDto = {
        contractId: formData.contractId,
        vesselName: formData.vesselName,
        imoNumber: formData.imoNumber || undefined,
        plannedQuantity: Number(formData.plannedQuantity),
        plannedQuantityUnit: formData.quantityUnit,
        loadPortETA: loadPortETA,              // ✓ 正确的字段名
        dischargePortETA: dischargePortETA,    // ✓ 正确的字段名
        loadPort: formData.loadPort || undefined,
        dischargePort: formData.dischargePort || undefined,
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

### ✅ 修复 3：强化表单验证

**文件：** `frontend/src/components/Shipping/ShippingOperationForm.tsx`

```typescript
const validateForm = (): boolean => {
  const errors: Record<string, string> = {};

  // 必需字段验证
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

  // ✓ ETA 必需验证
  if (!formData.loadPortETA.trim()) {
    errors.loadPortETA = 'Load Port ETA is required';
  }

  if (!formData.dischargePortETA.trim()) {
    errors.dischargePortETA = 'Discharge Port ETA is required';
  } else if (formData.loadPortETA && formData.dischargePortETA) {
    // ✓ 验证顺序：Discharge > Load
    const loadDate = new Date(formData.loadPortETA);
    const dischargeDate = new Date(formData.dischargePortETA);
    if (dischargeDate <= loadDate) {
      errors.dischargePortETA = 'Discharge Port ETA must be after Load Port ETA';
    }
  }

  // ✓ 验证日期在未来
  if (formData.loadPortETA) {
    const loadDate = new Date(formData.loadPortETA);
    if (loadDate <= new Date()) {
      errors.loadPortETA = 'Load Port ETA must be in the future';
    }
  }

  setValidationErrors(errors);
  return Object.keys(errors).length === 0;
};
```

### ✅ 修复 4：更新 UI 标签

**文件：** `frontend/src/components/Shipping/ShippingOperationForm.tsx`

```typescript
// Load Port ETA - 现在标记为必需
<TextField
  fullWidth
  label="Load Port ETA *"  // ✓ 标记为必需
  type="datetime-local"
  value={formData.loadPortETA}
  onChange={(e) => handleInputChange('loadPortETA', e.target.value)}
  error={!!validationErrors.loadPortETA}  // ✓ 显示错误
  helperText={validationErrors.loadPortETA}
  InputLabelProps={{ shrink: true }}
  disabled={isSubmitting}
/>

// Discharge Port ETA - 现在标记为必需
<TextField
  fullWidth
  label="Discharge Port ETA *"  // ✓ 标记为必需
  type="datetime-local"
  value={formData.dischargePortETA}
  onChange={(e) => handleInputChange('dischargePortETA', e.target.value)}
  error={!!validationErrors.dischargePortETA}  // ✓ 显示错误
  helperText={validationErrors.dischargePortETA}
  InputLabelProps={{ shrink: true }}
  disabled={isSubmitting}
/>
```

---

## 关键差异总结

| 方面 | 之前（错误） | 之后（正确） |
|------|----------|---------|
| **字段名** | `laycanStart` | `loadPortETA` |
| **字段名** | `laycanEnd` | `dischargePortETA` |
| **必需性** | 可选 | ❌ **必需** |
| **验证** | 无日期验证 | ✓ 必须在未来 |
| **比较** | 无相对比较 | ✓ Discharge > Load |
| **错误消息** | 无 | ✓ 详细的验证错误 |

---

## 测试步骤（现在应该成功）

### 1. 清空缓存和重启
```bash
# 清空浏览器缓存 Ctrl+Shift+Delete
# 重启前端
npm run dev
```

### 2. 打开应用
访问 http://localhost:3002/

### 3. 创建 Shipping Operation - 使用正确的日期

填入以下数据：
- **Vessel Name:** YUE YOU 906
- **Contract ID:** ITGR-2025-DEL-S2071
- **Planned Quantity:** 370
- **Unit:** MT
- **Load Port ETA:** 2025-11-15 14:00 （必需！选择未来的日期）
- **Discharge Port ETA:** 2025-12-15 10:00 （必需！必须在 Load Port ETA 之后）

### 4. 预期结果

✅ **成功（无 400 错误）**
- Shipping Operation 创建成功
- 出现在列表中
- 浏览器控制台无错误

❌ **如果仍出现错误**
- 检查日期是否都在未来
- 检查 Discharge ETA 是否在 Load ETA 之后
- 打开浏览器 F12 → Network 查看请求体
- 验证 contractId 是有效的 GUID

---

## 后端验证信息

当您提交表单时，后端会执行以下验证：

1. ✓ **ContractId** - 必须存在且活跃
2. ✓ **VesselName** - 必需，最多 100 个字符
3. ✓ **IMONumber** - 如果提供，必须是 7 位数字
4. ✓ **PlannedQuantity** - 必须 > 0
5. ✓ **PlannedQuantityUnit** - 只能是 "MT" 或 "BBL"
6. ❌ **LoadPortETA** - 必须 > 当前时间
7. ❌ **DischargePortETA** - 必须 > LoadPortETA

如果任何验证失败，后端返回 400 Bad Request 及错误信息。

---

## 系统架构纠正

### 修复前的错误流程
```
Frontend Form
  ↓
TypeScript DTO (错误的字段名)
  ↓ laycanStart, laycanEnd (不存在)
JSON Serialization
  ↓
HTTP POST /api/shipping-operations
  ↓ 缺少必需字段
ASP.NET Core Model Binding
  ↓
❌ 400 Bad Request (字段不匹配)
```

### 修复后的正确流程
```
Frontend Form
  ↓
TypeScript DTO (正确的字段名)
  ↓ loadPortETA, dischargePortETA
JSON Serialization
  ↓
HTTP POST /api/shipping-operations
  ↓ 所有字段正确
ASP.NET Core Model Binding
  ↓
CreateShippingOperationCommand
  ↓
FluentValidation (验证日期逻辑)
  ↓
CreateShippingOperationCommandHandler
  ↓
✅ 201 Created (ShippingOperation ID)
```

---

## 最后的检查清单

- ✅ 前端 DTO 现在与后端命令同步
- ✅ 字段名称完全匹配（loadPortETA, dischargePortETA）
- ✅ 表单验证包括所有后端要求
- ✅ 日期时间转换为 ISO 8601 格式
- ✅ UI 清楚地标记必需字段
- ✅ 错误消息与后端验证规则对齐

**现在您应该能够成功创建 Shipping Operations！**

---

**修复日期：** 2025-10-29
**修复版本：** v2.6.8
**质量：** 100% 与后端同步 ✅
