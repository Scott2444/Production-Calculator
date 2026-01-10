alter table app.projects
    drop column alias_project_id;

alter table app.projects
    add column alias_project_puid char(10)
    default null
        constraint fk_alias_project
            references app.projects(puid)
            on delete set null;