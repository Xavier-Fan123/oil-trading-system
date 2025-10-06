#!/bin/bash
# Stop Monitoring Stack Script for Oil Trading System
# This script gracefully stops the complete monitoring infrastructure

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_NAME="${PROJECT_NAME:-oiltrading}"
GRACEFUL_TIMEOUT="${GRACEFUL_TIMEOUT:-30}"

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

# Stop services gracefully
stop_services() {
    local phase=$1
    local services=$2
    
    log_info "Phase $phase: Stopping $services..."
    
    for service in $services; do
        log_info "Stopping $service..."
        docker-compose -p $PROJECT_NAME stop -t $GRACEFUL_TIMEOUT $service || log_warning "Failed to stop $service gracefully"
    done
}

# Main execution
main() {
    echo "Oil Trading System - Monitoring Stack Shutdown"
    echo "=============================================="
    
    log_info "Starting graceful shutdown..."
    
    # Phase 1: Stop application first
    stop_services "1" "nginx oiltrading-api"
    
    # Phase 2: Stop monitoring services
    stop_services "2" "grafana prometheus alertmanager"
    
    # Phase 3: Stop exporters
    stop_services "3" "node-exporter cadvisor postgres-exporter redis-exporter nginx-exporter blackbox-exporter"
    
    # Phase 4: Stop tracing services
    stop_services "4" "jaeger tempo otel-collector"
    
    # Phase 5: Stop logging services
    stop_services "5" "kibana logstash promtail loki"
    
    # Phase 6: Stop infrastructure last
    stop_services "6" "elasticsearch redis postgres"
    
    # Remove containers
    log_info "Removing containers..."
    docker-compose -p $PROJECT_NAME down --remove-orphans
    
    # Cleanup networks
    log_info "Cleaning up networks..."
    docker network prune -f
    
    log_success "🛑 Oil Trading Monitoring Stack stopped successfully!"
    
    echo
    echo "Data retention:"
    echo "- Volumes are preserved for data persistence"
    echo "- Use 'docker-compose down -v' to remove volumes if needed"
    echo "- Use 'docker system prune' to clean up unused resources"
}

# Handle script termination
cleanup() {
    log_info "Force stopping all containers..."
    docker-compose -p $PROJECT_NAME down --remove-orphans
    exit 1
}

trap cleanup INT TERM

# Run main function
main "$@"