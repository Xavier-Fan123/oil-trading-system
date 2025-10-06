#!/bin/bash
# =============================================================================
# Oil Trading System - Health Check and Automated Recovery Script
# =============================================================================
# This script monitors the Oil Trading System health and performs
# automated recovery actions when issues are detected
# =============================================================================

set -euo pipefail

# Script metadata
SCRIPT_VERSION="1.0.0"
SCRIPT_NAME="Oil Trading System Health Check & Recovery"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT="${ENVIRONMENT:-production}"
NAMESPACE="${NAMESPACE:-oil-trading-${ENVIRONMENT}}"
CHECK_INTERVAL="${CHECK_INTERVAL:-60}"
MAX_RETRY_ATTEMPTS="${MAX_RETRY_ATTEMPTS:-3}"
ALERT_WEBHOOK="${ALERT_WEBHOOK:-}"
RECOVERY_ENABLED="${RECOVERY_ENABLED:-true}"
VERBOSE="${VERBOSE:-false}"

# Health check thresholds
CPU_THRESHOLD=80
MEMORY_THRESHOLD=85
ERROR_RATE_THRESHOLD=5
RESPONSE_TIME_THRESHOLD=2000
DISK_USAGE_THRESHOLD=85

# Logging functions
log_info() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] [INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')] [SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] [WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] [ERROR]${NC} $1"
}

log_debug() {
    if [[ "${VERBOSE}" == "true" ]]; then
        echo -e "${PURPLE}[$(date +'%Y-%m-%d %H:%M:%S')] [DEBUG]${NC} $1"
    fi
}

# Banner function
show_banner() {
    echo -e "${CYAN}"
    cat << 'EOF'
╔═══════════════════════════════════════════════════════════════════════════════╗
║                    Oil Trading System Health Monitor                          ║
║                        Automated Health & Recovery                           ║
╚═══════════════════════════════════════════════════════════════════════════════╝
EOF
    echo -e "${NC}"
    echo -e "${BLUE}Version: ${SCRIPT_VERSION}${NC}"
    echo -e "${BLUE}Environment: ${ENVIRONMENT}${NC}"
    echo -e "${BLUE}Namespace: ${NAMESPACE}${NC}"
    echo -e "${BLUE}Check Interval: ${CHECK_INTERVAL}s${NC}"
    echo
}

# Notification function
send_alert() {
    local severity="$1"
    local title="$2"
    local message="$3"
    local component="${4:-system}"
    
    if [[ -z "$ALERT_WEBHOOK" ]]; then
        log_debug "Alert webhook not configured, skipping notification"
        return 0
    fi
    
    local color="warning"
    local emoji="⚠️"
    
    case "$severity" in
        "critical")
            color="danger"
            emoji="🚨"
            ;;
        "warning")
            color="warning"
            emoji="⚠️"
            ;;
        "info")
            color="good"
            emoji="ℹ️"
            ;;
        "recovery")
            color="good"
            emoji="🔧"
            ;;
    esac
    
    local payload=$(cat << EOF
{
    "attachments": [
        {
            "color": "$color",
            "title": "$emoji $title",
            "fields": [
                {
                    "title": "Environment",
                    "value": "$ENVIRONMENT",
                    "short": true
                },
                {
                    "title": "Component",
                    "value": "$component",
                    "short": true
                },
                {
                    "title": "Severity",
                    "value": "$severity",
                    "short": true
                },
                {
                    "title": "Timestamp",
                    "value": "$(date -u +'%Y-%m-%d %H:%M:%S UTC')",
                    "short": true
                }
            ],
            "text": "$message",
            "footer": "Oil Trading System Health Monitor",
            "ts": $(date +%s)
        }
    ]
}
EOF
)
    
    curl -X POST -H 'Content-type: application/json' \
        --data "$payload" \
        --max-time 10 \
        "$ALERT_WEBHOOK" &> /dev/null || log_warning "Failed to send alert"
}

