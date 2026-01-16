create table app.modifiers
(
    modifier_id serial primary key,
    project_id integer not null
        constraint fk_project
            references app.projects
            on delete cascade,
    puid char(10) unique not null,
    name varchar(255) not null,
    description text,
    flat_speed_bonus numeric(13, 5) not null default 0.00,
    additive_percent_bonus numeric(13, 5) not null default 0.00,
    multiplicative_modifiers numeric(13, 5) not null default 0.00,
    created_at timestamp with time zone default now() not null,
    last_updated timestamp with time zone default now() not null
);