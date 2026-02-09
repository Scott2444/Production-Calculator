alter table app.workflow_product_nodes
    alter column actual_flow_rate_in set not null,
    alter column actual_flow_rate_in set default 0.0,
    alter column actual_flow_rate_out set not null, 
    alter column actual_flow_rate_out set default 0.0;