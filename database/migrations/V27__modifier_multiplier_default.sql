alter table app.modifiers
    alter column input_multiplier set default 0.00;
alter table app.modifiers
    rename column input_multiplier to input_percent;
alter table app.modifiers
    alter column output_multiplier set default 0.00;
alter table app.modifiers
    rename column output_multiplier to output_percent;
