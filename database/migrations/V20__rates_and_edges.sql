drop table app.workflow_items;

alter table app.workflow_nodes
    alter column machine_version drop not null,
    alter column actual_machine_count drop not null,
    add column calculated_machine_count numeric(14, 6),
    add column calculated_target_rate numeric(14, 6),
    add column calculated_actual_rate numeric(14, 6);


create table app.workflow_edges
(
    workflow_edge_id serial primary key,
    workflow_id integer not null references app.workflows on delete cascade,
    
    producer_node_id integer references app.workflow_nodes on delete cascade,
    consumer_node_id integer references app.workflow_nodes on delete cascade,
    product_id integer not null references app.products,
    
    calculated_flow_rate numeric(14, 6) not null,
    actual_flow_rate numeric(14, 6) not null,
    is_external boolean default false not null
);