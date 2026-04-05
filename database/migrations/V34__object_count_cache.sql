alter table app.users
    add column project_count integer not null default 0;

update app.users u
set project_count = (
    select count(*)
    from app.projects p
    where p.user_id = u.user_id
);

alter table app.projects
    add column product_count integer not null default 0,
    add column recipe_count integer not null default 0,
    add column machine_count integer not null default 0,
    add column modifier_count integer not null default 0,
    add column attribute_count integer not null default 0,
    add column workflow_count integer not null default 0;

update app.projects p
set product_count = (
        select count(*)
        from app.products pr
        where pr.project_id = p.project_id
    ),
    recipe_count = (
        select count(*)
        from app.recipes r
        where r.project_id = p.project_id
    ),
    machine_count = (
        select count(*)
        from app.machines m
        where m.project_id = p.project_id
    ),
    modifier_count = (
        select count(*)
        from app.modifiers mo
        where mo.project_id = p.project_id
    ),
    attribute_count = (
        select count(*)
        from app.attributes a
        where a.project_id = p.project_id
    ),
    workflow_count = (
        select count(*)
        from app.workflows w
        where w.project_id = p.project_id
    );
