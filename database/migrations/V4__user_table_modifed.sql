ALTER TABLE app.users
ADD last_updated timestamp with time zone default now() not null;
