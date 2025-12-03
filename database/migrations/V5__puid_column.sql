ALTER TABLE app.users
    ADD COLUMN puid CHAR(10) UNIQUE NOT NULL;
CREATE UNIQUE INDEX idx_users_puid ON app.users (puid);

ALTER TABLE app.projects
    ADD COLUMN puid CHAR(10) UNIQUE NOT NULL;
CREATE UNIQUE INDEX idx_projects_puid ON app.projects (puid);