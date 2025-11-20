create table app.roles
(
    role_id   serial primary key,
    role_name varchar(100) not null unique
);

insert into app.roles (role_name) values
('Unverified'),
('User'),
('Admin');

alter table app.users
    add column role_id int references app.roles(role_id) default 1 not null;
