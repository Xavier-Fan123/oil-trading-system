{{/*
Expand the name of the chart.
*/}}
{{- define "oil-trading-system.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "oil-trading-system.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "oil-trading-system.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "oil-trading-system.labels" -}}
helm.sh/chart: {{ include "oil-trading-system.chart" . }}
{{ include "oil-trading-system.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- if .Values.global.environment }}
environment: {{ .Values.global.environment }}
{{- end }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "oil-trading-system.selectorLabels" -}}
app.kubernetes.io/name: {{ include "oil-trading-system.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account to use for API
*/}}
{{- define "oil-trading-system.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (printf "%s-api" (include "oil-trading-system.fullname" .)) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
Create the name of the service account to use for Frontend
*/}}
{{- define "oil-trading-system.frontend.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- printf "%s-frontend" (include "oil-trading-system.fullname" .) }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
Create namespace name
*/}}
{{- define "oil-trading-system.namespace" -}}
{{- if .Values.namespaceOverride }}
{{- .Values.namespaceOverride }}
{{- else }}
{{- .Release.Namespace }}
{{- end }}
{{- end }}

{{/*
PostgreSQL connection string
*/}}
{{- define "oil-trading-system.postgresql.connectionString" -}}
{{- if .Values.postgresql.enabled }}
Host={{ include "oil-trading-system.fullname" . }}-postgresql;Port=5432;Database={{ .Values.postgresql.auth.database }};Username=postgres;Password={{ .Values.postgresql.auth.postgresPassword }}
{{- else }}
{{- .Values.externalDatabase.connectionString }}
{{- end }}
{{- end }}

{{/*
Redis connection string
*/}}
{{- define "oil-trading-system.redis.connectionString" -}}
{{- if .Values.redis.enabled }}
{{ include "oil-trading-system.fullname" . }}-redis:6379
{{- else }}
{{- .Values.externalRedis.connectionString }}
{{- end }}
{{- end }}

{{/*
Common security context
*/}}
{{- define "oil-trading-system.securityContext" -}}
runAsNonRoot: true
runAsUser: 2000
runAsGroup: 2000
fsGroup: 2000
readOnlyRootFilesystem: true
allowPrivilegeEscalation: false
seccompProfile:
  type: RuntimeDefault
capabilities:
  drop:
    - ALL
{{- end }}

{{/*
Image pull policy
*/}}
{{- define "oil-trading-system.imagePullPolicy" -}}
{{- if eq .Values.global.environment "development" }}
{{- "Always" }}
{{- else }}
{{- .Values.image.pullPolicy | default "IfNotPresent" }}
{{- end }}
{{- end }}

{{/*
Resource limits for development
*/}}
{{- define "oil-trading-system.resources.development" -}}
limits:
  cpu: 500m
  memory: 512Mi
requests:
  cpu: 100m
  memory: 128Mi
{{- end }}

{{/*
Resource limits for staging
*/}}
{{- define "oil-trading-system.resources.staging" -}}
limits:
  cpu: 1000m
  memory: 1Gi
requests:
  cpu: 250m
  memory: 256Mi
{{- end }}

{{/*
Resource limits for production
*/}}
{{- define "oil-trading-system.resources.production" -}}
limits:
  cpu: 2000m
  memory: 2Gi
requests:
  cpu: 500m
  memory: 512Mi
{{- end }}

{{/*
Environment-specific resources
*/}}
{{- define "oil-trading-system.resources" -}}
{{- if eq .Values.global.environment "development" }}
{{- include "oil-trading-system.resources.development" . }}
{{- else if eq .Values.global.environment "staging" }}
{{- include "oil-trading-system.resources.staging" . }}
{{- else }}
{{- include "oil-trading-system.resources.production" . }}
{{- end }}
{{- end }}

{{/*
Monitoring labels
*/}}
{{- define "oil-trading-system.monitoring.labels" -}}
{{- if .Values.monitoring.enabled }}
monitor: "true"
prometheus.io/scrape: "true"
{{- end }}
{{- end }}

{{/*
Generate certificates secret name
*/}}
{{- define "oil-trading-system.certificates.secretName" -}}
{{- if .Values.ingress.tls }}
{{- range .Values.ingress.tls }}
{{- .secretName }}
{{- end }}
{{- else }}
{{- printf "%s-tls" (include "oil-trading-system.fullname" .) }}
{{- end }}
{{- end }}

{{/*
Validate required values
*/}}
{{- define "oil-trading-system.validateValues" -}}
{{- if and .Values.postgresql.enabled (not .Values.postgresql.auth.postgresPassword) }}
{{- fail "PostgreSQL password is required when PostgreSQL is enabled" }}
{{- end }}
{{- if and .Values.redis.enabled (not .Values.redis.auth.password) }}
{{- fail "Redis password is required when Redis is enabled" }}
{{- end }}
{{- if not .Values.global.imageRegistry }}
{{- fail "Global image registry is required" }}
{{- end }}
{{- end }}