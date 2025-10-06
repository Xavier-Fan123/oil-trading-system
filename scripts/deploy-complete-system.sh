#!/bin/bash
# =============================================================================
# Oil Trading System - Complete Production Deployment Script
# =============================================================================
# This script deploys the entire Oil Trading System to Kubernetes
# Supports multiple environments: development, staging, production
# =============================================================================

set -euo pipefail

# Script metadata
SCRIPT_VERSION="1.0.0"
SCRIPT_NAME="Oil Trading System Deployment"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Logging functions
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

log_debug() {
    if [[ "${DEBUG:-false}" == "true" ]]; then
        echo -e "${PURPLE}[DEBUG]${NC} $1"
    fi
}

# Banner function
show_banner() {
    echo -e "${CYAN}"
    cat << 'EOF'
╔═══════════════════════════════════════════════════════════════════════════════╗
║                        Oil Trading System Deployment                         ║
║                     Enterprise Trading & Risk Management                     ║
╚═══════════════════════════════════════════════════════════════════════════════╝
EOF
    echo -e "${NC}"
    echo -e "${BLUE}Version: ${SCRIPT_VERSION}${NC}"
    echo -e "${BLUE}Environment: ${ENVIRONMENT}${NC}"
    echo -e "${BLUE}Namespace: ${NAMESPACE}${NC}"
    echo
}

# Default configuration
ENVIRONMENT="${ENVIRONMENT:-staging}"
NAMESPACE="${NAMESPACE:-oil-trading-${ENVIRONMENT}}"
HELM_RELEASE_NAME="${HELM_RELEASE_NAME:-oil-trading-${ENVIRONMENT}}"
IMAGE_TAG="${IMAGE_TAG:-latest}"
TIMEOUT="${TIMEOUT:-600}"
DRY_RUN="${DRY_RUN:-false}"
DEBUG="${DEBUG:-false}"
SKIP_TESTS="${SKIP_TESTS:-false}"
BACKUP_ENABLED="${BACKUP_ENABLED:-true}"
FORCE_DEPLOY="${FORCE_DEPLOY:-false}"

# Configuration files
VALUES_FILE="${PROJECT_ROOT}/helm/oil-trading-system/values-${ENVIRONMENT}.yaml"
CHART_PATH="${PROJECT_ROOT}/helm/oil-trading-system"

# Usage function
usage() {
    cat << EOF
${SCRIPT_NAME} v${SCRIPT_VERSION}

Usage: $0 [OPTIONS]

OPTIONS:
    -e, --environment ENV       Target environment (development|staging|production) [default: staging]
    -n, --namespace NAMESPACE   Kubernetes namespace [default: oil-trading-ENV]
    -r, --release RELEASE       Helm release name [default: oil-trading-ENV]
    -t, --tag TAG               Docker image tag [default: latest]
    -f, --values-file FILE      Custom values file [default: values-ENV.yaml]
    --timeout SECONDS           Deployment timeout in seconds [default: 600]
    --dry-run                   Perform a dry run without making changes
    --skip-tests                Skip pre-deployment tests
    --no-backup                 Skip database backup (production only)
    --force                     Force deployment even if validation fails
    --debug                     Enable debug output
    -h, --help                  Show this help message

EXAMPLES:
    # Deploy to staging
    $0 --environment staging

    # Deploy to production with specific image tag
    $0 --environment production --tag v1.2.3

    # Dry run for production
    $0 --environment production --dry-run

    # Force deployment with debug output
    $0 --environment staging --force --debug

ENVIRONMENT VARIABLES:
    KUBECONFIG                  Path to kubeconfig file
    HELM_DEBUG                  Enable Helm debug output
    IMAGE_REGISTRY              Container image registry [default: ghcr.io]
    SLACK_WEBHOOK_URL           Slack webhook for notifications
EOF
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -e|--environment)
                ENVIRONMENT="$2"
                shift 2
                ;;
            -n|--namespace)
                NAMESPACE="$2"
                shift 2
                ;;
            -r|--release)
                HELM_RELEASE_NAME="$2"
                shift 2
                ;;
            -t|--tag)
                IMAGE_TAG="$2"
                shift 2
                ;;
            -f|--values-file)
                VALUES_FILE="$2"
                shift 2
                ;;
            --timeout)
                TIMEOUT="$2"
                shift 2
                ;;
            --dry-run)
                DRY_RUN="true"
                shift
                ;;
            --skip-tests)
                SKIP_TESTS="true"
                shift
                ;;
            --no-backup)
                BACKUP_ENABLED="false"
                shift
                ;;
            --force)
                FORCE_DEPLOY="true"
                shift
                ;;
            --debug)
                DEBUG="true"
                shift
                ;;
            -h|--help)
                usage
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                usage
                exit 1
                ;;
        esac
    done
    
    # Update derived variables
    NAMESPACE="${NAMESPACE:-oil-trading-${ENVIRONMENT}}"
    HELM_RELEASE_NAME="${HELM_RELEASE_NAME:-oil-trading-${ENVIRONMENT}}"
    VALUES_FILE="${VALUES_FILE:-${PROJECT_ROOT}/helm/oil-trading-system/values-${ENVIRONMENT}.yaml}"
}

