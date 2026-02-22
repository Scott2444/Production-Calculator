alter table app.modifiers
    rename column flat_speed_bonus to flat_bonus;
alter table app.modifiers
    rename column additive_percent_bonus to percent_bonus;
alter table app.modifiers
    rename column multiplicative_modifiers to multiplicative_bonus;
alter table app.modifiers
    add column input_multiplier numeric(13, 5) not null default 1.00,
    add column output_multiplier numeric(13, 5) not null default 1.00;


create table app.attributes
(
    attribute_id  serial primary key,
    project_id integer not null
        constraint fk_project
            references app.projects
            on delete cascade,
    puid char(10) unique not null,
    name varchar(255) not null,
    description text,
    unit varchar(50),
    version integer not null default 1,
    created_at timestamp with time zone default now() not null,
    last_updated timestamp with time zone default now() not null
);

create table app.recipe_attributes
(
    recipe_attribute_id serial primary key,
    recipe_id integer not null
        constraint fk_recipe
            references app.recipes
            on delete cascade,
    attribute_id integer not null
        constraint fk_attribute
            references app.attributes
            on delete cascade,
        constraint unique_recipe_attribute 
            unique (recipe_id, attribute_id),
    rate numeric(13, 5) not null,
    version integer not null default 1,
    created_at timestamp with time zone default now() not null,
    last_updated timestamp with time zone default now() not null
);

create table app.machine_attributes
(
    machine_attribute_id serial primary key,
    machine_id integer not null
        constraint fk_machine
            references app.machines
            on delete cascade,
    attribute_id integer not null
        constraint fk_attribute
            references app.attributes
            on delete cascade,
        constraint unique_machine_attribute 
            unique (machine_id, attribute_id),
    rate numeric(13, 5) not null,
    version integer not null default 1,
    created_at timestamp with time zone default now() not null,
    last_updated timestamp with time zone default now() not null
);

create table app.modifier_attributes
(
    modifier_attribute_id serial primary key,
    modifier_id integer not null
        constraint fk_modifier
            references app.modifiers
            on delete cascade,
    attribute_id integer not null
        constraint fk_attribute
            references app.attributes
            on delete cascade,
        constraint unique_modifier_attribute 
            unique (modifier_id, attribute_id),
    flat_bonus numeric(13, 5) not null default 0.00,
    percent_bonus numeric(13, 5) not null default 0.00,
    multiplicative_bonus numeric(13, 5) not null default 1.00,
    version integer not null default 1,
    created_at timestamp with time zone default now() not null,
    last_updated timestamp with time zone default now() not null
);

create table app.workflow_recipe_attributes
(
    workflow_recipe_attribute_id serial primary key,
    workflow_id integer not null
        constraint fk_workflow
            references app.workflows
            on delete cascade,
    recipe_id integer not null
        constraint fk_recipe
            references app.recipes
            on delete cascade,
    attribute_id integer not null
        constraint fk_attribute
            references app.attributes
            on delete cascade,
    rate numeric(13, 5) not null
);

create table app.workflow_machine_attributes
(
    workflow_machine_attribute_id serial primary key,
    workflow_id integer not null
        constraint fk_workflow
            references app.workflows
            on delete cascade,
    machine_id integer not null
        constraint fk_machine
            references app.machines
            on delete cascade,
    attribute_id integer not null
        constraint fk_attribute
            references app.attributes
            on delete cascade,
    rate numeric(13, 5) not null
);

create table app.workflow_modifier_attributes
(
    workflow_modifier_attribute_id serial primary key,
    workflow_id integer not null
        constraint fk_workflow
            references app.workflows
            on delete cascade,
    modifier_id integer not null
        constraint fk_modifier
            references app.modifiers
            on delete cascade,
    attribute_id integer not null
        constraint fk_attribute
            references app.attributes
            on delete cascade,
    flat_bonus numeric(13, 5) not null default 0.00,
    percent_bonus numeric(13, 5) not null default 0.00,
    multiplicative_bonus numeric(13, 5) not null default 1.00
);