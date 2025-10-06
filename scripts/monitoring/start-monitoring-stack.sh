#!/bin/bash
# Start Monitoring Stack Script for Oil Trading System
# This script starts the complete monitoring infrastructure

set -e

echo "🚀 Starting Oil Trading Monitoring Stack..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
DOCKER_COMPOSE_FILE="${DOCKER_COMPOSE_FILE:-docker-compose.yml}"
PROJECT_NAME="${PROJECT_NAME:-oiltrading}"
HEALTH_CHECK_TIMEOUT="${HEALTH_CHECK_TIMEOUT:-300}"
HEALTH_CHECK_INTERVAL="${HEALTH_CHECK_INTERVAL:-10}"

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

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi
    
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        log_error "Docker Compose is not installed"
        exit 1
    fi
    
    if ! docker info &> /dev/null; then
        log_error "Docker daemon is not running"
        exit 1
    fi
    
    log_success "Prerequisites check passed"
}

# Create required directories
create_directories() {
    log_info "Creating required directories..."
    
    mkdir -p logs
    mkdir -p monitoring/grafana/dashboards
    mkdir -p monitoring/grafana/datasources
    mkdir -p monitoring/grafana/alerting
    mkdir -p monitoring/prometheus/rules
    mkdir -p monitoring/alertmanager
    mkdir -p monitoring/logstash/config
    mkdir -p monitoring/logstash/pipeline
    mkdir -p monitoring/kibana/config
    mkdir -p monitoring/loki
    mkdir -p monitoring/promtail
    mkdir -p monitoring/tempo
    mkdir -p monitoring/otel
    mkdir -p monitoring/blackbox
    
    log_success "Directories created"
}

# Start infrastructure services first
start_infrastructure() {
    log_info "Starting infrastructure services..."
    
    # Start core infrastructure
    docker-compose -p $PROJECT_NAME up -d postgres redis elasticsearch
    
    # Wait for infrastructure to be ready
    log_info "Waiting for infrastructure services to be ready..."
    
    # Wait for PostgreSQL
    wait_for_service "postgres" "5432" "PostgreSQL"
    
    # Wait for Redis
    wait_for_service "redis" "6379" "Redis"
    
    # Wait for Elasticsearch
    wait_for_service "elasticsearch" "9200" "Elasticsearch"
    
    log_success "Infrastructure services are ready"
}

# Start monitoring services
start_monitoring() {
    log_info "Starting monitoring services..."
    
    # Start logging stack
    docker-compose -p $PROJECT_NAME up -d logstash kibana loki promtail
    
    # Start metrics stack
    docker-compose -p $PROJECT_NAME up -d prometheus alertmanager grafana
    
    # Start tracing stack
    docker-compose -p $PROJECT_NAME up -d jaeger tempo otel-collector
    
    # Start exporters
    docker-compose -p $PROJECT_NAME up -d node-exporter cadvisor postgres-exporter redis-exporter nginx-exporter blackbox-exporter
    
    log_success "Monitoring services started"
}

# Start application services
start_application() {
    log_info "Starting application services..."
    
    # Start application
    docker-compose -p $PROJECT_NAME up -d oiltrading-api nginx
    
    log_success "Application services started"
}

# Wait for service to be ready
wait_for_service() {
    local service_name=$1
    local port=$2
    local display_name=$3
    local timeout=$HEALTH_CHECK_TIMEOUT
    local interval=$HEALTH_CHECK_INTERVAL
    local counter=0
    
    log_info "Waiting for $display_name to be ready..."
    
    while [ $counter -lt $timeout ]; do
        if docker-compose -p $PROJECT_NAME exec -T $service_name /bin/sh -c "echo 'test' | nc -z localhost $port" &> /dev/null; then
            log_success "$display_name is ready"
            return 0
        fi
        
        sleep $interval
        counter=$((counter + interval))
        echo -n "."
    done
    
    echo
    log_error "$display_name failed to start within ${timeout}s"
    return 1
}

# Check service health
check_service_health() {
    local service_url=$1
    local service_name=$2
    
    log_info "Checking $service_name health..."
    
    if curl -sf "$service_url" > /dev/null 2>&1; then
        log_success "$service_name is healthy"
        return 0
    else
        log_warning "$service_name health check failed"
        return 1
    fi
}

