#!/bin/bash
# PostgreSQL Replica Setup Script
# This script configures a PostgreSQL instance as a streaming replica

set -e

echo "Starting PostgreSQL replica setup..."

# Wait for master to be ready
echo "Waiting for master database to be ready..."
until pg_isready -h $POSTGRES_MASTER_HOST -p $POSTGRES_MASTER_PORT -U $POSTGRES_USER; do
    echo "Master database is not ready yet, waiting 5 seconds..."
    sleep 5
done

echo "Master database is ready, proceeding with replica setup..."

# Stop PostgreSQL if running
pg_ctl stop -D $PGDATA -m fast || true

# Remove existing data directory
rm -rf $PGDATA/*

# Create base backup from master
echo "Creating base backup from master..."
pg_basebackup -h $POSTGRES_MASTER_HOST -p $POSTGRES_MASTER_PORT \
              -U $POSTGRES_REPLICATION_USER -D $PGDATA \
              -W -v -P -R

# Set up recovery configuration (for PostgreSQL 12+)
cat > $PGDATA/postgresql.auto.conf << EOF
# Replica configuration
primary_conninfo = 'host=$POSTGRES_MASTER_HOST port=$POSTGRES_MASTER_PORT user=$POSTGRES_REPLICATION_USER password=$POSTGRES_REPLICATION_PASSWORD application_name=replica1'
primary_slot_name = 'replica_slot'
hot_standby = on
max_standby_streaming_delay = 30s
wal_receiver_status_interval = 10s
hot_standby_feedback = on
EOF

# Create standby.signal file to indicate this is a replica
touch $PGDATA/standby.signal

# Set correct permissions
chmod 600 $PGDATA/postgresql.auto.conf
chown -R postgres:postgres $PGDATA

echo "PostgreSQL replica setup completed successfully!"

# Start PostgreSQL
exec postgres