# Health check functions
check_pod_health() {
    log_info "Checking pod health..."
    
    local pods=$(kubectl get pods -n "$NAMESPACE" -l app.kubernetes.io/name=oil-trading-system --no-headers 2>/dev/null || echo "")
    
    if [[ -z "$pods" ]]; then
        log_error "No pods found in namespace $NAMESPACE"
        send_alert "critical" "No Pods Found" "No Oil Trading System pods found in namespace $NAMESPACE" "pods"
        return 1
    fi
    
    local failed_pods=0
    local total_pods=0
    
    while IFS= read -r pod_line; do
        if [[ -z "$pod_line" ]]; then continue; fi
        
        total_pods=$((total_pods + 1))
        local pod_name=$(echo "$pod_line" | awk '{print $1}')
        local pod_status=$(echo "$pod_line" | awk '{print $3}')
        local ready_status=$(echo "$pod_line" | awk '{print $2}')
        
        log_debug "Pod: $pod_name, Status: $pod_status, Ready: $ready_status"
        
        if [[ "$pod_status" != "Running" ]]; then
            failed_pods=$((failed_pods + 1))
            log_error "Pod $pod_name is not running (Status: $pod_status)"
            
            # Get pod details for troubleshooting
            local pod_events=$(kubectl describe pod "$pod_name" -n "$NAMESPACE" | grep -A 5 "Events:" || echo "No events")
            send_alert "critical" "Pod Not Running" "Pod $pod_name is in $pod_status state. Events: $pod_events" "pod"
        elif [[ "$ready_status" == "0/"* ]]; then
            failed_pods=$((failed_pods + 1))
            log_error "Pod $pod_name is not ready (Ready: $ready_status)"
            send_alert "warning" "Pod Not Ready" "Pod $pod_name is not ready: $ready_status" "pod"
        fi
    done <<< "$pods"
    
    if [[ $failed_pods -gt 0 ]]; then
        log_error "$failed_pods out of $total_pods pods are unhealthy"
        return 1
    else
        log_success "All $total_pods pods are healthy"
        return 0
    fi
}

check_service_endpoints() {
    log_info "Checking service endpoints..."
    
    local services=(
        "oil-trading-api"
        "oil-trading-frontend"
        "postgresql"
        "redis"
    )
    
    local failed_services=0
    
    for service in "${services[@]}"; do
        local endpoints=$(kubectl get endpoints "$service" -n "$NAMESPACE" -o jsonpath='{.subsets[0].addresses[0].ip}' 2>/dev/null || echo "")
        
        if [[ -z "$endpoints" ]]; then
            failed_services=$((failed_services + 1))
            log_error "Service $service has no endpoints"
            send_alert "critical" "Service Endpoint Missing" "Service $service has no available endpoints" "service"
        else
            log_debug "Service $service has endpoints: $endpoints"
        fi
    done
    
    if [[ $failed_services -gt 0 ]]; then
        log_error "$failed_services services have no endpoints"
        return 1
    else
        log_success "All services have healthy endpoints"
        return 0
    fi
}

check_resource_usage() {
    log_info "Checking resource usage..."
    
    local high_usage_pods=0
    
    # Check CPU usage
    local cpu_usage=$(kubectl top pods -n "$NAMESPACE" --no-headers 2>/dev/null || echo "")
    
    if [[ -n "$cpu_usage" ]]; then
        while IFS= read -r pod_line; do
            if [[ -z "$pod_line" ]]; then continue; fi
            
            local pod_name=$(echo "$pod_line" | awk '{print $1}')
            local cpu_value=$(echo "$pod_line" | awk '{print $2}' | sed 's/m$//')
            
            if [[ "$cpu_value" =~ ^[0-9]+$ ]] && [[ $cpu_value -gt $((CPU_THRESHOLD * 10)) ]]; then
                high_usage_pods=$((high_usage_pods + 1))
                log_warning "Pod $pod_name has high CPU usage: ${cpu_value}m"
                send_alert "warning" "High CPU Usage" "Pod $pod_name CPU usage: ${cpu_value}m" "resource"
            fi
        done <<< "$cpu_usage"
    fi
    
    # Check memory usage
    local memory_usage=$(kubectl top pods -n "$NAMESPACE" --no-headers 2>/dev/null || echo "")
    
    if [[ -n "$memory_usage" ]]; then
        while IFS= read -r pod_line; do
            if [[ -z "$pod_line" ]]; then continue; fi
            
            local pod_name=$(echo "$pod_line" | awk '{print $1}')
            local memory_value=$(echo "$pod_line" | awk '{print $3}' | sed 's/Mi$//')
            
            if [[ "$memory_value" =~ ^[0-9]+$ ]] && [[ $memory_value -gt 1000 ]]; then
                log_warning "Pod $pod_name has high memory usage: ${memory_value}Mi"
                send_alert "warning" "High Memory Usage" "Pod $pod_name memory usage: ${memory_value}Mi" "resource"
            fi
        done <<< "$memory_usage"
    fi
    
    if [[ $high_usage_pods -gt 0 ]]; then
        return 1
    else
        log_success "Resource usage is within acceptable limits"
        return 0
    fi
}

