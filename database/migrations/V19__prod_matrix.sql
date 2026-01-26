drop table app.production_node_modifiers;
drop table app.production_node_state;
drop table app.production_node_inputs;
drop table app.production_nodes;

create table app.workflow_targets
(
    workflow_target_id serial primary key,
    workflow_id integer not null
        references app.workflows on delete cascade,
    product_id integer not null
        references app.products on delete cascade,
    target_rate numeric(14, 6) not null
);

create index idx_workflow_targets_workflow_id
    on app.workflow_targets (workflow_id);

create table app.workflow_nodes
(
    workflow_node_id serial primary key,
    workflow_id integer not null
        references app.workflows on delete cascade,

    -- Recipe used at this node
    recipe_id integer not null
        references app.recipes on delete restrict,
    recipe_version integer not null,
    is_preferred boolean default false not null,

    machine_id integer
        references app.machines on delete restrict,
    machine_version integer not null,
    actual_machine_count numeric(14, 6) default 0 not null
);

create index idx_workflow_nodes_workflow_id
    on app.workflow_nodes (workflow_id);

create table app.workflow_node_modifiers
(
    workflow_node_modifier_id serial primary key,
    workflow_node_id integer not null
        references app.workflow_nodes on delete cascade,
    modifier_id integer not null
        references app.modifiers on delete restrict
);

create index idx_workflow_node_modifiers_workflow_node_id
    on app.workflow_node_modifiers (workflow_node_id);

create table app.workflow_items
(
    workflow_item_id serial primary key,
    workflow_id integer not null
        references app.workflows on delete cascade,
    product_id integer not null
        references app.products on delete restrict,
    is_external boolean default false not null,
    external_supply_rate numeric(14, 6)
);