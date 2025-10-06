-- PostgreSQL Master Database Setup for Replication
-- This script sets up the master database for streaming replication

-- Create replication user
CREATE USER replica_user WITH REPLICATION ENCRYPTED PASSWORD 'replica_pass';

-- Grant necessary permissions
GRANT CONNECT ON DATABASE "OilTradingDb" TO replica_user;

-- Create replication slot (optional, for more robust replication)
SELECT pg_create_physical_replication_slot('replica_slot');

-- Log replication setup
INSERT INTO public.audit_logs (table_name, operation, old_values, new_values, user_name, timestamp)
VALUES ('SYSTEM', 'REPLICATION_SETUP', '{}', '{"replication_user": "created", "replication_slot": "replica_slot"}', 'system', NOW())
ON CONFLICT DO NOTHING;

-- Enable necessary extensions for production
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Create monitoring views
CREATE OR REPLACE VIEW v_replication_status AS
SELECT 
    client_addr,
    client_hostname,
    client_port,
    state,
    sent_lsn,
    write_lsn,
    flush_lsn,
    replay_lsn,
    write_lag,
    flush_lag,
    replay_lag,
    sync_state,
    sync_priority
FROM pg_stat_replication;

-- Create function to check replication lag
CREATE OR REPLACE FUNCTION get_replication_lag() 
RETURNS TABLE(
    replica_name text,
    lag_seconds numeric
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COALESCE(client_hostname, client_addr::text) as replica_name,
        EXTRACT(EPOCH FROM (pg_clock_timestamp() - pg_last_xact_replay_timestamp())) as lag_seconds
    FROM pg_stat_replication;
END;
$$ LANGUAGE plpgsql;

-- Log setup completion
DO $$
BEGIN
    RAISE NOTICE 'PostgreSQL Master replication setup completed successfully';
END $$;