# Validation functions
validate_environment() {
    log_info "Validating environment configuration..."
    
    case "$ENVIRONMENT" in
        development|staging|production)
            log_success "Environment '$ENVIRONMENT' is valid"
            ;;
        *)
            log_error "Invalid environment: $ENVIRONMENT"
            log_error "Valid environments: development, staging, production"
            exit 1
            ;;
    esac
}

validate_tools() {
    log_info "Validating required tools..."
    
    local required_tools=("kubectl" "helm" "docker" "jq" "curl")
    local missing_tools=()
    
    for tool in "${required_tools[@]}"; do
        if ! command -v "$tool" &> /dev/null; then
            missing_tools+=("$tool")
        fi
    done
    
    if [[ ${#missing_tools[@]} -gt 0 ]]; then
        log_error "Missing required tools: ${missing_tools[*]}"
        log_error "Please install missing tools and try again"
        exit 1
    fi
    
    log_success "All required tools are available"
}

validate_kubeconfig() {
    log_info "Validating Kubernetes configuration..."
    
    if ! kubectl cluster-info &> /dev/null; then
        log_error "Cannot connect to Kubernetes cluster"
        log_error "Please check your kubeconfig and try again"
        exit 1
    fi
    
    local context=$(kubectl config current-context)
    log_success "Connected to Kubernetes cluster (context: $context)"
    
    # Validate environment-specific context
    if [[ "$ENVIRONMENT" == "production" ]] && [[ "$context" != *"production"* ]]; then
        if [[ "$FORCE_DEPLOY" != "true" ]]; then
            log_error "Production deployment requires production context"
            log_error "Current context: $context"
            log_error "Use --force to override this check"
            exit 1
        else
            log_warning "Production deployment forced with non-production context"
        fi
    fi
}

validate_files() {
    log_info "Validating deployment files..."
    
    local required_files=(
        "$VALUES_FILE"
        "$CHART_PATH/Chart.yaml"
        "$CHART_PATH/values.yaml"
    )
    
    for file in "${required_files[@]}"; do
        if [[ ! -f "$file" ]]; then
            log_error "Required file not found: $file"
            exit 1
        fi
    done
    
    log_success "All required files are available"
}

validate_helm_chart() {
    log_info "Validating Helm chart..."
    
    if ! helm lint "$CHART_PATH" -f "$VALUES_FILE" &> /dev/null; then
        log_error "Helm chart validation failed"
        helm lint "$CHART_PATH" -f "$VALUES_FILE"
        exit 1
    fi
    
    log_success "Helm chart validation passed"
}

# Pre-deployment functions
create_namespace() {
    log_info "Creating namespace if not exists..."
    
    if kubectl get namespace "$NAMESPACE" &> /dev/null; then
        log_info "Namespace '$NAMESPACE' already exists"
    else
        if [[ "$DRY_RUN" == "true" ]]; then
            log_info "[DRY RUN] Would create namespace: $NAMESPACE"
        else
            kubectl create namespace "$NAMESPACE"
            kubectl label namespace "$NAMESPACE" "environment=$ENVIRONMENT" --overwrite
            log_success "Created namespace: $NAMESPACE"
        fi
    fi
}

setup_external_secrets() {
    log_info "Setting up external secrets..."
    
    if [[ "$ENVIRONMENT" == "production" ]]; then
        log_info "Applying external secrets configuration..."
        
        if [[ "$DRY_RUN" == "true" ]]; then
            log_info "[DRY RUN] Would apply external secrets"
        else
            kubectl apply -f "$PROJECT_ROOT/k8s/external-secrets/" -n "$NAMESPACE" || true
            log_success "External secrets configuration applied"
        fi
    else
        log_info "External secrets skipped for non-production environment"
    fi
}

create_backup() {
    if [[ "$ENVIRONMENT" != "production" ]] || [[ "$BACKUP_ENABLED" != "true" ]]; then
        log_info "Backup skipped for environment: $ENVIRONMENT"
        return 0
    fi
    
    log_info "Creating database backup..."
    
    local backup_name="backup-$(date +%Y%m%d-%H%M%S)"
    
    if [[ "$DRY_RUN" == "true" ]]; then
        log_info "[DRY RUN] Would create backup: $backup_name"
    else
        # Create backup job
        kubectl create job "$backup_name" \
            --from=cronjob/postgresql-backup \
            -n "$NAMESPACE" || true
        
        # Wait for backup completion
        kubectl wait --for=condition=complete \
            job/"$backup_name" \
            -n "$NAMESPACE" \
            --timeout=300s || true
        
        log_success "Database backup created: $backup_name"
    fi
}

# Testing functions
run_pre_deployment_tests() {
    if [[ "$SKIP_TESTS" == "true" ]]; then
        log_info "Pre-deployment tests skipped"
        return 0
    fi
    
    log_info "Running pre-deployment tests..."
    
    # Template Helm chart to validate
    log_info "Validating Helm templates..."
    if ! helm template "$HELM_RELEASE_NAME" "$CHART_PATH" \
        -f "$VALUES_FILE" \
        --namespace "$NAMESPACE" \
        --set image.tag="$IMAGE_TAG" > /dev/null; then
        log_error "Helm template validation failed"
        exit 1
    fi
    
    # Validate Kubernetes resources
    log_info "Validating Kubernetes resources..."
    helm template "$HELM_RELEASE_NAME" "$CHART_PATH" \
        -f "$VALUES_FILE" \
        --namespace "$NAMESPACE" \
        --set image.tag="$IMAGE_TAG" | \
        kubectl apply --dry-run=client -f - > /dev/null
    
    log_success "Pre-deployment tests passed"
}

# Deployment functions
deploy_monitoring() {
    log_info "Deploying monitoring stack..."
    
    if [[ "$DRY_RUN" == "true" ]]; then
        log_info "[DRY RUN] Would deploy monitoring stack"
    else
        # Apply Prometheus Operator
        kubectl apply -f "$PROJECT_ROOT/k8s/monitoring/prometheus-operator.yaml" || true
        
        # Apply Grafana dashboards
        kubectl apply -f "$PROJECT_ROOT/k8s/monitoring/grafana-dashboards.yaml" || true
        
        log_success "Monitoring stack deployed"
    fi
}

deploy_application() {
    log_info "Deploying Oil Trading System..."
    
    local helm_args=(
        "$HELM_RELEASE_NAME"
        "$CHART_PATH"
        "--namespace" "$NAMESPACE"
        "--create-namespace"
        "--values" "$VALUES_FILE"
        "--set" "image.tag=$IMAGE_TAG"
        "--set" "global.environment=$ENVIRONMENT"
        "--timeout" "${TIMEOUT}s"
        "--atomic"
        "--wait"
    )
    
    if [[ "$DRY_RUN" == "true" ]]; then
        helm_args+=("--dry-run")
        log_info "[DRY RUN] Helm deployment simulation"
    fi
    
    if [[ "$DEBUG" == "true" ]]; then
        helm_args+=("--debug")
    fi
    
    # Check if release exists
    if helm list -n "$NAMESPACE" | grep -q "$HELM_RELEASE_NAME"; then
        log_info "Upgrading existing release..."
        helm upgrade "${helm_args[@]}"
    else
        log_info "Installing new release..."
        helm install "${helm_args[@]}"
    fi
    
    if [[ "$DRY_RUN" != "true" ]]; then
        log_success "Application deployed successfully"
    fi
}

# Post-deployment functions
run_smoke_tests() {
    if [[ "$DRY_RUN" == "true" ]] || [[ "$SKIP_TESTS" == "true" ]]; then
        log_info "Smoke tests skipped"
        return 0
    fi
    
    log_info "Running smoke tests..."
    
    # Wait for pods to be ready
    log_info "Waiting for pods to be ready..."
    kubectl wait --for=condition=ready pod \
        -l app.kubernetes.io/name=oil-trading-system \
        -n "$NAMESPACE" \
        --timeout=300s
    
    # Test API health endpoint
    log_info "Testing API health endpoint..."
    kubectl run smoke-test-$(date +%s) \
        --image=curlimages/curl:latest \
        --rm -i --restart=Never \
        -n "$NAMESPACE" \
        -- curl -f "http://${HELM_RELEASE_NAME}-api:8080/health" \
        --max-time 10 \
        --retry 3
    
    # Test frontend
    log_info "Testing frontend..."
    kubectl run frontend-test-$(date +%s) \
        --image=curlimages/curl:latest \
        --rm -i --restart=Never \
        -n "$NAMESPACE" \
        -- curl -f "http://${HELM_RELEASE_NAME}-frontend:8080/health" \
        --max-time 10 \
        --retry 3
    
    log_success "Smoke tests passed"
}

verify_deployment() {
    if [[ "$DRY_RUN" == "true" ]]; then
        log_info "[DRY RUN] Deployment verification skipped"
        return 0
    fi
    
    log_info "Verifying deployment..."
    
    # Check Helm release status
    local release_status=$(helm status "$HELM_RELEASE_NAME" -n "$NAMESPACE" -o json | jq -r '.info.status')
    if [[ "$release_status" != "deployed" ]]; then
        log_error "Helm release status: $release_status"
        exit 1
    fi
    
    # Check pod status
    local failed_pods=$(kubectl get pods -n "$NAMESPACE" -l app.kubernetes.io/name=oil-trading-system --no-headers | grep -v Running | wc -l)
    if [[ "$failed_pods" -gt 0 ]]; then
        log_error "$failed_pods pod(s) are not running"
        kubectl get pods -n "$NAMESPACE" -l app.kubernetes.io/name=oil-trading-system
        exit 1
    fi
    
    # Check service endpoints
    local services=$(kubectl get services -n "$NAMESPACE" -l app.kubernetes.io/name=oil-trading-system -o name)
    for service in $services; do
        local endpoints=$(kubectl get endpoints "${service##*/}" -n "$NAMESPACE" -o jsonpath='{.subsets[0].addresses[0].ip}' 2>/dev/null || echo "")
        if [[ -z "$endpoints" ]]; then
            log_warning "Service ${service##*/} has no endpoints"
        fi
    done
    
    log_success "Deployment verification completed"
}

# Notification functions
send_notification() {
    local status="$1"
    local message="$2"
    
    if [[ -z "${SLACK_WEBHOOK_URL:-}" ]]; then
        log_debug "Slack webhook not configured, skipping notification"
        return 0
    fi
    
    local color="good"
    local emoji="✅"
    
    if [[ "$status" == "failure" ]]; then
        color="danger"
        emoji="❌"
    elif [[ "$status" == "warning" ]]; then
        color="warning"
        emoji="⚠️"
    fi
    
    local payload=$(cat << EOF
{
    "attachments": [
        {
            "color": "$color",
            "title": "$emoji Oil Trading System Deployment",
            "fields": [
                {
                    "title": "Environment",
                    "value": "$ENVIRONMENT",
                    "short": true
                },
                {
                    "title": "Namespace",
                    "value": "$NAMESPACE",
                    "short": true
                },
                {
                    "title": "Image Tag",
                    "value": "$IMAGE_TAG",
                    "short": true
                },
                {
                    "title": "Status",
                    "value": "$status",
                    "short": true
                }
            ],
            "text": "$message",
            "footer": "Oil Trading System Deployment Script",
            "ts": $(date +%s)
        }
    ]
}
EOF
)
    
    if curl -X POST -H 'Content-type: application/json' \
        --data "$payload" \
        "$SLACK_WEBHOOK_URL" &> /dev/null; then
        log_debug "Notification sent successfully"
    else
        log_warning "Failed to send notification"
    fi
}

# Cleanup function
cleanup() {
    local exit_code=$?
    
    if [[ $exit_code -ne 0 ]]; then
        log_error "Deployment failed with exit code: $exit_code"
        send_notification "failure" "Deployment failed. Please check the logs for details."
    fi
    
    # Cleanup temporary files
    rm -f /tmp/oil-trading-deploy-*
    
    exit $exit_code
}

# Main deployment workflow
main() {
    # Set up signal handlers
    trap cleanup EXIT INT TERM
    
    # Parse arguments
    parse_args "$@"
    
    # Show banner
    show_banner
    
    # Validation phase
    log_info "=== VALIDATION PHASE ==="
    validate_environment
    validate_tools
    validate_kubeconfig
    validate_files
    validate_helm_chart
    
    # Pre-deployment phase
    log_info "=== PRE-DEPLOYMENT PHASE ==="
    create_namespace
    setup_external_secrets
    create_backup
    run_pre_deployment_tests
    
    # Deployment phase
    log_info "=== DEPLOYMENT PHASE ==="
    deploy_monitoring
    deploy_application
    
    # Post-deployment phase
    log_info "=== POST-DEPLOYMENT PHASE ==="
    verify_deployment
    run_smoke_tests
    
    # Success
    log_success "=== DEPLOYMENT COMPLETED SUCCESSFULLY ==="
    send_notification "success" "Deployment completed successfully in $ENVIRONMENT environment."
    
    # Display information
    echo
    log_info "Deployment Information:"
    log_info "  Environment: $ENVIRONMENT"
    log_info "  Namespace: $NAMESPACE"
    log_info "  Release: $HELM_RELEASE_NAME"
    log_info "  Image Tag: $IMAGE_TAG"
    
    if [[ "$ENVIRONMENT" == "production" ]]; then
        log_info "  Production URL: https://oiltrading.example.com"
        log_info "  API URL: https://api.oiltrading.example.com"
    elif [[ "$ENVIRONMENT" == "staging" ]]; then
        log_info "  Staging URL: https://staging.oiltrading.example.com"
        log_info "  API URL: https://staging-api.oiltrading.example.com"
    fi
    
    echo
    log_success "Oil Trading System deployment completed! 🎉"
}

# Execute main function with all arguments
main "$@"