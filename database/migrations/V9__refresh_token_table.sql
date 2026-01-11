create table app.refresh_tokens
(
    token_id uuid primary key default gen_random_uuid(),
    user_id int not null
        constraint fk_refresh_token_user
            references app.users
            on delete cascade,
    token varchar(256) not null,
    expires_at timestamp with time zone not null,
    created_at timestamp with time zone default now() not null,
    revoked_at timestamp with time zone
);