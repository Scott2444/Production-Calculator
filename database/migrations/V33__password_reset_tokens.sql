create table app.password_reset_tokens
(
    reset_id uuid primary key default gen_random_uuid(),
    user_id int not null unique
        constraint fk_password_reset_token_user
            references app.users
            on delete cascade,
    token_hash varchar(64) not null unique,
    created_at timestamp with time zone default now() not null,
    expires_at timestamp with time zone not null
);

create index idx_password_reset_tokens_expires_at on app.password_reset_tokens(expires_at);