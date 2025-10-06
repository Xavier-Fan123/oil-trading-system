#!/bin/bash
# Cleanup Monitoring Data Script for Oil Trading System
# This script manages data retention and cleanup for monitoring components

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
PROJECT_NAME="${PROJECT_NAME:-oiltrading}"
RETENTION_DAYS="${RETENTION_DAYS:-30}"
LOG_RETENTION_DAYS="${LOG_RETENTION_DAYS:-7}"
PROMETHEUS_RETENTION="${PROMETHEUS_RETENTION:-15d}"
DRY_RUN="${DRY_RUN:-false}"
FORCE="${FORCE:-false}"

# Helper functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_section() {
    echo -e "\n${CYAN}=== $1 ===${NC}"
}

# Get human readable size
get_size() {
    local path=$1
    if [[ -d "$path" ]]; then
        du -sh "$path" 2>/dev/null | cut -f1
    else
        echo "0B"
    fi
}

# Confirm action
confirm_action() {
    local message=$1
    if [[ "$FORCE" == "true" ]]; then
        return 0
    fi
    
    echo -e "${YELLOW}$message${NC}"
    read -p "Are you sure? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log_info "Operation cancelled by user"
        return 1
    fi
    return 0
}

# Execute command with dry run support
execute_cmd() {
    local cmd="$1"
    local description="$2"
    
    if [[ "$DRY_RUN" == "true" ]]; then
        log_info "[DRY RUN] Would execute: $cmd"
        log_info "[DRY RUN] Description: $description"
    else
        log_info "Executing: $description"
        eval "$cmd"
    fi
}

# Cleanup application logs
cleanup_application_logs() {
    log_section "Application Logs Cleanup"
    
    local log_dir="./logs"
    if [[ ! -d "$log_dir" ]]; then
        log_warning "Log directory $log_dir not found"
        return
    fi
    
    local size_before=$(get_size "$log_dir")
    log_info "Log directory size before cleanup: $size_before"
    
    # Find old log files
    local old_logs=$(find "$log_dir" -name "*.txt" -type f -mtime +$LOG_RETENTION_DAYS 2>/dev/null | wc -l)
    log_info "Found $old_logs log files older than $LOG_RETENTION_DAYS days"
    
    if [[ $old_logs -gt 0 ]]; then
        if confirm_action "Delete $old_logs old log files?"; then
            execute_cmd "find \"$log_dir\" -name \"*.txt\" -type f -mtime +$LOG_RETENTION_DAYS -delete" \
                       "Removing old application log files"
        fi
    fi
    
    # Compress recent logs
    local uncompressed_logs=$(find "$log_dir" -name "*.txt" -type f -mtime +1 ! -name "*.gz" 2>/dev/null | wc -l)
    if [[ $uncompressed_logs -gt 0 ]]; then
        log_info "Found $uncompressed_logs uncompressed log files"
        if confirm_action "Compress uncompressed log files?"; then
            execute_cmd "find \"$log_dir\" -name \"*.txt\" -type f -mtime +1 ! -name \"*.gz\" -exec gzip {} +" \
                       "Compressing old log files"
        fi
    fi
    
    local size_after=$(get_size "$log_dir")
    log_info "Log directory size after cleanup: $size_after"
}

# Cleanup Prometheus data
cleanup_prometheus_data() {
    log_section "Prometheus Data Cleanup"
    
    # Get Prometheus data size
    local prom_data_size=$(docker-compose -p $PROJECT_NAME exec -T prometheus du -sh /prometheus 2>/dev/null | cut -f1 || echo "Unknown")
    log_info "Prometheus data size: $prom_data_size"
    
    # Check retention settings
    local current_retention=$(docker-compose -p $PROJECT_NAME exec -T prometheus /bin/prometheus --help 2>/dev/null | grep -A1 "storage.tsdb.retention.time" | tail -1 | awk '{print $1}' || echo "Unknown")
    log_info "Current Prometheus retention: $current_retention"
    
    # Cleanup old data beyond retention
    if confirm_action "Trigger Prometheus data cleanup (removes data older than $PROMETHEUS_RETENTION)?"; then
        execute_cmd "docker-compose -p $PROJECT_NAME exec -T prometheus promtool tsdb create-blocks-from wal /prometheus/wal /prometheus" \
                   "Converting WAL to blocks"
        
        execute_cmd "docker-compose -p $PROJECT_NAME restart prometheus" \
                   "Restarting Prometheus to apply retention policies"
    fi
}

