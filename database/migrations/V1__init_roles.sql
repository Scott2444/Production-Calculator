-- V1__initial_setup.sql

-- Do not run migrations as a superuser.
-- Have a dedicated Flyway user with permissions to alter the schema.

-- -----------------------------------------------------------------------------
-- ROLES
-- -----------------------------------------------------------------------------

-- Create a read/write role for the web server.
-- NOLOGIN means this role cannot be used to connect directly to the database.
-- It is intended to be granted to user roles.
CREATE ROLE web_server_role NOLOGIN;

-- Create a developer role with rights to modify the schema.
CREATE ROLE developer_role NOLOGIN;


-- -----------------------------------------------------------------------------
-- PERMISSIONS
-- -----------------------------------------------------------------------------

-- Grant connect permission on the database to both roles.
GRANT CONNECT ON DATABASE production_calculator TO web_server_role;
GRANT CONNECT ON DATABASE production_calculator TO developer_role;

-- Grant usage on the app schema.
GRANT USAGE ON SCHEMA app TO web_server_role;
GRANT USAGE, CREATE ON SCHEMA app TO developer_role;

-- Grant read/write permissions to the web server role on all current and future tables, views, and sequences.
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA app TO web_server_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA app GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO web_server_role;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA app TO web_server_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA app GRANT USAGE, SELECT ON SEQUENCES TO web_server_role;


-- Grant all privileges to the developer role on all current and future tables, views, and sequences.
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA app TO developer_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA app GRANT ALL PRIVILEGES ON TABLES TO developer_role;

GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA app TO developer_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA app GRANT ALL PRIVILEGES ON SEQUENCES TO developer_role;

GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA app TO developer_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA app GRANT ALL PRIVILEGES ON FUNCTIONS TO developer_role;

-- Grant developer read access to public schema for extensions/system views
GRANT USAGE ON SCHEMA public TO developer_role;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO developer_role;


-- -----------------------------------------------------------------------------
-- USER CREATION (Optional, can be managed outside of migrations)
-- -----------------------------------------------------------------------------

-- It is often better to manage user creation and password rotation outside of version-controlled migrations.
-- However, for completeness, here is how you would create users and assign them to the roles.
-- Remember to replace 'strong_password' with a secure password, preferably managed through a secrets manager.

-- CREATE USER web_user WITH LOGIN PASSWORD 'strong_password';
-- GRANT web_server_role TO web_user;

-- CREATE USER dev_user WITH LOGIN PASSWORD 'strong_password';
-- GRANT developer_role TO dev_user;