check_api_health() {
    log_info "Checking API health endpoints..."
    
    local api_pods=$(kubectl get pods -n "$NAMESPACE" -l app.kubernetes.io/component=api --no-headers | awk '{print $1}' || echo "")
    
    if [[ -z "$api_pods" ]]; then
        log_error "No API pods found"
        send_alert "critical" "API Pods Missing" "No API pods found in namespace $NAMESPACE" "api"
        return 1
    fi
    
    local failed_checks=0
    
    while IFS= read -r pod_name; do
        if [[ -z "$pod_name" ]]; then continue; fi
        
        log_debug "Checking health endpoint for pod: $pod_name"
        
        # Port forward and check health endpoint
        kubectl port-forward -n "$NAMESPACE" "pod/$pod_name" 8080:8080 >/dev/null 2>&1 &
        local port_forward_pid=$!
        sleep 2
        
        local health_check=$(curl -s -f http://localhost:8080/health --max-time 5 2>/dev/null || echo "FAILED")
        
        # Cleanup port forward
        kill $port_forward_pid >/dev/null 2>&1 || true
        
        if [[ "$health_check" == "FAILED" ]]; then
            failed_checks=$((failed_checks + 1))
            log_error "Health check failed for pod: $pod_name"
            send_alert "critical" "API Health Check Failed" "Health endpoint unreachable for pod $pod_name" "api"
        else
            log_debug "Health check passed for pod: $pod_name"
        fi
    done <<< "$api_pods"
    
    if [[ $failed_checks -gt 0 ]]; then
        log_error "$failed_checks API health checks failed"
        return 1
    else
        log_success "All API health checks passed"
        return 0
    fi
}

check_database_connectivity() {
    log_info "Checking database connectivity..."
    
    local postgres_pod=$(kubectl get pods -n "$NAMESPACE" -l app.kubernetes.io/component=database -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || echo "")
    
    if [[ -z "$postgres_pod" ]]; then
        log_error "PostgreSQL pod not found"
        send_alert "critical" "Database Pod Missing" "PostgreSQL pod not found in namespace $NAMESPACE" "database"
        return 1
    fi
    
    # Test database connection
    local db_test=$(kubectl exec -n "$NAMESPACE" "$postgres_pod" -- psql -U postgres -d OilTradingDb -c "SELECT 1;" 2>/dev/null || echo "FAILED")
    
    if [[ "$db_test" == *"FAILED"* ]]; then
        log_error "Database connectivity check failed"
        send_alert "critical" "Database Connection Failed" "Cannot connect to PostgreSQL database" "database"
        return 1
    else
        log_success "Database connectivity check passed"
        return 0
    fi
}

check_redis_connectivity() {
    log_info "Checking Redis connectivity..."
    
    local redis_pod=$(kubectl get pods -n "$NAMESPACE" -l app.kubernetes.io/component=cache -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || echo "")
    
    if [[ -z "$redis_pod" ]]; then
        log_error "Redis pod not found"
        send_alert "critical" "Redis Pod Missing" "Redis pod not found in namespace $NAMESPACE" "redis"
        return 1
    fi
    
    # Test Redis connection
    local redis_test=$(kubectl exec -n "$NAMESPACE" "$redis_pod" -- redis-cli ping 2>/dev/null || echo "FAILED")
    
    if [[ "$redis_test" != "PONG" ]]; then
        log_error "Redis connectivity check failed"
        send_alert "critical" "Redis Connection Failed" "Cannot connect to Redis cache" "redis"
        return 1
    else
        log_success "Redis connectivity check passed"
        return 0
    fi
}

# Recovery functions
restart_unhealthy_pods() {
    if [[ "$RECOVERY_ENABLED" != "true" ]]; then
        log_info "Automated recovery is disabled"
        return 0
    fi
    
    log_info "Attempting to restart unhealthy pods..."
    
    local unhealthy_pods=$(kubectl get pods -n "$NAMESPACE" -l app.kubernetes.io/name=oil-trading-system --no-headers | grep -v Running | awk '{print $1}' || echo "")
    
    if [[ -z "$unhealthy_pods" ]]; then
        log_info "No unhealthy pods to restart"
        return 0
    fi
    
    local restarted_pods=0
    
    while IFS= read -r pod_name; do
        if [[ -z "$pod_name" ]]; then continue; fi
        
        log_info "Restarting unhealthy pod: $pod_name"
        kubectl delete pod "$pod_name" -n "$NAMESPACE" --grace-period=30
        restarted_pods=$((restarted_pods + 1))
        
        send_alert "recovery" "Pod Restarted" "Automatically restarted unhealthy pod: $pod_name" "recovery"
    done <<< "$unhealthy_pods"
    
    if [[ $restarted_pods -gt 0 ]]; then
        log_success "Restarted $restarted_pods unhealthy pods"
        
        # Wait for pods to restart
        log_info "Waiting for pods to restart..."
        sleep 30
        
        # Verify restart was successful
        kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=oil-trading-system -n "$NAMESPACE" --timeout=300s || true
    fi
}

scale_up_on_high_load() {
    if [[ "$RECOVERY_ENABLED" != "true" ]]; then
        return 0
    fi
    
    log_info "Checking if scaling is needed..."
    
    # Check current replica count
    local current_api_replicas=$(kubectl get deployment oil-trading-api -n "$NAMESPACE" -o jsonpath='{.status.replicas}' 2>/dev/null || echo "0")
    local max_replicas=10
    
    # Check CPU usage across API pods
    local avg_cpu=$(kubectl top pods -n "$NAMESPACE" -l app.kubernetes.io/component=api --no-headers 2>/dev/null | awk '{gsub(/m/, "", $2); sum+=$2; count++} END {if(count>0) print sum/count; else print 0}')
    
    if [[ $(echo "$avg_cpu > $((CPU_THRESHOLD * 10))" | bc 2>/dev/null || echo "0") -eq 1 ]] && [[ $current_api_replicas -lt $max_replicas ]]; then
        local new_replicas=$((current_api_replicas + 1))
        log_info "Scaling up API deployment from $current_api_replicas to $new_replicas replicas"
        
        kubectl scale deployment oil-trading-api -n "$NAMESPACE" --replicas="$new_replicas"
        send_alert "recovery" "Auto-scaled API" "Scaled API deployment to $new_replicas replicas due to high CPU usage" "autoscaling"
    fi
}

# Main health check runner
run_health_checks() {
    log_info "Starting comprehensive health check..."
    
    local failed_checks=0
    local total_checks=0
    
    # List of health check functions
    local health_checks=(
        "check_pod_health"
        "check_service_endpoints"
        "check_resource_usage"
        "check_api_health"
        "check_database_connectivity"
        "check_redis_connectivity"
    )
    
    for check_function in "${health_checks[@]}"; do
        total_checks=$((total_checks + 1))
        
        if ! $check_function; then
            failed_checks=$((failed_checks + 1))
        fi
        
        sleep 2  # Brief pause between checks
    done
    
    # Summary
    if [[ $failed_checks -eq 0 ]]; then
        log_success "All health checks passed ($total_checks/$total_checks)"
        send_alert "info" "Health Check Passed" "All $total_checks health checks passed successfully" "health"
    else
        log_error "$failed_checks out of $total_checks health checks failed"
        send_alert "critical" "Health Check Failed" "$failed_checks out of $total_checks health checks failed" "health"
        
        # Attempt recovery if enabled
        if [[ "$RECOVERY_ENABLED" == "true" ]]; then
            log_info "Attempting automated recovery..."
            restart_unhealthy_pods
            scale_up_on_high_load
        fi
    fi
    
    return $failed_checks
}

# Continuous monitoring function
start_monitoring() {
    log_info "Starting continuous health monitoring..."
    log_info "Check interval: ${CHECK_INTERVAL} seconds"
    log_info "Recovery enabled: $RECOVERY_ENABLED"
    
    local check_count=0
    
    while true; do
        check_count=$((check_count + 1))
        log_info "=== Health Check #$check_count ==="
        
        if run_health_checks; then
            log_success "Health check #$check_count completed successfully"
        else
            log_warning "Health check #$check_count detected issues"
        fi
        
        log_info "Next check in ${CHECK_INTERVAL} seconds..."
        sleep "$CHECK_INTERVAL"
    done
}

# Usage function
usage() {
    cat << EOF
$SCRIPT_NAME v$SCRIPT_VERSION

Usage: $0 [OPTIONS] [COMMAND]

COMMANDS:
    monitor         Start continuous monitoring (default)
    check           Run health checks once and exit
    recover         Run recovery actions only

OPTIONS:
    -e, --environment ENV       Target environment [default: production]
    -n, --namespace NAMESPACE   Kubernetes namespace [default: oil-trading-ENV]
    -i, --interval SECONDS      Check interval for monitoring [default: 60]
    --no-recovery               Disable automated recovery
    --verbose                   Enable verbose output
    -h, --help                  Show this help message

EXAMPLES:
    # Start continuous monitoring
    $0 monitor

    # Run a single health check
    $0 check

    # Monitor with custom interval
    $0 monitor --interval 30

    # Check staging environment
    $0 check --environment staging

ENVIRONMENT VARIABLES:
    ALERT_WEBHOOK               Webhook URL for alerts
    RECOVERY_ENABLED            Enable/disable recovery [default: true]
    VERBOSE                     Enable verbose logging [default: false]
EOF
}

# Parse command line arguments
parse_args() {
    local command="monitor"
    
    while [[ $# -gt 0 ]]; do
        case $1 in
            -e|--environment)
                ENVIRONMENT="$2"
                NAMESPACE="oil-trading-$ENVIRONMENT"
                shift 2
                ;;
            -n|--namespace)
                NAMESPACE="$2"
                shift 2
                ;;
            -i|--interval)
                CHECK_INTERVAL="$2"
                shift 2
                ;;
            --no-recovery)
                RECOVERY_ENABLED="false"
                shift
                ;;
            --verbose)
                VERBOSE="true"
                shift
                ;;
            -h|--help)
                usage
                exit 0
                ;;
            monitor|check|recover)
                command="$1"
                shift
                ;;
            *)
                log_error "Unknown option: $1"
                usage
                exit 1
                ;;
        esac
    done
    
    echo "$command"
}

# Main function
main() {
    local command=$(parse_args "$@")
    
    show_banner
    
    # Validate tools
    if ! command -v kubectl &> /dev/null; then
        log_error "kubectl is required but not installed"
        exit 1
    fi
    
    # Validate cluster connection
    if ! kubectl cluster-info &> /dev/null; then
        log_error "Cannot connect to Kubernetes cluster"
        exit 1
    fi
    
    case "$command" in
        "monitor")
            start_monitoring
            ;;
        "check")
            run_health_checks
            exit $?
            ;;
        "recover")
            restart_unhealthy_pods
            scale_up_on_high_load
            ;;
        *)
            log_error "Unknown command: $command"
            usage
            exit 1
            ;;
    esac
}

# Execute main function
main "$@"