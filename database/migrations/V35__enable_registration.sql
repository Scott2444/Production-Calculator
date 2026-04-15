create table app.registration_settings
(
	settings_id integer primary key
		constraint registration_settings_single_row_check check (settings_id = 1),
	is_registration_enabled boolean not null default true,
	last_updated timestamp with time zone not null default now()
);

insert into app.registration_settings (settings_id, is_registration_enabled)
values (1, true)
on conflict (settings_id) do nothing;
