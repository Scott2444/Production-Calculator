create table app.products
(
    product_id  serial primary key,
    project_id integer not null
        constraint fk_project
            references app.projects
            on delete cascade,
    puid char(10) unique not null,
    name varchar(255) not null,
    description text,
    created_at timestamp with time zone default now() not null,
    last_updated timestamp with time zone default now() not null
);

create unique index idx_products_puid on app.products (puid);
