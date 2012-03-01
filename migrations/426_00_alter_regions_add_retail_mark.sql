alter table Farm.Regions
add column Retail tinyint(1) not null default 0;

update Farm.Regions
set Retail = 1
where Region like '%Справка%';