# Run comprehensive health checks
run_health_checks() {
    log_info "Running comprehensive health checks..."
    
    local failed_checks=0
    
    # Core infrastructure
    check_service_health "http://localhost:5432" "PostgreSQL" || ((failed_checks++))
    check_service_health "http://localhost:6379" "Redis" || ((failed_checks++))
    check_service_health "http://localhost:9200" "Elasticsearch" || ((failed_checks++))
    
    # Monitoring services
    check_service_health "http://localhost:9090/-/healthy" "Prometheus" || ((failed_checks++))
    check_service_health "http://localhost:3000/api/health" "Grafana" || ((failed_checks++))
    check_service_health "http://localhost:5601/api/status" "Kibana" || ((failed_checks++))
    check_service_health "http://localhost:9093/-/healthy" "AlertManager" || ((failed_checks++))
    check_service_health "http://localhost:16686/" "Jaeger" || ((failed_checks++))
    check_service_health "http://localhost:3100/ready" "Loki" || ((failed_checks++))
    check_service_health "http://localhost:3200/ready" "Tempo" || ((failed_checks++))
    
    # Application
    check_service_health "http://localhost:8080/health" "Oil Trading API" || ((failed_checks++))
    
    if [ $failed_checks -eq 0 ]; then
        log_success "All health checks passed!"
    else
        log_warning "$failed_checks health checks failed"
    fi
    
    return $failed_checks
}

# Display access information
display_access_info() {
    log_info "Service Access Information:"
    echo
    echo "📊 Monitoring Dashboards:"
    echo "   Grafana:        http://localhost:3000 (admin/admin123)"
    echo "   Prometheus:     http://localhost:9090"
    echo "   AlertManager:   http://localhost:9093"
    echo "   Kibana:         http://localhost:5601"
    echo "   Jaeger:         http://localhost:16686"
    echo
    echo "🔧 System Metrics:"
    echo "   Node Exporter:  http://localhost:9100/metrics"
    echo "   cAdvisor:       http://localhost:8080"
    echo "   API Metrics:    http://localhost:8080/metrics"
    echo
    echo "🏥 Health Checks:"
    echo "   API Health:     http://localhost:8080/health"
    echo "   API Ready:      http://localhost:8080/health/ready"
    echo "   API Live:       http://localhost:8080/health/live"
    echo "   Detailed:       http://localhost:8080/health/detailed"
    echo
    echo "📖 Documentation:"
    echo "   API Docs:       http://localhost:8080/swagger"
    echo "   API OpenAPI:    http://localhost:8080/swagger/v1/swagger.json"
    echo
}

# Setup Grafana datasources and dashboards
setup_grafana() {
    log_info "Setting up Grafana datasources and dashboards..."
    
    # Wait for Grafana to be fully ready
    sleep 30
    
    # Create datasources
    curl -X POST \
        -H "Content-Type: application/json" \
        -d '{
            "name": "Prometheus",
            "type": "prometheus",
            "url": "http://prometheus:9090",
            "access": "proxy",
            "isDefault": true
        }' \
        http://admin:admin123@localhost:3000/api/datasources 2>/dev/null || true
    
    curl -X POST \
        -H "Content-Type: application/json" \
        -d '{
            "name": "Loki",
            "type": "loki",
            "url": "http://loki:3100",
            "access": "proxy"
        }' \
        http://admin:admin123@localhost:3000/api/datasources 2>/dev/null || true
    
    curl -X POST \
        -H "Content-Type: application/json" \
        -d '{
            "name": "Tempo",
            "type": "tempo",
            "url": "http://tempo:3200",
            "access": "proxy"
        }' \
        http://admin:admin123@localhost:3000/api/datasources 2>/dev/null || true
    
    log_success "Grafana setup completed"
}

# Main execution
main() {
    echo "Oil Trading System - Monitoring Stack Startup"
    echo "=============================================="
    
    check_prerequisites
    create_directories
    
    log_info "Starting services in phases..."
    
    # Phase 1: Infrastructure
    start_infrastructure
    
    # Phase 2: Monitoring
    start_monitoring
    
    # Phase 3: Application
    start_application
    
    # Wait for all services to stabilize
    log_info "Waiting for services to stabilize..."
    sleep 60
    
    # Run health checks
    run_health_checks
    health_status=$?
    
    # Setup Grafana
    setup_grafana
    
    # Display access information
    display_access_info
    
    if [ $health_status -eq 0 ]; then
        log_success "🎉 Oil Trading Monitoring Stack started successfully!"
        echo
        echo "Next steps:"
        echo "1. Access Grafana at http://localhost:3000"
        echo "2. Import additional dashboards as needed"
        echo "3. Configure alert notifications in AlertManager"
        echo "4. Review logs in Kibana at http://localhost:5601"
        exit 0
    else
        log_warning "Some services may need attention. Check the logs for details."
        echo
        echo "Troubleshooting:"
        echo "1. Check Docker container logs: docker-compose -p $PROJECT_NAME logs <service>"
        echo "2. Restart specific services: docker-compose -p $PROJECT_NAME restart <service>"
        echo "3. Check system resources: docker stats"
        exit 1
    fi
}

# Handle script termination
cleanup() {
    log_info "Cleaning up..."
    exit 1
}

trap cleanup INT TERM

# Run main function
main "$@"