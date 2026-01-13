create table app.recipes
(
    recipe_id serial primary key,
    project_id integer not null
        constraint fk_project
            references app.projects
            on delete cascade,
    puid char(10) unique not null,
    name varchar(255) not null,
    description text,
    base_crafting_time numeric(10, 2) not null,
    created_at timestamp with time zone default now() not null,
    last_updated timestamp with time zone default now() not null
);

create unique index idx_recipe_puid on app.recipes (puid);
create index idx_recipe_project_id on app.recipes (project_id);

create table app.recipe_product_inputs
(
    recipe_product_inputs_id serial primary key,
    recipe_id integer not null
        constraint fk_recipe
            references app.recipes
            on delete cascade,
    product_id integer not null
        constraint fk_product
            references app.products
            on delete cascade,
    quantity numeric(10, 2) not null
);

create table app.recipe_product_outputs
(
    recipe_product_outputs_id serial primary key,
    recipe_id integer not null
        constraint fk_recipe
            references app.recipes
            on delete cascade,
    product_id integer not null
        constraint fk_product
            references app.products
            on delete cascade,
    quantity numeric(10, 2) not null
);