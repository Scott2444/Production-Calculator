create table app.projects
(
    project_id  serial primary key,
    user_id integer not null
        constraint fk_user
            references users
            on delete cascade,
    name varchar(255) not null,
    description text,
    created_at timestamp with time zone default now() not null,
    last_updated timestamp with time zone default now() not null
);