# Cleanup Elasticsearch data
cleanup_elasticsearch_data() {
    log_section "Elasticsearch Data Cleanup"
    
    # Get cluster info
    local cluster_health=$(curl -s http://localhost:9200/_cluster/health 2>/dev/null | jq -r '.status' 2>/dev/null || echo "unknown")
    local cluster_size=$(curl -s http://localhost:9200/_cat/allocation?h=disk.used 2>/dev/null | awk '{sum+=$1} END {print sum"MB"}' || echo "Unknown")
    
    log_info "Elasticsearch cluster health: $cluster_health"
    log_info "Elasticsearch cluster size: $cluster_size"
    
    # List indices older than retention period
    local old_indices=$(curl -s "http://localhost:9200/_cat/indices/oil-trading-*?h=index,creation.date.string" 2>/dev/null | \
        awk -v retention_days="$RETENTION_DAYS" '
        {
            cmd = "date -d \"" $2 "\" +%s 2>/dev/null || date -d \"" $2 " 00:00:00\" +%s"
            cmd | getline index_timestamp
            close(cmd)
            
            cmd = "date -d \"" retention_days " days ago\" +%s"
            cmd | getline cutoff_timestamp
            close(cmd)
            
            if (index_timestamp < cutoff_timestamp) {
                print $1
            }
        }' 2>/dev/null || echo "")
    
    if [[ -n "$old_indices" ]]; then
        local index_count=$(echo "$old_indices" | wc -l)
        log_info "Found $index_count indices older than $RETENTION_DAYS days"
        
        if confirm_action "Delete old Elasticsearch indices?"; then
            for index in $old_indices; do
                execute_cmd "curl -X DELETE \"http://localhost:9200/$index\"" \
                           "Deleting index: $index"
            done
        fi
    else
        log_info "No old indices found for cleanup"
    fi
    
    # Force merge segments
    if confirm_action "Force merge Elasticsearch segments for better performance?"; then
        execute_cmd "curl -X POST \"http://localhost:9200/oil-trading-*/_forcemerge?max_num_segments=1\"" \
                   "Force merging Elasticsearch segments"
    fi
}

# Cleanup Loki data
cleanup_loki_data() {
    log_section "Loki Data Cleanup"
    
    # Get Loki data size
    local loki_size=$(docker-compose -p $PROJECT_NAME exec -T loki du -sh /loki 2>/dev/null | cut -f1 || echo "Unknown")
    log_info "Loki data size: $loki_size"
    
    # Compact Loki data
    if confirm_action "Compact Loki data to improve performance?"; then
        execute_cmd "docker-compose -p $PROJECT_NAME exec -T loki /usr/bin/loki -config.file=/etc/loki/local-config.yaml -target=compactor -compactor.working-directory=/loki/compactor" \
                   "Running Loki compactor"
    fi
}

# Cleanup Docker resources
cleanup_docker_resources() {
    log_section "Docker Resources Cleanup"
    
    # Get Docker disk usage
    local docker_usage=$(docker system df --format "table {{.Type}}\t{{.TotalCount}}\t{{.Size}}\t{{.Reclaimable}}" 2>/dev/null || echo "Unknown")
    log_info "Docker disk usage:"
    echo "$docker_usage"
    
    # Cleanup unused containers
    local unused_containers=$(docker ps -aq --filter "status=exited" | wc -l)
    if [[ $unused_containers -gt 0 ]]; then
        log_info "Found $unused_containers stopped containers"
        if confirm_action "Remove stopped containers?"; then
            execute_cmd "docker container prune -f" \
                       "Removing stopped containers"
        fi
    fi
    
    # Cleanup unused images
    local dangling_images=$(docker images -f "dangling=true" -q | wc -l)
    if [[ $dangling_images -gt 0 ]]; then
        log_info "Found $dangling_images dangling images"
        if confirm_action "Remove dangling images?"; then
            execute_cmd "docker image prune -f" \
                       "Removing dangling images"
        fi
    fi
    
    # Cleanup unused volumes
    local unused_volumes=$(docker volume ls -f "dangling=true" -q | wc -l)
    if [[ $unused_volumes -gt 0 ]]; then
        log_info "Found $unused_volumes unused volumes"
        if confirm_action "Remove unused volumes? (WARNING: This may delete data!)"; then
            execute_cmd "docker volume prune -f" \
                       "Removing unused volumes"
        fi
    fi
    
    # Cleanup build cache
    if confirm_action "Clear Docker build cache?"; then
        execute_cmd "docker builder prune -f" \
                   "Clearing Docker build cache"
    fi
}

# Cleanup monitoring configurations
cleanup_monitoring_configs() {
    log_section "Monitoring Configuration Cleanup"
    
    # Backup current configurations
    local backup_dir="./backups/monitoring-$(date +%Y%m%d_%H%M%S)"
    
    if confirm_action "Create backup of current monitoring configurations?"; then
        execute_cmd "mkdir -p \"$backup_dir\"" \
                   "Creating backup directory"
        
        execute_cmd "cp -r ./monitoring \"$backup_dir/\"" \
                   "Backing up monitoring configurations"
        
        log_success "Backup created at: $backup_dir"
    fi
    
    # Cleanup temporary files
    local temp_files=$(find ./monitoring -name "*.tmp" -o -name "*.bak" -o -name "*~" 2>/dev/null | wc -l)
    if [[ $temp_files -gt 0 ]]; then
        log_info "Found $temp_files temporary files"
        if confirm_action "Remove temporary configuration files?"; then
            execute_cmd "find ./monitoring -name \"*.tmp\" -o -name \"*.bak\" -o -name \"*~\" -delete" \
                       "Removing temporary files"
        fi
    fi
}

# Generate cleanup report
generate_cleanup_report() {
    log_section "Cleanup Report"
    
    local report_file="./logs/cleanup-report-$(date +%Y%m%d_%H%M%S).txt"
    
    {
        echo "Oil Trading System - Monitoring Data Cleanup Report"
        echo "=================================================="
        echo "Date: $(date)"
        echo "Project: $PROJECT_NAME"
        echo "Retention Days: $RETENTION_DAYS"
        echo "Log Retention Days: $LOG_RETENTION_DAYS"
        echo "Prometheus Retention: $PROMETHEUS_RETENTION"
        echo "Dry Run: $DRY_RUN"
        echo
        
        echo "Disk Usage After Cleanup:"
        echo "========================"
        echo "Application Logs: $(get_size ./logs)"
        echo "Monitoring Config: $(get_size ./monitoring)"
        
        if docker-compose -p $PROJECT_NAME ps prometheus >/dev/null 2>&1; then
            echo "Prometheus Data: $(docker-compose -p $PROJECT_NAME exec -T prometheus du -sh /prometheus 2>/dev/null | cut -f1 || echo 'Unknown')"
        fi
        
        if docker-compose -p $PROJECT_NAME ps loki >/dev/null 2>&1; then
            echo "Loki Data: $(docker-compose -p $PROJECT_NAME exec -T loki du -sh /loki 2>/dev/null | cut -f1 || echo 'Unknown')"
        fi
        
        echo
        echo "Docker Resources:"
        docker system df 2>/dev/null || echo "Docker system df failed"
        
        echo
        echo "Next Recommended Cleanup: $(date -d '+7 days')"
        
    } > "$report_file"
    
    log_success "Cleanup report generated: $report_file"
}

# Main execution
main() {
    echo "Oil Trading System - Monitoring Data Cleanup"
    echo "===========================================" 
    echo "Timestamp: $(date)"
    echo "Project: $PROJECT_NAME"
    echo "Retention: $RETENTION_DAYS days"
    echo "Log Retention: $LOG_RETENTION_DAYS days"
    [[ "$DRY_RUN" == "true" ]] && echo "Mode: DRY RUN"
    [[ "$FORCE" == "true" ]] && echo "Mode: FORCE (no confirmations)"
    
    # Disk space check before cleanup
    log_section "Pre-Cleanup System Status"
    local disk_usage=$(df / | awk 'NR==2 {print $5}' | sed 's/%//')
    log_info "Current disk usage: ${disk_usage}%"
    
    if [[ $disk_usage -gt 90 ]]; then
        log_warning "Disk usage is critically high! Cleanup is recommended."
    elif [[ $disk_usage -gt 80 ]]; then
        log_warning "Disk usage is high. Cleanup recommended."
    fi
    
    # Run cleanup operations
    cleanup_application_logs
    cleanup_prometheus_data
    cleanup_elasticsearch_data
    cleanup_loki_data
    cleanup_docker_resources
    cleanup_monitoring_configs
    
    # Generate report
    generate_cleanup_report
    
    # Post-cleanup status
    log_section "Post-Cleanup System Status"
    local disk_usage_after=$(df / | awk 'NR==2 {print $5}' | sed 's/%//')
    local disk_freed=$((disk_usage - disk_usage_after))
    
    log_info "Disk usage after cleanup: ${disk_usage_after}%"
    if [[ $disk_freed -gt 0 ]]; then
        log_success "Freed ${disk_freed}% disk space"
    fi
    
    log_success "🧹 Monitoring data cleanup completed!"
    
    echo
    echo "Recommendations:"
    echo "1. Schedule this script to run weekly via cron"
    echo "2. Monitor disk usage regularly"
    echo "3. Adjust retention periods based on compliance requirements"
    echo "4. Review cleanup report for optimization opportunities"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -d|--dry-run)
            DRY_RUN=true
            shift
            ;;
        -f|--force)
            FORCE=true
            shift
            ;;
        -r|--retention)
            RETENTION_DAYS="$2"
            shift 2
            ;;
        -l|--log-retention)
            LOG_RETENTION_DAYS="$2"
            shift 2
            ;;
        -p|--project)
            PROJECT_NAME="$2"
            shift 2
            ;;
        --prometheus-retention)
            PROMETHEUS_RETENTION="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  -d, --dry-run              Show what would be done without executing"
            echo "  -f, --force                Skip confirmations (use with caution)"
            echo "  -r, --retention DAYS       Data retention in days (default: 30)"
            echo "  -l, --log-retention DAYS   Log retention in days (default: 7)"
            echo "  -p, --project NAME         Docker project name (default: oiltrading)"
            echo "  --prometheus-retention     Prometheus retention period (default: 15d)"
            echo "  -h, --help                 Show this help"
            echo
            echo "Examples:"
            echo "  $0 --dry-run               # Show what would be cleaned up"
            echo "  $0 --retention 60          # Keep data for 60 days"
            echo "  $0 --force --retention 7   # Aggressive cleanup with no prompts"
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Safety checks
if [[ "$RETENTION_DAYS" -lt 1 ]]; then
    log_error "Retention days must be at least 1"
    exit 1
fi

if [[ "$LOG_RETENTION_DAYS" -lt 1 ]]; then
    log_error "Log retention days must be at least 1"
    exit 1
fi

# Create logs directory if it doesn't exist
mkdir -p ./logs

# Run main function
main "$@"