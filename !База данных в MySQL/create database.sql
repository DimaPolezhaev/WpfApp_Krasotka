-- создать базу данных saloonBeauty
create database if not exists saloonBeauty;

-- создать таблиц для базы данных saloonBeauty
use saloonBeauty;

create table if not exists servTypes
	(
		servTypeCode int primary key,
        servType varchar(15)
	);
create table if not exists services
	(
		servCode int primary key,
        servName varchar(20),
        servPrice int,
        servDuration int,
        servTypeCode int,
        foreign key(servTypeCode) references servTypes (servTypeCode)
	);
create table if not exists masters
	(
		masterCode int primary key,
        masterName varchar(20),
        masterTel varchar(15),
        servTypeCode int,
        foreign key(servTypeCode) references servTypes (servTypeCode)
	);
create table if not exists clients
	(
		clientCode int primary key,
        clientName varchar(20),
        clientTel varchar(15)
	);
create table if not exists appointments
	(
		appCode int primary key,
        masterCode int,
        clientCode int,
        servTypeCode int,
        servCode int,
        queueFrom int,
        queueTo int,
        appDate date,
        foreign key(servTypeCode) references servTypes (servTypeCode),
        foreign key (masterCode) references masters (masterCode),
        foreign key (clientCode) references clients (clientCode),
        foreign key (servCode) references services (servCode)
	);

-- Добавление столбца в таблицу Клиенты
use saloonBeauty;
alter table clients add clientsActivity varchar(5);

-- Добавление столбца в таблицу Мастера
use saloonBeauty;
alter table masters add masterActivity varchar(5);

-- Добавление столбца в таблицу Услуги
use saloonBeauty;
alter table services add serviceActivity varchar(5);

-- Добавление столбца в таблицу Типы услуг
use saloonBeauty;
alter table servTypes add servTypeActivity varchar(5);

-- Добавление столбца в таблицу Оказания услуг
use saloonBeauty;
alter table appointments add AppointmentActivity varchar(5);