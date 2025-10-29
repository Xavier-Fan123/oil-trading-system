# 快速开始 - Oil Trading System v2.6.8

## 🚀 一键启动所有服务

### 最简单的方式

**直接双击运行：**
```
START.bat
```

或者更完整的启动脚本：
```
START-PRODUCTION.bat
```

---

## 📋 启动脚本说明

### START.bat（推荐用于开发）
快速启动所有服务的基础脚本。会自动：
1. ✅ 启动 Redis 缓存服务器（port 6379）
2. ✅ 启动后端 API 服务器（port 5000）
3. ✅ 启动前端 React 应用（port 3002+）
4. ✅ 打开浏览器访问应用

**耗时：** 约 15-20 秒

### START-PRODUCTION.bat（完整版本）
功能更全面的启动脚本，包含详细日志和说明。

---

## 🔧 手动启动（如果脚本不工作）

### 终端 1：启动 Redis
```powershell
cd "C:\Users\itg\Desktop\X\redis"
redis-server.exe redis.windows.conf
```

### 终端 2：启动后端 API
```powershell
cd "C:\Users\itg\Desktop\X\src\OilTrading.Api"
dotnet run
```

### 终端 3：启动前端（以管理员身份）
```powershell
cd "C:\Users\itg\Desktop\X\frontend"
"C:\Users\itg\nodejs\npm.cmd" run dev
```

---

## 🌐 应用访问地址

启动完成后，访问以下地址：

| 服务 | URL | 说明 |
|-----|-----|------|
| **Frontend** | http://localhost:3002 | React 应用主界面 |
| **Backend API** | http://localhost:5000 | REST API 服务器 |
| **API Health** | http://localhost:5000/health | 健康检查端点 |
| **API Documentation** | http://localhost:5000/swagger | Swagger API 文档 |
| **Redis** | localhost:6379 | 缓存服务器 |

---

## ✅ 启动完成标志

当您看到这些日志时，说明系统已启动完成：

**Redis:**
```
Ready to accept connections
```

**Backend:**
```
Listening on: http://localhost:5000
```

**Frontend:**
```
VITE v... dev server running at:
http://localhost:3002
```

---

## 🧪 快速验证系统

### 1. 验证后端
打开浏览器访问：
```
http://localhost:5000/health
```

应该返回 200 OK 状态

### 2. 验证前端
打开浏览器访问：
```
http://localhost:3002
```

应该看到 Oil Trading 应用界面

### 3. 验证数据库连接
在前端应用中：
- 点击左侧菜单的任何选项
- 应该能看到数据加载（可能需要等待 1-2 秒）
- 如果看到数据，说明数据库连接正常

---

## ⚠️ 常见问题

### 启动脚本不工作？

**问题：** 双击 START.bat 后什么都没有发生

**解决方案：**
1. 尝试右键 → 以管理员身份运行
2. 或者在 PowerShell 中运行：
   ```powershell
   .\START.bat
   ```

### Port 已被占用？

**问题：** 看到错误信息 "Address already in use"

**解决方案：**
```powershell
# 查看占用的进程
netstat -ano | findstr :5000    # Backend
netstat -ano | findstr :3002    # Frontend
netstat -ano | findstr :6379    # Redis

# 杀死进程（如果需要）
taskkill /pid <PID> /f
```

### npm 命令找不到？

**问题：** 看到错误 "npm: command not found"

**解决方案：**
前端启动脚本已使用完整路径：
```
"C:\Users\itg\nodejs\npm.cmd" run dev
```

不需要额外配置。

### Redis 服务不启动？

**问题：** Redis 窗口立即关闭

**解决方案：**
检查 redis.windows.conf 是否存在：
```powershell
ls C:\Users\itg\Desktop\X\redis\
```

应该看到 `redis-server.exe` 和 `redis.windows.conf`

---

## 🛑 停止服务

当您想停止应用时：

1. **关闭前端窗口** - 在前端 CMD 窗口中按 `Ctrl+C`
2. **关闭后端窗口** - 在后端 CMD 窗口中按 `Ctrl+C`
3. **关闭 Redis 窗口** - 在 Redis CMD 窗口中按 `Ctrl+C`

---

## 📊 系统要求

- Windows 10/11
- .NET 9.0 SDK
- Node.js (已安装在 C:\Users\itg\nodejs\)
- Redis (已包含在项目中)
- PostgreSQL 或 SQLite (内存数据库)

---

## 📝 最近的修复 (v2.6.8)

**Shipping Operation 400 错误已修复！**

修复内容：
- ✅ 修正了 Shipping Operation DTO 字段名称
- ✅ 添加了日期验证规则
- ✅ 增强了表单验证

现在您可以成功创建 Shipping Operations，只需记住：
- Load Port ETA 和 Discharge Port ETA 是必需的
- 日期必须在未来
- Discharge Port ETA 必须在 Load Port ETA 之后

详见：[FINAL_FIX_SUMMARY.md](FINAL_FIX_SUMMARY.md)

---

## 🔗 相关文档

- [CLAUDE.md](CLAUDE.md) - 项目完整文档
- [README.md](README.md) - 项目概述
- [FINAL_FIX_SUMMARY.md](FINAL_FIX_SUMMARY.md) - Shipping Operation 修复说明
- [REAL_FIX_ANALYSIS.md](REAL_FIX_ANALYSIS.md) - 技术深度分析

---

## 💡 提示

- 首次启动可能需要 20-30 秒（编译前端）
- 如果看到 npm warning，可以忽略
- 如果看到 EF Core warning，这是正常的（使用 InMemory 数据库时）
- Redis 对性能很重要，确保它正在运行

---

**祝您使用愉快！** 🎉

如有任何问题，请查看相关的文档或检查浏览器控制台错误信息。
