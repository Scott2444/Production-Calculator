alter table app.projects
    add column alias_count integer not null default 0;

update app.projects po
set alias_count = (
    select COALESCE(count(*), 0)
    from app.projects pa
    where po.puid = pa.alias_project_puid
);
