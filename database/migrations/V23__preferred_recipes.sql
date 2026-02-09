create table app.workflow_recipes
(
    workflow_recipe_id serial primary key,
    workflow_id integer not null
        references app.workflows on delete cascade,
    recipe_id integer not null
        references app.recipes on delete cascade
);

alter table app.workflow_nodes
    drop column is_preferred;