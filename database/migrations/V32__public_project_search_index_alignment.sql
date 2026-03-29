alter table app.projects
    add column search_vector tsvector
    generated always as (
        setweight(to_tsvector('english', coalesce(name, '')), 'A') ||
        setweight(to_tsvector('english', coalesce(description, '')), 'B')
    ) stored;

drop index if exists app.idx_projects_public_search;

create index idx_projects_public_search
    on app.projects
    using gin (search_vector)
    where is_public = true;
