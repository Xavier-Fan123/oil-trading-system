#!/bin/bash
# Performance Monitoring Script for Oil Trading System
# This script continuously monitors system performance and generates alerts

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
API_BASE_URL="${API_BASE_URL:-http://localhost:8080}"
MONITOR_INTERVAL="${MONITOR_INTERVAL:-60}"
ALERT_THRESHOLD_CPU="${ALERT_THRESHOLD_CPU:-80}"
ALERT_THRESHOLD_MEMORY="${ALERT_THRESHOLD_MEMORY:-80}"
ALERT_THRESHOLD_RESPONSE_TIME="${ALERT_THRESHOLD_RESPONSE_TIME:-2000}"
ALERT_THRESHOLD_ERROR_RATE="${ALERT_THRESHOLD_ERROR_RATE:-5}"
LOG_FILE="${LOG_FILE:-./logs/performance-monitor.log}"
ALERT_FILE="${LOG_FILE%.*}-alerts.log"
REPORT_INTERVAL="${REPORT_INTERVAL:-3600}" # Generate report every hour
EMAIL_ALERTS="${EMAIL_ALERTS:-false}"
SLACK_WEBHOOK="${SLACK_WEBHOOK:-}"

# Global variables
declare -A previous_metrics
alert_count=0
last_report_time=0

# Helper functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1" | tee -a "$LOG_FILE"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" | tee -a "$LOG_FILE"
}

log_alert() {
    local message="$1"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo -e "${RED}[ALERT]${NC} $message" | tee -a "$LOG_FILE"
    echo "[$timestamp] ALERT: $message" >> "$ALERT_FILE"
    ((alert_count++))
    
    # Send notifications
    send_alert_notification "$message"
}

log_section() {
    echo -e "\n${CYAN}=== $1 ===${NC}" | tee -a "$LOG_FILE"
}

# Send alert notifications
send_alert_notification() {
    local message="$1"
    
    # Slack notification
    if [[ -n "$SLACK_WEBHOOK" ]]; then
        curl -X POST -H 'Content-type: application/json' \
            --data "{\"text\":\"🚨 Oil Trading Alert: $message\"}" \
            "$SLACK_WEBHOOK" >/dev/null 2>&1 || true
    fi
    
    # Email notification (if configured)
    if [[ "$EMAIL_ALERTS" == "true" ]]; then
        echo "Alert: $message" | mail -s "Oil Trading Performance Alert" admin@oiltrading.com >/dev/null 2>&1 || true
    fi
}

# Get API metrics
get_api_metrics() {
    local metrics_url="$API_BASE_URL/api/metrics/system"
    local response=$(curl -s -w "%{http_code}" "$metrics_url" 2>/dev/null)
    local http_code="${response: -3}"
    local body="${response%???}"
    
    if [[ "$http_code" == "200" ]]; then
        echo "$body"
        return 0
    else
        log_error "Failed to get API metrics (HTTP $http_code)"
        return 1
    fi
}

# Get performance report
get_performance_report() {
    local report_url="$API_BASE_URL/api/performance/metrics/summary"
    local response=$(curl -s -w "%{http_code}" "$report_url" 2>/dev/null)
    local http_code="${response: -3}"
    local body="${response%???}"
    
    if [[ "$http_code" == "200" ]]; then
        echo "$body"
        return 0
    else
        log_error "Failed to get performance report (HTTP $http_code)"
        return 1
    fi
}

# Check API health
check_api_health() {
    local start_time=$(date +%s%3N)
    local health_url="$API_BASE_URL/health"
    local response=$(curl -s -w "%{http_code}" "$health_url" 2>/dev/null)
    local end_time=$(date +%s%3N)
    local response_time=$((end_time - start_time))
    local http_code="${response: -3}"
    
    if [[ "$http_code" == "200" ]]; then
        log_success "API health check passed (${response_time}ms)"
        
        # Check response time threshold
        if [[ $response_time -gt $ALERT_THRESHOLD_RESPONSE_TIME ]]; then
            log_alert "High API response time: ${response_time}ms (threshold: ${ALERT_THRESHOLD_RESPONSE_TIME}ms)"
        fi
        
        return 0
    else
        log_alert "API health check failed (HTTP $http_code)"
        return 1
    fi
}

