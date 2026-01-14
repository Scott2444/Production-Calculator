drop table app.recipe_product_inputs;
drop table app.recipe_product_outputs;

create table app.recipe_products
(
    recipe_product_id serial primary key,
    recipe_id integer not null
        constraint fk_recipe
            references app.recipes
            on delete cascade,
    product_id integer not null
        constraint fk_product
            references app.products
            on delete cascade,
    quantity numeric(10, 2) not null,
    is_input boolean not null
);