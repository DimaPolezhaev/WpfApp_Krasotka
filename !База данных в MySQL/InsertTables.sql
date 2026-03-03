-- выборка из таблицы мастеры
use saloonBeauty;
select * from masters;

-- выборка из таблицы клиенты
use saloonBeauty;
select * from clients;

-- выборка из таблицы услуги
use saloonBeauty;
select * from services;

-- выборка из таблицы типы услуг
use saloonBeauty;
select * from servTypes;

-- выборка из таблицы оказание услуг
use saloonBeauty;
select * from appointments;

-- выборка из 2-х таблиц
use saloonBeauty;
select masterName, servType
from
	masters inner join servTypes on
	masters.servTypeCode = servTypes.servTypeCode;

-- выборка из 4-х таблиц
use saloonBeauty;
select appDate, masterName, servName, clientName
from
	appointments
    inner join masters on appointments.masterCode = masters.masterCode
	inner join services on appointments.servCode = services.servCode
	inner join clients on appointments.clientCode = clients.clientCode;
    
	