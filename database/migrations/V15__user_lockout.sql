create table app.user_lockout
(
    user_id        uuid        not null
        constraint user_lockout_pkey
            primary key,
    failed_attempts_count integer not null default 0,
    lockout_until timestamp with time zone not null
);