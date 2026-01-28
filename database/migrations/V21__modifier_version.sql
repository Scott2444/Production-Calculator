alter table app.workflow_node_modifiers
    add column modifier_version integer not null;

alter table app.workflow_nodes
    add column puid varchar(10) unique not null;

create index idx_workflow_edges_workflow_id
    on app.workflow_edges (workflow_id);