# Monitor system resources
monitor_system_resources() {
    log_section "System Resources"
    
    # CPU usage
    local cpu_usage=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | sed 's/%us,//' 2>/dev/null || echo "0")
    cpu_usage=${cpu_usage%.*} # Remove decimal part
    
    if [[ $cpu_usage -gt $ALERT_THRESHOLD_CPU ]]; then
        log_alert "High CPU usage: ${cpu_usage}% (threshold: ${ALERT_THRESHOLD_CPU}%)"
    else
        log_info "CPU usage: ${cpu_usage}%"
    fi
    
    # Memory usage
    local memory_info=$(free | awk 'NR==2{printf "%.0f %.0f", $3*100/$2, $3/1024/1024}')
    local memory_percent=$(echo $memory_info | awk '{print $1}')
    local memory_mb=$(echo $memory_info | awk '{print $2}')
    
    if [[ $memory_percent -gt $ALERT_THRESHOLD_MEMORY ]]; then
        log_alert "High memory usage: ${memory_percent}% (${memory_mb}MB) (threshold: ${ALERT_THRESHOLD_MEMORY}%)"
    else
        log_info "Memory usage: ${memory_percent}% (${memory_mb}MB)"
    fi
    
    # Disk usage
    local disk_usage=$(df / | awk 'NR==2 {print $5}' | sed 's/%//')
    if [[ $disk_usage -gt 90 ]]; then
        log_alert "High disk usage: ${disk_usage}%"
    elif [[ $disk_usage -gt 80 ]]; then
        log_warning "Elevated disk usage: ${disk_usage}%"
    else
        log_info "Disk usage: ${disk_usage}%"
    fi
    
    # Load average
    local load_avg=$(uptime | awk -F'load average:' '{print $2}' | cut -d, -f1 | xargs)
    local cpu_cores=$(nproc)
    local load_ratio=$(echo "$load_avg $cpu_cores" | awk '{printf "%.1f", $1/$2*100}')
    
    log_info "Load average: $load_avg (${load_ratio}% of $cpu_cores cores)"
    
    if (( $(echo "$load_ratio > 90" | bc -l) )); then
        log_alert "High system load: ${load_ratio}%"
    fi
}

# Monitor application performance
monitor_application_performance() {
    log_section "Application Performance"
    
    local performance_data=$(get_performance_report)
    if [[ $? -eq 0 && -n "$performance_data" ]]; then
        # Extract metrics using jq if available
        if command -v jq >/dev/null 2>&1; then
            local response_time=$(echo "$performance_data" | jq -r '.application.p95_response_time_ms // 0' 2>/dev/null)
            local error_rate=$(echo "$performance_data" | jq -r '.application.error_rate_4xx_percent // 0' 2>/dev/null)
            local throughput=$(echo "$performance_data" | jq -r '.application.throughput_requests_per_second // 0' 2>/dev/null)
            
            # Response time check
            if (( $(echo "$response_time > $ALERT_THRESHOLD_RESPONSE_TIME" | bc -l) )); then
                log_alert "High P95 response time: ${response_time}ms"
            else
                log_info "P95 response time: ${response_time}ms"
            fi
            
            # Error rate check
            if (( $(echo "$error_rate > $ALERT_THRESHOLD_ERROR_RATE" | bc -l) )); then
                log_alert "High error rate: ${error_rate}%"
            else
                log_info "Error rate: ${error_rate}%"
            fi
            
            log_info "Throughput: ${throughput} RPS"
        else
            log_warning "jq not available - skipping detailed performance analysis"
        fi
    else
        log_warning "Could not retrieve performance data"
    fi
}

