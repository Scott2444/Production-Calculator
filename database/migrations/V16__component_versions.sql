alter table app.recipes
add column version integer default 1 not null;

alter table app.machines
add column version integer default 1 not null;

alter table app.modifiers
add column version integer default 1 not null;