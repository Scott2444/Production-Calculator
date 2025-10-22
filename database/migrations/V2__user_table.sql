create table app.users
(
    user_id       serial primary key,
    username      varchar(100) not null unique,
    email         varchar(255) not null unique,
    password_hash varchar(255) not null,
    created_at    timestamp with time zone default now() not null
);
