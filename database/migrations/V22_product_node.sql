create table app.workflow_product_nodes
(
    workflow_product_node_id serial primary key,
    workflow_id integer not null
        references app.workflows on delete cascade,
    product_id integer not null
        references app.products on delete cascade,

    calculated_flow_rate numeric(14, 6) not null,
    actual_flow_rate numeric(14, 6) not null,
    is_external boolean default false not null
);

alter table app.workflow_edges
    add column product_node_id integer
        references app.workflow_product_nodes on delete set null,
    drop column product_id;
