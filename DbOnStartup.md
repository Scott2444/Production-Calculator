# Notes for how to run 
-- Run in server terminal
docker run --name postgres_startup -e POSTGRES_PASSWORD=password -e POSTGRES_USER=postgres -e POSTGRES_DB=production_calculator -p 5151:5432 -v production_calculator_dev:/var/lib/postgresql/data postgres:18

-- Run in postgres container terminal
psql -h localhost -p 5151 -U postgres -d production_calculator

-- Create a dedicated, non-superuser role for Flyway
CREATE ROLE flyway LOGIN PASSWORD 'password'CREATEROLE NOINHERIT;

-- Create a schema owned by flyway so it can alter it freely
CREATE SCHEMA IF NOT EXISTS app AUTHORIZATION flyway;

-- Allow connection and object creation in the managed schema
GRANT CONNECT ON DATABASE production_calculator TO flyway;
GRANT USAGE, CREATE ON SCHEMA public TO flyway;
