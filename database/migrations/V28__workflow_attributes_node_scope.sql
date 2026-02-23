alter table app.workflow_recipe_attributes
    add column workflow_node_id integer not null,
    add constraint fk_workflow_recipe_attribute_node
        foreign key (workflow_node_id)
            references app.workflow_nodes (workflow_node_id)
            on delete cascade,
    drop column workflow_id,
    drop column recipe_id;

alter table app.workflow_machine_attributes
    add column workflow_node_id integer not null,
    add constraint fk_workflow_machine_attribute_node
        foreign key (workflow_node_id)
            references app.workflow_nodes (workflow_node_id)
            on delete cascade,
    drop column workflow_id,
    drop column machine_id;

alter table app.workflow_modifier_attributes
    add column workflow_node_id integer not null,
    add constraint fk_workflow_modifier_attribute_node
        foreign key (workflow_node_id)
            references app.workflow_nodes (workflow_node_id)
            on delete cascade,
    add column workflow_node_modifier_id integer not null,
    add constraint fk_workflow_modifier_attribute_modifier
        foreign key (workflow_node_modifier_id)
            references app.workflow_node_modifiers (workflow_node_modifier_id)
            on delete cascade,
    drop column workflow_id,
    drop column modifier_id;
