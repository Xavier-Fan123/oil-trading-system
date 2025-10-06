# Oil Trading System - Startup Guide v2.4.2

## 🚀 Quick Start (Recommended)

### One-Click Startup
```batch
# Double-click this file to start everything:
START.bat
```

### Manual Startup (Advanced)
```batch
# Use this if you need step-by-step control:
MANUAL-STARTUP.bat
```

## 📋 System Requirements

- **Redis**: Required for caching and performance
- **Node.js**: For frontend development (use explicit paths on Windows)
- **.NET 9**: For backend API
- **Administrator Privileges**: Required for npm operations on Windows

## 🔧 Startup Sequence

1. **Redis Cache Server** (localhost:6379) - MUST start first
2. **Backend API Server** (localhost:5000) - Depends on Redis
3. **Frontend React App** (auto-selects port) - Requires Administrator

## 🌐 Access Points

- **Frontend Application**: http://localhost:3000 (or auto-selected port)
- **Backend API**: http://localhost:5000
- **API Health Check**: http://localhost:5000/health
- **API Documentation**: http://localhost:5000/swagger
- **Redis Cache**: localhost:6379

## ⚠️ Common Issues & Solutions

### Frontend npm Issues
```bash
# Problem: "Could not determine Node.js install directory"
# Solution: Use explicit paths
"D:\npm.cmd" run dev
```

### Permission Issues  
```bash
# Problem: esbuild installation fails
# Solution: Run as Administrator
# Open Command Prompt as Administrator, then:
cd "C:\Users\itg\Desktop\X\frontend"
npm install
```

### WebSocket Connection Issues
```text
# Problem: WebSocket connection failures
# Solution: System now uses polling instead of WebSocket
# This is automatically configured in vite.config.ts
```

### Port Conflicts
```text
# Problem: Port already in use
# Solution: System auto-selects next available port
# Frontend: 3000 → 3001 → 3002 → etc.
```

## 🏥 Health Checks

```bash
# Test backend health
curl http://localhost:5000/health

# Check active ports
netstat -an | findstr :3000
netstat -an | findstr :5000
netstat -an | findstr :6379
```

## 🛑 Emergency Procedures

### Kill All Processes
```bash
# Stop all Node.js processes
taskkill /f /im node.exe

# Stop Redis
taskkill /f /im redis-server.exe

# Stop .NET processes
taskkill /f /im dotnet.exe
```

### Restart Redis
```bash
taskkill /f /im redis-server.exe
powershell -Command "Start-Process -FilePath 'C:\Users\itg\Desktop\X\redis\redis-server.exe' -ArgumentList 'C:\Users\itg\Desktop\X\redis\redis.windows.conf' -WindowStyle Hidden"
```

## 📊 Performance Notes

- **Without Redis**: API responses 20+ seconds ❌
- **With Redis**: API responses <200ms ✅  
- **Cache Hit Rate**: >90% for dashboard operations
- **Frontend Build Time**: ~584ms with optimized Vite config

---

**Last Updated**: August 22, 2025  
**Version**: 2.4.1 (WebSocket Issues Resolved)