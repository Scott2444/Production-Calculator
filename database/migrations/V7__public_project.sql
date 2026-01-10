alter table app.projects
    add column is_public boolean not null default false;

alter table app.projects
    add column alias_project_id integer
    default null
        constraint fk_alias_project
            references app.projects(project_id)
            on delete set null;