# Monitor database performance
monitor_database_performance() {
    log_section "Database Performance"
    
    # Check if we can connect to the database via API
    local db_health_url="$API_BASE_URL/health/detailed"
    local db_response=$(curl -s "$db_health_url" 2>/dev/null)
    
    if [[ $? -eq 0 && -n "$db_response" ]]; then
        if command -v jq >/dev/null 2>&1; then
            local db_status=$(echo "$db_response" | jq -r '.systemChecks[] | select(.name=="database") | .status' 2>/dev/null)
            
            if [[ "$db_status" == "Healthy" ]]; then
                log_success "Database status: Healthy"
            else
                log_alert "Database status: $db_status"
            fi
        fi
    else
        log_warning "Could not retrieve database health status"
    fi
}

# Monitor business metrics
monitor_business_metrics() {
    log_section "Business Metrics"
    
    local business_url="$API_BASE_URL/api/metrics/business"
    local business_data=$(curl -s "$business_url" 2>/dev/null)
    
    if [[ $? -eq 0 && -n "$business_data" ]]; then
        if command -v jq >/dev/null 2>&1; then
            local active_contracts=$(echo "$business_data" | jq -r '.metrics.activeContracts // 0' 2>/dev/null)
            local expiring_contracts=$(echo "$business_data" | jq -r '.metrics.contractsExpiringToday // 0' 2>/dev/null)
            local trading_partners=$(echo "$business_data" | jq -r '.metrics.totalTradingPartners // 0' 2>/dev/null)
            
            log_info "Active contracts: $active_contracts"
            log_info "Contracts expiring today: $expiring_contracts"
            log_info "Trading partners: $trading_partners"
            
            # Business alerts
            if [[ $expiring_contracts -gt 50 ]]; then
                log_alert "High number of contracts expiring today: $expiring_contracts"
            fi
        fi
    else
        log_warning "Could not retrieve business metrics"
    fi
}

# Generate performance report
generate_performance_report() {
    local report_file="./reports/performance-report-$(date +%Y%m%d_%H%M%S).json"
    mkdir -p "./reports"
    
    log_info "Generating performance report..."
    
    local report_url="$API_BASE_URL/api/performance/report"
    local report_data=$(curl -s "$report_url" 2>/dev/null)
    
    if [[ $? -eq 0 && -n "$report_data" ]]; then
        echo "$report_data" > "$report_file"
        log_success "Performance report saved: $report_file"
        
        # Generate summary
        if command -v jq >/dev/null 2>&1; then
            local avg_response_time=$(echo "$report_data" | jq -r '.ApplicationMetrics.AverageResponseTime' 2>/dev/null)
            local error_rate=$(echo "$report_data" | jq -r '.ApplicationMetrics.ErrorRate' 2>/dev/null)
            local throughput=$(echo "$report_data" | jq -r '.ApplicationMetrics.ThroughputRps' 2>/dev/null)
            
            log_info "Report Summary - Avg Response: ${avg_response_time}ms, Error Rate: ${error_rate}%, Throughput: ${throughput} RPS"
        fi
    else
        log_error "Failed to generate performance report"
    fi
}

# Main monitoring loop
run_monitoring_cycle() {
    local cycle_start=$(date +%s)
    log_section "Performance Monitoring Cycle - $(date)"
    
    # Reset alert count for this cycle
    local cycle_alerts=0
    
    # Run monitoring checks
    check_api_health || ((cycle_alerts++))
    monitor_system_resources
    monitor_application_performance
    monitor_database_performance
    monitor_business_metrics
    
    # Generate hourly report
    local current_time=$(date +%s)
    if [[ $((current_time - last_report_time)) -ge $REPORT_INTERVAL ]]; then
        generate_performance_report
        last_report_time=$current_time
    fi
    
    local cycle_duration=$(($(date +%s) - cycle_start))
    log_info "Monitoring cycle completed in ${cycle_duration}s (Alerts this cycle: $cycle_alerts)"
    
    # Summary
    if [[ $cycle_alerts -eq 0 ]]; then
        log_success "✅ All systems operating normally"
    else
        log_warning "⚠️  $cycle_alerts alert(s) detected this cycle"
    fi
}

