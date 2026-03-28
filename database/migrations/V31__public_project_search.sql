create index idx_projects_public_search
    on app.projects
    using gin (
        (
            setweight(to_tsvector('english', coalesce(name, '')), 'A') ||
            setweight(to_tsvector('english', coalesce(description, '')), 'B')
        )
    )
    where is_public = true;

create index idx_projects_public_alias_count
    on app.projects (alias_count desc, project_id asc)
    where is_public = true;
