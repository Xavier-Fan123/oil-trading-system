# Oil Trading Dashboard Frontend

一个现代化的React前端应用，用于展示石油交易仪表盘数据。

## 🎯 功能特性

### 核心仪表盘组件 (5个)
- **OverviewCard** - 投资组合概览 (总仓位、日度P&L、VaR、未实现损益等)
- **TradingMetrics** - 交易指标 (交易量、频率、产品分布、对手方集中度)
- **PerformanceChart** - 绩效分析 (月度P&L趋势、夏普比率、最大回撤等)
- **MarketInsights** - 市场洞察 (基准价格、波动率分析、相关性矩阵)
- **OperationalStatus** - 运营状态 (合同状态、船运跟踪、风险警报)

### 数据可视化组件 (3个)
- **PnLChart** - P&L趋势图 (日度/累计/未实现P&L + 交易量)
- **VaRChart** - 风险价值分析 (VaR 95%/99%、预期损失、集中度风险)
- **PositionChart** - 仓位分布图 (产品暴露度饼图 + P&L贡献条形图)

## 🛠️ 技术栈

- **React 18** + TypeScript - 现代化前端框架
- **Material-UI (MUI)** - 企业级UI组件库
- **Recharts** - 数据可视化图表库
- **React Query** - 数据获取和缓存管理
- **Axios** - HTTP客户端
- **Vite** - 快速构建工具

## 🚀 快速开始

### 先决条件
- Node.js 18+ 
- npm 或 yarn
- 后端API服务运行在 http://localhost:5000

### 安装和运行

1. **安装依赖**
   ```bash
   npm install
   ```

2. **启动开发服务器**
   ```bash
   npm run dev
   ```

3. **或者使用批处理文件 (Windows)**
   ```bash
   start-frontend.bat
   ```

4. **打开浏览器访问**
   ```
   http://localhost:3000
   ```

### 构建生产版本
```bash
npm run build
npm run preview
```

## 📊 仪表盘布局

```
┌─────────────────────────────────────────────┐
│              Header (Logo + Time)            │
├───────────────┬─────────────┬───────────────┤
│   总仓位      │   日度P&L    │    VaR 95%   │
│   $45.2M     │   +$125K     │    $2.1M     │
├───────────────┴─────────────┴───────────────┤
│            P&L 趋势图 (30天)                 │
├───────────────────┬─────────────────────────┤
│   仓位分布饼图    │    交易指标表格         │
├───────────────────┼─────────────────────────┤
│   市场洞察卡片    │    操作状态面板         │
└───────────────────┴─────────────────────────┘
```

## 🔧 配置

### 环境变量
在 `.env` 文件中配置API端点：
```
VITE_API_URL=http://localhost:5000
```

### API集成
应用连接到以下后端端点：
- `GET /api/dashboard/overview` - 投资组合概览
- `GET /api/dashboard/trading-metrics` - 交易指标
- `GET /api/dashboard/performance-analytics` - 绩效分析
- `GET /api/dashboard/market-insights` - 市场洞察
- `GET /api/dashboard/operational-status` - 运营状态

### 自动刷新
- 概览数据：每15秒刷新
- 交易指标：每30秒刷新
- 绩效分析：每60秒刷新
- 市场洞察：每20秒刷新
- 运营状态：每15秒刷新

## 🎨 UI/UX特性

- **深色主题** - 专业交易界面风格
- **响应式设计** - 支持桌面和移动设备
- **实时数据** - 自动刷新和缓存管理
- **错误处理** - 优雅的错误状态和重试机制
- **加载状态** - 骨架屏和进度指示器
- **颜色编码** - 盈利绿色/亏损红色的直观显示

## 📁 项目结构

```
src/
├── components/
│   ├── Dashboard/          # 5个核心仪表盘组件
│   │   ├── OverviewCard.tsx
│   │   ├── TradingMetrics.tsx
│   │   ├── PerformanceChart.tsx
│   │   ├── MarketInsights.tsx
│   │   └── OperationalStatus.tsx
│   ├── Charts/             # 3个数据可视化组件
│   │   ├── PnLChart.tsx
│   │   ├── VaRChart.tsx
│   │   └── PositionChart.tsx
│   └── Common/             # 通用组件
│       ├── KPICard.tsx
│       └── AlertBanner.tsx
├── services/               # API服务层
│   └── api.ts
├── hooks/                  # React Query hooks
│   └── useDashboard.ts
├── pages/                  # 页面组件
│   └── Dashboard.tsx
├── types/                  # TypeScript类型定义
│   └── index.ts
├── theme/                  # Material-UI主题
│   └── theme.ts
└── main.tsx               # 应用入口点
```

## 🔍 开发工具

### 类型检查
```bash
npm run type-check
```

### 代码检查
```bash
npm run lint
```

### 构建检查
```bash
npm run build
```

## 🚦 状态管理

- **React Query** - 服务器状态管理和缓存
- **Material-UI Theme** - UI状态和主题管理
- **Local State** - 组件内部状态使用React hooks

## 📈 性能优化

- **代码分割** - 使用动态导入
- **数据缓存** - React Query智能缓存
- **图表优化** - Recharts按需渲染
- **Bundle优化** - Vite快速构建

## 🔗 与后端集成

确保后端API服务正在运行：
```bash
cd ../src/OilTrading.Api
dotnet run
```

前端会自动连接到 `http://localhost:5000` 的API服务。

## 📝 已知问题

1. **开发模式下的CORS** - 通过Vite代理解决
2. **数据模拟** - 部分图表使用模拟数据，等待完整API实现
3. **实时更新** - 目前使用轮询，未来可升级为WebSocket

## 🎯 未来扩展

- [ ] WebSocket实时数据流
- [ ] 更多交互式图表
- [ ] 自定义仪表盘配置
- [ ] 移动端应用
- [ ] 数据导出功能
- [ ] 用户权限管理

---

**开发完成日期**: 2025年1月  
**版本**: 1.0.0  
**技术栈版本**: React 18 + Vite 5 + MUI 5