#!/bin/bash
# Comprehensive Health Check Script for Oil Trading System
# This script performs detailed health checks on all system components

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
TIMEOUT="${TIMEOUT:-10}"
DETAILED="${DETAILED:-false}"
OUTPUT_FORMAT="${OUTPUT_FORMAT:-text}" # text, json, prometheus

# Health check results
declare -A health_results
declare -A response_times
declare -A error_messages

# Helper functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[✓]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[⚠]${NC} $1"
}

log_error() {
    echo -e "${RED}[✗]${NC} $1"
}

log_section() {
    echo -e "\n${CYAN}=== $1 ===${NC}"
}

# Health check function
check_service() {
    local service_name=$1
    local check_url=$2
    local expected_status=${3:-200}
    local check_type=${4:-http}
    
    local start_time=$(date +%s%3N)
    local status="FAIL"
    local error_msg=""
    
    case $check_type in
        "http")
            local response=$(curl -s -w "%{http_code}" -m $TIMEOUT "$check_url" 2>&1)
            local http_code=${response: -3}
            
            if [[ "$http_code" == "$expected_status" ]]; then
                status="PASS"
            else
                error_msg="HTTP $http_code (expected $expected_status)"
            fi
            ;;
            
        "tcp")
            if nc -z -w $TIMEOUT ${check_url/:/ } 2>/dev/null; then
                status="PASS"
            else
                error_msg="Connection failed"
            fi
            ;;
            
        "container")
            if docker-compose -p $PROJECT_NAME ps $service_name | grep -q "Up"; then
                status="PASS"
            else
                error_msg="Container not running"
            fi
            ;;
    esac
    
    local end_time=$(date +%s%3N)
    local response_time=$((end_time - start_time))
    
    health_results[$service_name]=$status
    response_times[$service_name]=$response_time
    error_messages[$service_name]=$error_msg
    
    if [[ "$status" == "PASS" ]]; then
        log_success "$service_name (${response_time}ms)"
    else
        log_error "$service_name - $error_msg (${response_time}ms)"
    fi
}

# Infrastructure health checks
check_infrastructure() {
    log_section "Infrastructure Services"
    
    check_service "postgres" "localhost:5432" 0 "tcp"
    check_service "redis" "localhost:6379" 0 "tcp"
    check_service "elasticsearch" "http://localhost:9200/_cluster/health" 200 "http"
    
    if [[ "$DETAILED" == "true" ]]; then
        # Additional PostgreSQL checks
        local pg_status=$(docker-compose -p $PROJECT_NAME exec -T postgres pg_isready -U postgres 2>/dev/null || echo "FAIL")
        if [[ "$pg_status" =~ "accepting connections" ]]; then
            log_success "PostgreSQL accepting connections"
        else
            log_error "PostgreSQL not accepting connections"
        fi
        
        # Redis ping
        local redis_ping=$(docker-compose -p $PROJECT_NAME exec -T redis redis-cli ping 2>/dev/null || echo "FAIL")
        if [[ "$redis_ping" == "PONG" ]]; then
            log_success "Redis responding to PING"
        else
            log_error "Redis not responding to PING"
        fi
        
        # Elasticsearch cluster health
        local es_health=$(curl -s http://localhost:9200/_cluster/health 2>/dev/null | jq -r '.status' 2>/dev/null || echo "unknown")
        case $es_health in
            "green") log_success "Elasticsearch cluster status: GREEN" ;;
            "yellow") log_warning "Elasticsearch cluster status: YELLOW" ;;
            "red") log_error "Elasticsearch cluster status: RED" ;;
            *) log_warning "Elasticsearch cluster status: UNKNOWN" ;;
        esac
    fi
}

