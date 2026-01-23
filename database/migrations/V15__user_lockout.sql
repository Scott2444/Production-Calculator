alter table app.users
    add column failed_login_attempts integer default 0 not null,
    add column lockout_until timestamp with time zone;
    