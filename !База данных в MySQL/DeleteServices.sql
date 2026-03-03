-- пример удаления данных в таблицах
use saloonBeauty;
DELETE FROM services WHERE servCode > 19;

-- удалить базу данных saloonBeauty
use saloonBeauty;
drop database if exists saloonBeauty;