# Monitoring services health checks
check_monitoring() {
    log_section "Monitoring Services"
    
    check_service "prometheus" "http://localhost:9090/-/healthy" 200 "http"
    check_service "grafana" "http://localhost:3000/api/health" 200 "http"
    check_service "alertmanager" "http://localhost:9093/-/healthy" 200 "http"
    check_service "kibana" "http://localhost:5601/api/status" 200 "http"
    check_service "jaeger" "http://localhost:16686/" 200 "http"
    check_service "loki" "http://localhost:3100/ready" 200 "http"
    check_service "tempo" "http://localhost:3200/ready" 200 "http"
    
    if [[ "$DETAILED" == "true" ]]; then
        # Prometheus targets
        local prom_targets=$(curl -s http://localhost:9090/api/v1/targets 2>/dev/null | jq -r '.data.activeTargets | length' 2>/dev/null || echo "0")
        log_info "Prometheus monitoring $prom_targets targets"
        
        # Grafana datasources
        local grafana_ds=$(curl -s -u admin:admin123 http://localhost:3000/api/datasources 2>/dev/null | jq '. | length' 2>/dev/null || echo "0")
        log_info "Grafana has $grafana_ds datasources configured"
        
        # AlertManager alerts
        local alerts=$(curl -s http://localhost:9093/api/v1/alerts 2>/dev/null | jq '.data | length' 2>/dev/null || echo "0")
        log_info "AlertManager has $alerts active alerts"
    fi
}

# Application health checks
check_application() {
    log_section "Application Services"
    
    check_service "oiltrading-api" "http://localhost:8080/health" 200 "http"
    check_service "nginx" "http://localhost:80" 200 "http"
    
    if [[ "$DETAILED" == "true" ]]; then
        # API detailed health
        local api_health=$(curl -s http://localhost:8080/health/detailed 2>/dev/null)
        if [[ -n "$api_health" ]]; then
            local overall_status=$(echo "$api_health" | jq -r '.status' 2>/dev/null || echo "unknown")
            local uptime=$(echo "$api_health" | jq -r '.uptime' 2>/dev/null || echo "unknown")
            local version=$(echo "$api_health" | jq -r '.version' 2>/dev/null || echo "unknown")
            
            log_info "API Status: $overall_status, Version: $version, Uptime: $uptime"
            
            # Business metrics
            local active_contracts=$(echo "$api_health" | jq -r '.businessMetrics.activeContracts' 2>/dev/null || echo "0")
            local pricing_events=$(echo "$api_health" | jq -r '.businessMetrics.todayPricingEvents' 2>/dev/null || echo "0")
            local trading_partners=$(echo "$api_health" | jq -r '.businessMetrics.totalTradingPartners' 2>/dev/null || echo "0")
            
            log_info "Business: $active_contracts contracts, $pricing_events price events today, $trading_partners partners"
        fi
        
        # Check API endpoints
        local endpoints=("/health/ready" "/health/live" "/swagger/v1/swagger.json" "/metrics")
        for endpoint in "${endpoints[@]}"; do
            check_service "api$endpoint" "http://localhost:8080$endpoint" 200 "http"
        done
    fi
}

# Exporters health checks
check_exporters() {
    log_section "Metrics Exporters"
    
    check_service "node-exporter" "http://localhost:9100/metrics" 200 "http"
    check_service "cadvisor" "http://localhost:8080/metrics" 200 "http"
    check_service "postgres-exporter" "http://localhost:9187/metrics" 200 "http"
    check_service "redis-exporter" "http://localhost:9121/metrics" 200 "http"
    check_service "blackbox-exporter" "http://localhost:9115/metrics" 200 "http"
    
    if [[ "$DETAILED" == "true" ]]; then
        # Check metric counts
        for exporter in "node-exporter:9100" "postgres-exporter:9187" "redis-exporter:9121"; do
            local name=${exporter%:*}
            local port=${exporter#*:}
            local metric_count=$(curl -s "http://localhost:$port/metrics" 2>/dev/null | grep -c "^[a-zA-Z]" || echo "0")
            log_info "$name exposing $metric_count metrics"
        done
    fi
}

# Container health checks
check_containers() {
    log_section "Container Status"
    
    local containers=(
        "postgres" "redis" "elasticsearch"
        "prometheus" "grafana" "alertmanager" "kibana"
        "jaeger" "loki" "tempo" "otel-collector"
        "logstash" "promtail"
        "node-exporter" "cadvisor" "postgres-exporter" "redis-exporter" "blackbox-exporter"
        "oiltrading-api" "nginx"
    )
    
    for container in "${containers[@]}"; do
        check_service "$container" "" 0 "container"
    done
    
    if [[ "$DETAILED" == "true" ]]; then
        log_info "Container resource usage:"
        docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}" $(docker-compose -p $PROJECT_NAME ps -q) 2>/dev/null || log_warning "Could not retrieve container stats"
    fi
}

# System resource checks
check_system_resources() {
    log_section "System Resources"
    
    # Disk space
    local disk_usage=$(df / | awk 'NR==2 {print $5}' | sed 's/%//')
    if [[ $disk_usage -lt 80 ]]; then
        log_success "Disk usage: ${disk_usage}%"
    elif [[ $disk_usage -lt 90 ]]; then
        log_warning "Disk usage: ${disk_usage}%"
    else
        log_error "Disk usage: ${disk_usage}% (Critical)"
    fi
    
    # Memory usage
    local memory_usage=$(free | awk 'NR==2{printf "%.1f", $3*100/$2}')
    local memory_usage_int=${memory_usage%.*}
    if [[ $memory_usage_int -lt 80 ]]; then
        log_success "Memory usage: ${memory_usage}%"
    elif [[ $memory_usage_int -lt 90 ]]; then
        log_warning "Memory usage: ${memory_usage}%"
    else
        log_error "Memory usage: ${memory_usage}% (Critical)"
    fi
    
    # Load average
    local load_avg=$(uptime | awk -F'load average:' '{print $2}' | cut -d, -f1 | xargs)
    local cpu_cores=$(nproc)
    local load_ratio=$(echo "$load_avg $cpu_cores" | awk '{printf "%.1f", $1/$2*100}')
    local load_ratio_int=${load_ratio%.*}
    
    if [[ $load_ratio_int -lt 70 ]]; then
        log_success "Load average: $load_avg (${load_ratio}% of $cpu_cores cores)"
    elif [[ $load_ratio_int -lt 90 ]]; then
        log_warning "Load average: $load_avg (${load_ratio}% of $cpu_cores cores)"
    else
        log_error "Load average: $load_avg (${load_ratio}% of $cpu_cores cores)"
    fi
    
    # Docker daemon
    if docker info >/dev/null 2>&1; then
        log_success "Docker daemon is running"
    else
        log_error "Docker daemon is not accessible"
    fi
}

# Generate summary
generate_summary() {
    log_section "Health Check Summary"
    
    local total=0
    local passed=0
    local failed=0
    
    for service in "${!health_results[@]}"; do
        total=$((total + 1))
        if [[ "${health_results[$service]}" == "PASS" ]]; then
            passed=$((passed + 1))
        else
            failed=$((failed + 1))
        fi
    done
    
    echo "Total checks: $total"
    echo "Passed: $passed"
    echo "Failed: $failed"
    echo "Success rate: $(echo "scale=1; $passed * 100 / $total" | bc)%"
    
    if [[ $failed -eq 0 ]]; then
        log_success "🎉 All health checks passed!"
        return 0
    else
        log_warning "⚠️  $failed health check(s) failed"
        echo
        echo "Failed services:"
        for service in "${!health_results[@]}"; do
            if [[ "${health_results[$service]}" == "FAIL" ]]; then
                echo "  - $service: ${error_messages[$service]}"
            fi
        done
        return 1
    fi
}

# Output results in different formats
output_results() {
    case $OUTPUT_FORMAT in
        "json")
            echo "{"
            echo "  \"timestamp\": \"$(date -Iseconds)\","
            echo "  \"overall_status\": \"$([ $1 -eq 0 ] && echo "healthy" || echo "unhealthy")\","
            echo "  \"services\": {"
            local first=true
            for service in "${!health_results[@]}"; do
                [[ "$first" == "true" ]] && first=false || echo ","
                echo -n "    \"$service\": {"
                echo -n "\"status\": \"${health_results[$service]}\","
                echo -n "\"response_time_ms\": ${response_times[$service]}"
                [[ -n "${error_messages[$service]}" ]] && echo -n ",\"error\": \"${error_messages[$service]}\""
                echo -n "}"
            done
            echo
            echo "  }"
            echo "}"
            ;;
        "prometheus")
            echo "# HELP oil_trading_health_check Health check results"
            echo "# TYPE oil_trading_health_check gauge"
            for service in "${!health_results[@]}"; do
                local value=$([ "${health_results[$service]}" == "PASS" ] && echo "1" || echo "0")
                echo "oil_trading_health_check{service=\"$service\"} $value"
            done
            echo "# HELP oil_trading_response_time_ms Response time in milliseconds"
            echo "# TYPE oil_trading_response_time_ms gauge"
            for service in "${!health_results[@]}"; do
                echo "oil_trading_response_time_ms{service=\"$service\"} ${response_times[$service]}"
            done
            ;;
    esac
}

# Main execution
main() {
    echo "Oil Trading System - Comprehensive Health Check"
    echo "=============================================="
    echo "Timestamp: $(date)"
    echo "Project: $PROJECT_NAME"
    echo "Timeout: ${TIMEOUT}s"
    [[ "$DETAILED" == "true" ]] && echo "Mode: Detailed"
    
    # Run all health checks
    check_infrastructure
    check_monitoring
    check_application
    check_exporters
    check_containers
    check_system_resources
    
    # Generate summary
    generate_summary
    summary_result=$?
    
    # Output in requested format
    if [[ "$OUTPUT_FORMAT" != "text" ]]; then
        echo
        output_results $summary_result
    fi
    
    exit $summary_result
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -d|--detailed)
            DETAILED=true
            shift
            ;;
        -t|--timeout)
            TIMEOUT="$2"
            shift 2
            ;;
        -f|--format)
            OUTPUT_FORMAT="$2"
            shift 2
            ;;
        -p|--project)
            PROJECT_NAME="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  -d, --detailed     Run detailed health checks"
            echo "  -t, --timeout SEC  Health check timeout (default: 10)"
            echo "  -f, --format FMT   Output format: text, json, prometheus (default: text)"
            echo "  -p, --project NAME Docker project name (default: oiltrading)"
            echo "  -h, --help         Show this help"
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Check dependencies
if ! command -v curl >/dev/null 2>&1; then
    log_error "curl is required but not installed"
    exit 1
fi

if ! command -v nc >/dev/null 2>&1; then
    log_warning "netcat (nc) not found - TCP checks will be skipped"
fi

if ! command -v jq >/dev/null 2>&1; then
    log_warning "jq not found - JSON parsing will be limited"
fi

# Run main function
main "$@"