# ============================================================================
# OPTIMIZED FRONTEND DOCKERFILE FOR OIL TRADING SYSTEM
# Multi-stage build for React/Vite application
# ============================================================================

# ============================================================================
# STAGE 1: Build Stage
# ============================================================================
FROM node:20-alpine AS builder

# Install security updates
RUN apk update && apk upgrade && apk add --no-cache \
    git \
    python3 \
    make \
    g++ \
    && rm -rf /var/cache/apk/*

# Create non-root user
RUN addgroup -g 1000 nodeuser && adduser -D -s /bin/sh -u 1000 -G nodeuser nodeuser

# Set working directory
WORKDIR /app

# Copy package files first (for better layer caching)
COPY --chown=nodeuser:nodeuser package*.json ./

# Switch to non-root user
USER nodeuser

# Install dependencies with security audit
RUN npm ci --only=production --audit --audit-level=moderate \
    && npm cache clean --force

# Copy source code
COPY --chown=nodeuser:nodeuser . .

# Build the application
ARG NODE_ENV=production
ARG VITE_API_BASE_URL=https://api.oiltrading.example.com
ENV NODE_ENV=$NODE_ENV
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

# ============================================================================
# STAGE 2: Runtime Stage (Nginx)
# ============================================================================
FROM nginx:1.25-alpine AS runtime

# Install security updates
RUN apk update && apk upgrade && apk add --no-cache \
    curl \
    && rm -rf /var/cache/apk/*

# Create non-root user for nginx
RUN addgroup -g 2000 nginxuser && adduser -D -s /bin/sh -u 2000 -G nginxuser nginxuser

# Copy built application from builder stage
COPY --from=builder --chown=nginxuser:nginxuser /app/dist /usr/share/nginx/html

# Copy optimized nginx configuration
COPY --chown=nginxuser:nginxuser nginx.conf /etc/nginx/nginx.conf

# Create nginx cache and log directories
RUN mkdir -p /var/cache/nginx/client_temp \
    /var/cache/nginx/proxy_temp \
    /var/cache/nginx/fastcgi_temp \
    /var/cache/nginx/uwsgi_temp \
    /var/cache/nginx/scgi_temp \
    /var/log/nginx \
    && chown -R nginxuser:nginxuser /var/cache/nginx /var/log/nginx \
    && chmod -R 755 /var/cache/nginx /var/log/nginx

# Create pid file directory
RUN mkdir -p /var/run \
    && chown nginxuser:nginxuser /var/run

# Switch to non-root user
USER nginxuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Add labels
LABEL maintainer="Oil Trading System DevOps Team" \
      description="Oil Trading System Frontend - Production" \
      version="1.0.0"

# Start nginx
CMD ["nginx", "-g", "daemon off;"]