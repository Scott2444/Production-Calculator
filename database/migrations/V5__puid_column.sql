ALTER TABLE app.users
    ADD COLUMN puid CHAR(10) UNIQUE NOT NULL;
    ADD UNIQUE INDEX idx_users_puid (puid);

ALTER TABLE app.projects
    ADD COLUMN puid CHAR(10) UNIQUE NOT NULL;
    ADD UNIQUE INDEX idx_projects_puid (puid);