# Cleanup function
cleanup() {
    log_info "Performance monitoring stopped"
    exit 0
}

# Signal handlers
trap cleanup SIGINT SIGTERM

# Main execution
main() {
    echo "Oil Trading System - Performance Monitor"
    echo "======================================="
    echo "API Base URL: $API_BASE_URL"
    echo "Monitor Interval: ${MONITOR_INTERVAL}s"
    echo "Log File: $LOG_FILE"
    echo "Alert File: $ALERT_FILE"
    echo "Started: $(date)"
    echo
    
    # Create log directory
    mkdir -p "$(dirname "$LOG_FILE")"
    mkdir -p "$(dirname "$ALERT_FILE")"
    
    # Initialize log file
    echo "Performance monitoring started at $(date)" > "$LOG_FILE"
    
    # Check dependencies
    if ! command -v curl >/dev/null 2>&1; then
        log_error "curl is required but not installed"
        exit 1
    fi
    
    if ! command -v bc >/dev/null 2>&1; then
        log_warning "bc not found - some calculations may be limited"
    fi
    
    if ! command -v jq >/dev/null 2>&1; then
        log_warning "jq not found - JSON parsing will be limited"
    fi
    
    # Initial API connectivity check
    log_info "Checking initial API connectivity..."
    if ! check_api_health; then
        log_error "Cannot connect to Oil Trading API at $API_BASE_URL"
        log_error "Please ensure the API is running and accessible"
        exit 1
    fi
    
    log_success "Performance monitoring initialized successfully"
    
    # Main monitoring loop
    while true; do
        run_monitoring_cycle
        
        # Display summary
        echo
        echo "Next check in ${MONITOR_INTERVAL}s... (Press Ctrl+C to stop)"
        echo "Total alerts: $alert_count"
        
        sleep "$MONITOR_INTERVAL"
    done
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --api-url)
            API_BASE_URL="$2"
            shift 2
            ;;
        --interval)
            MONITOR_INTERVAL="$2"
            shift 2
            ;;
        --log-file)
            LOG_FILE="$2"
            ALERT_FILE="${LOG_FILE%.*}-alerts.log"
            shift 2
            ;;
        --email-alerts)
            EMAIL_ALERTS=true
            shift
            ;;
        --slack-webhook)
            SLACK_WEBHOOK="$2"
            shift 2
            ;;
        --cpu-threshold)
            ALERT_THRESHOLD_CPU="$2"
            shift 2
            ;;
        --memory-threshold)
            ALERT_THRESHOLD_MEMORY="$2"
            shift 2
            ;;
        --response-time-threshold)
            ALERT_THRESHOLD_RESPONSE_TIME="$2"
            shift 2
            ;;
        --error-rate-threshold)
            ALERT_THRESHOLD_ERROR_RATE="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  --api-url URL              API base URL (default: http://localhost:8080)"
            echo "  --interval SECONDS         Monitor interval (default: 60)"
            echo "  --log-file FILE            Log file path (default: ./logs/performance-monitor.log)"
            echo "  --email-alerts             Enable email alerts"
            echo "  --slack-webhook URL        Slack webhook URL for notifications"
            echo "  --cpu-threshold PERCENT    CPU usage alert threshold (default: 80)"
            echo "  --memory-threshold PERCENT Memory usage alert threshold (default: 80)"
            echo "  --response-time-threshold MS Response time alert threshold (default: 2000)"
            echo "  --error-rate-threshold PERCENT Error rate alert threshold (default: 5)"
            echo "  -h, --help                 Show this help"
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Run main function
main "$@"