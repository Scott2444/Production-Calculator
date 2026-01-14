create table app.machines
(
    machine_id serial primary key,
    project_id integer not null
        constraint fk_project
            references app.projects
            on delete cascade,
    puid char(10) unique not null,
    name varchar(255) not null,
    description text,
    base_speed numeric(10, 2) not null default 1.00,
    created_at timestamp with time zone default now() not null,
    last_updated timestamp with time zone default now() not null
);

create table app.machine_recipes
(
    machine_recipe_id serial primary key,
    recipe_id integer not null
        constraint fk_recipe
            references app.recipes
            on delete cascade,
    machine_id integer not null
        constraint fk_machine
            references app.machines
            on delete cascade
);