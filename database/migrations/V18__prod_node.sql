create table app.production_nodes
(
    node_id serial primary key,
    workflow_id integer not null
        references app.workflows on delete cascade,
    puid char(10) not null unique,

    product_id integer not null
        references app.products on delete restrict,
    product_version integer not null,

    recipe_id integer
        references app.recipes on delete restrict,
    recipe_version integer not null,

    machine_id integer
        references app.machines on delete restrict,
    machine_version integer not null,

    parent_node_id integer
        references app.production_nodes on delete cascade,

    -- demand flowing into this node (items/sec)
    target_rate numeric(14, 6) not null,

    -- solver output (idealized)
    ideal_machine_count numeric(14, 6) not null,

    -- status flags
    is_root boolean default false not null,
    is_external boolean default false not null,

    created_at timestamp with time zone default now() not null,
    last_updated timestamp with time zone default now() not null
);

create table app.production_node_inputs
(
    node_input_id serial primary key,
    node_id       integer not null
        references app.production_nodes on delete cascade,

    input_product_id integer not null
        references app.products on delete restrict,

    -- required rate derived from solver (items/sec)
    required_rate numeric(14, 6) not null,
    is_cyclic boolean default false not null
);

create table app.production_node_state
(
    node_id integer primary key
        references app.production_nodes on delete cascade,
    -- what the user actually has
    actual_machine_count numeric(14, 6) default 0 not null,

    -- optional explicit supply override (items/sec)
    external_supply_rate numeric(14, 6),

    -- derived but stored for performance
    realized_recipe_rate numeric(14, 6) default 0 not null
);

create table app.production_node_modifiers
(
    node_modifier_id serial primary key,
    node_id integer not null
        references app.production_nodes on delete cascade,
    modifier_id integer not null
        references app.modifiers on delete restrict,
    modifier_version integer not null
);
