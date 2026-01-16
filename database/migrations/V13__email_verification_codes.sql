create table app.email_verification_codes (
  code_id UUID primary key,
  user_id UUID references app.users(user_id) on delete cascade,
  code_hash text not null,
  attempts int not null default 0,
  created_at timestamp with time zone not null,
  expires_at timestamp with time zone default now() not null
);
