-- Initialize the database with TimescaleDB extension and basic setup
CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;

-- Create a development database
CREATE DATABASE oil_trading_dev;

-- Connect to the development database and set up TimescaleDB
\c oil_trading_dev
CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;

-- You can add any additional initialization scripts here
-- For example, creating specific schemas, functions, or seed data