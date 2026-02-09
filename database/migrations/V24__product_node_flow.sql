alter table app.workflow_product_nodes
    drop column actual_flow_rate,
    add column actual_flow_rate_in numeric(14, 6),
    add column actual_flow_rate_out numeric(14, 6);
