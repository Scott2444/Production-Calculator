create table app.verification_codes (
  code_id uuid primary key default gen_random_uuid(),
  user_id int not null
    constraint fk_verification_code_user
        references app.users
        on delete cascade,
  code_hash text not null,
  attempts int not null default 0,
  created_at timestamp with time zone not null,
  expires_at timestamp with time zone default now() not null
);
