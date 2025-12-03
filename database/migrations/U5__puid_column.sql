ALTER TABLE app.users
    DROP COLUMN puid;
DROP INDEX IF EXISTS idx_users_puid;

ALTER TABLE app.projects
    DROP COLUMN puid;
DROP INDEX IF EXISTS idx_projects_puid;