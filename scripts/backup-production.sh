#!/bin/bash

# 🔄 石油交易系统生产数据备份脚本
# 自动备份PostgreSQL数据库和重要配置文件

set -e

# 配置变量
BACKUP_DIR="/app/backups"
DATE=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=30

# 从环境变量获取数据库配置
DB_HOST=${DB_HOST:-localhost}
DB_NAME=${DB_NAME:-oiltrading_prod}
DB_USER=${DB_USER:-oil_trading_admin}
DB_PASSWORD=${DB_PASSWORD}

# 日志函数
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

# 创建备份目录
create_backup_directory() {
    mkdir -p "${BACKUP_DIR}/database"
    mkdir -p "${BACKUP_DIR}/config"
    mkdir -p "${BACKUP_DIR}/logs"
}

# 数据库备份
backup_database() {
    log "开始数据库备份..."
    
    local backup_file="${BACKUP_DIR}/database/oiltrading_backup_${DATE}.sql"
    
    # 使用pg_dump进行备份
    PGPASSWORD="${DB_PASSWORD}" pg_dump \
        -h "${DB_HOST}" \
        -U "${DB_USER}" \
        -d "${DB_NAME}" \
        --verbose \
        --clean \
        --create \
        --if-exists \
        --format=custom \
        --file="${backup_file}.custom"
    
    # 同时创建SQL格式备份（便于人工查看）
    PGPASSWORD="${DB_PASSWORD}" pg_dump \
        -h "${DB_HOST}" \
        -U "${DB_USER}" \
        -d "${DB_NAME}" \
        --verbose \
        --clean \
        --create \
        --if-exists \
        > "${backup_file}"
    
    # 压缩备份文件
    gzip "${backup_file}"
    
    log "数据库备份完成: ${backup_file}.gz"
    log "自定义格式备份: ${backup_file}.custom"
}

# 配置文件备份
backup_configs() {
    log "开始配置文件备份..."
    
    local config_backup_dir="${BACKUP_DIR}/config/config_${DATE}"
    mkdir -p "${config_backup_dir}"
    
    # 备份重要配置文件
    [ -f ".env" ] && cp ".env" "${config_backup_dir}/"
    [ -f "appsettings.Production.json" ] && cp "appsettings.Production.json" "${config_backup_dir}/"
    [ -f "docker-compose.production.yml" ] && cp "docker-compose.production.yml" "${config_backup_dir}/"
    
    # 备份Nginx配置
    if [ -d "nginx" ]; then
        cp -r "nginx" "${config_backup_dir}/"
    fi
    
    # 备份监控配置
    if [ -d "monitoring" ]; then
        cp -r "monitoring" "${config_backup_dir}/"
    fi
    
    # 打包配置文件
    tar -czf "${config_backup_dir}.tar.gz" -C "${BACKUP_DIR}/config" "config_${DATE}"
    rm -rf "${config_backup_dir}"
    
    log "配置文件备份完成: ${config_backup_dir}.tar.gz"
}

# 应用日志备份
backup_logs() {
    log "开始日志文件备份..."
    
    local log_backup_dir="${BACKUP_DIR}/logs/logs_${DATE}"
    mkdir -p "${log_backup_dir}"
    
    # 备份应用日志
    if [ -d "logs" ]; then
        cp -r logs/* "${log_backup_dir}/" 2>/dev/null || true
    fi
    
    # 如果有日志文件，则打包
    if [ "$(ls -A ${log_backup_dir})" ]; then
        tar -czf "${log_backup_dir}.tar.gz" -C "${BACKUP_DIR}/logs" "logs_${DATE}"
        rm -rf "${log_backup_dir}"
        log "日志备份完成: ${log_backup_dir}.tar.gz"
    else
        rm -rf "${log_backup_dir}"
        log "没有找到日志文件，跳过日志备份"
    fi
}

# 清理旧备份
cleanup_old_backups() {
    log "清理${RETENTION_DAYS}天前的备份..."
    
    # 清理数据库备份
    find "${BACKUP_DIR}/database" -name "*.sql.gz" -mtime +${RETENTION_DAYS} -delete 2>/dev/null || true
    find "${BACKUP_DIR}/database" -name "*.custom" -mtime +${RETENTION_DAYS} -delete 2>/dev/null || true
    
    # 清理配置文件备份
    find "${BACKUP_DIR}/config" -name "*.tar.gz" -mtime +${RETENTION_DAYS} -delete 2>/dev/null || true
    
    # 清理日志备份
    find "${BACKUP_DIR}/logs" -name "*.tar.gz" -mtime +${RETENTION_DAYS} -delete 2>/dev/null || true
    
    log "旧备份清理完成"
}

# 备份验证
verify_backup() {
    log "验证备份文件完整性..."
    
    local backup_file="${BACKUP_DIR}/database/oiltrading_backup_${DATE}.sql.gz"
    local custom_backup="${BACKUP_DIR}/database/oiltrading_backup_${DATE}.sql.custom"
    
    # 检查备份文件是否存在且大小大于0
    if [ -f "${backup_file}" ] && [ -s "${backup_file}" ]; then
        log "✓ SQL备份文件验证成功"
    else
        log "✗ SQL备份文件验证失败"
        return 1
    fi
    
    if [ -f "${custom_backup}" ] && [ -s "${custom_backup}" ]; then
        log "✓ 自定义格式备份文件验证成功"
    else
        log "✗ 自定义格式备份文件验证失败"
        return 1
    fi
    
    # 尝试解压缩测试
    if gzip -t "${backup_file}" 2>/dev/null; then
        log "✓ 备份文件压缩完整性验证成功"
    else
        log "✗ 备份文件压缩完整性验证失败"
        return 1
    fi
}

# 发送备份报告（可选）
send_backup_report() {
    local status=$1
    local backup_size=$(du -sh "${BACKUP_DIR}/database/oiltrading_backup_${DATE}.sql.gz" 2>/dev/null | cut -f1)
    
    log "备份报告:"
    log "- 备份时间: ${DATE}"
    log "- 备份状态: ${status}"
    log "- 备份大小: ${backup_size:-未知}"
    log "- 备份位置: ${BACKUP_DIR}"
    
    # 这里可以添加邮件或Slack通知逻辑
    # 例如: curl -X POST -H 'Content-type: application/json' --data "{\"text\":\"Backup ${status}: ${backup_size}\"}" YOUR_SLACK_WEBHOOK_URL
}

# 主函数
main() {
    log "开始执行生产环境备份..."
    
    create_backup_directory
    
    # 执行备份
    backup_database
    backup_configs
    backup_logs
    
    # 验证备份
    if verify_backup; then
        log "备份验证成功"
        cleanup_old_backups
        send_backup_report "成功"
        log "✅ 备份任务完成"
    else
        log "备份验证失败"
        send_backup_report "失败"
        log "❌ 备份任务失败"
        exit 1
    fi
}

# 错误处理
trap 'log "备份过程中发生错误"; send_backup_report "错误"; exit 1' ERR

# 执行主函数
main "$@"