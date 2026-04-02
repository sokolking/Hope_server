# Battle Server

Сервер пошагового боя: **JWT-аутентификация**, **PostgreSQL** (пользователи, инвентарь, предметы, история боёв и ходов, логи), **HTTP API** для входа в бой, опроса состояния, побега и выхода, **WebSocket `/ws/battle`** для ходов и событий раунда, **WebSocket `/ws/session`** для матчмейкинга (очередь 1v1 / команды, ready-check), спектаторов и запроса профиля. Есть **встроенные HTML-админки** (`/db`, `/users`, `/items`, `/logs` и др.) и **экспорт/импорт** снапшота БД по секретному ключу.

Требуется **.NET 8 SDK**.

## PostgreSQL в Docker

Поднять контейнер из корня репозитория:

```bash
cd "Директория проекта"
docker compose up -d
```

По умолчанию сервер ожидает БД с параметрами:

- host: `localhost`
- port: `55432`
- database: `battle_server`
- user: `battle_user`
- password: `battle_password`

Строка подключения задаётся в `appsettings.json` (`ConnectionStrings:BattleDatabase`) и может быть переопределена переменной окружения **`BATTLE_DB_CONNECTION_STRING`**.

### Накатить схему и данные из дампа

После первого запуска контейнера примените файл **`base_alpha1.sql`** (создаёт таблицы и начальные данные). Пример, если контейнер называется `battle-postgres`:

```bash
docker exec -i battle-postgres psql -U battle_user -d battle_server < base_alpha1.sql
```

Перед повторным применением на непустой базе сделайте резервную копию: дамп **пересоздаёт** ряд объектов (`DROP TABLE` и т.д.).

## Запуск сервера

Перейдите в каталог проекта и выполните:

```bash
dotnet run
```

Сервер слушает **http://localhost:5000** (см. `Kestrel:Endpoints` в `appsettings.json`).

Статика раздаётся из каталога **`wwwroot`** (при необходимости путь настраивается через `StaticFiles:WebRoot`).

---

## API

Ниже перечислены основные маршруты. Для защищённых эндпоинтов используйте заголовок **`Authorization: Bearer <JWT>`**, если не указано иное.

### Клиент

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/api/client/version` | Версии клиента и ссылки на загрузки (из секции `Client` в конфиге). |

### Аутентификация

| Метод | Путь | Описание |
|-------|------|----------|
| POST | `/api/auth/login` | Логин по `username` / `password`. Ответ: `accessToken`, данные пользователя, при наличии — снимок незавершённого боя. Новый вход отзывает предыдущую сессию (JWT и связанные WebSocket). |

### Бой (HTTP)

Все маршруты ниже требуют JWT, кроме явно отмеченных.

| Метод | Путь | Описание |
|-------|------|----------|
| POST | `/api/battle/join` | Вход в бой или постановка в очередь. Тело: `startCol`, `startRow`, опционально `solo` (одиночный бой с мобом). Учитывается профиль и экипировка из БД. |
| GET | `/api/battle/{battleId}` | Срез состояния боя: раунд, дедлайн, участники, массивы спавна и т.д. |
| GET | `/api/battle/{battleId}/poll?playerId=...` | Ожидание второго игрока в классическом 1v1: `waiting` / `battle`, при старте — `battleStarted`. |
| POST | `/api/battle/{battleId}/leave?playerId=...` | Выход игрока из боя или отмена ожидания в очереди. |
| POST | `/api/battle/{battleId}/escape?playerId=...` | Начать побег с границы поля (механика «escape»). |
| POST | `/api/battle/{battleId}/equip-weapon` | Смена оружия/медикамента в бою по `itemId` для своего `playerId` (проверка инвентаря в БД). |
| GET | `/api/battle/{battleId}/turns/{turnId}` | Загрузка сохранённого хода из PostgreSQL по `turnId` (проверка принадлежности бою). |

### WebSocket боя: `/ws/battle`

Подключение с query-параметрами: `battleId`, `playerId`, при необходимости `spectator`, токен — `access_token` или заголовок `Authorization: Bearer`.

По сокету клиент отправляет JSON с ходом (в т.ч. `submitTurn`); сервер отвечает подтверждениями и после закрытия раунда рассылает события (в т.ч. `roundResolved` с результатом и дедлайном следующего раунда).

### WebSocket сессии: `/ws/session`

Долгоживущий канал с тем же JWT. Обрабатываются JSON-сообщения с полем `type`, в том числе:

- `queueJoin` / `queueLeave` / `readyCheckConfirm` — матчмейкинг (режим в `mode`, при ready-check — `readyCheckId`);
- `profileRequest` — профиль прогресса пользователя;
- `spectatorListRequest` / `spectatorWatchRequest` — список боёв для наблюдения и просмотр конкретного боя.

### Пользователь и инвентарь (JWT)

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/api/db/user/profile` | Профиль по токену. |
| GET | `/api/db/user/items` | Сетка инвентаря (слоты) для текущего пользователя. |
| POST | `/api/db/user/equip-weapon` | Смена экипировки вне боя (MainScene): `itemId` оружия или медикамента. |

### Профиль по имени (без токена)

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/api/db/user/profile/{username}` | Публичный срез профиля по имени пользователя. |

### Админка и данные в PostgreSQL (часть без JWT — для локальной отладки)

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/api/db/battles` | Список боёв (`take`). |
| GET | `/api/db/battles/{battleId}/turns` | Ходы боя. |
| GET | `/api/db/turns/{turnId}` | Детальный ход по id. |
| GET | `/api/db/users` | Список пользователей для админки. |
| PUT | `/api/db/users` | Обновление пользователя. |
| POST | `/api/db/users/{userId}/debug-hp` | Отладочная установка HP. |
| GET | `/api/db/users/{userId}/items` | Предметы пользователя (админ). |
| PUT | `/api/db/users/{userId}/items` | Замена предметов пользователя (админ). |
| GET | `/api/db/weapons` | Каталог оружия. |
| GET | `/api/db/medicine` | Каталог медикаментов. |
| GET | `/api/db/weapons/meta` | Мета по оружию (категории и т.п.). |
| GET | `/api/db/items/catalog` | Унифицированный каталог предметов; query: `take`, `itemType`, `weaponCategory`, `q`. |
| GET | `/api/db/items/catalog/next-id` | Следующий id для нового предмета. |
| POST | `/api/db/items/catalog` | Создание/обновление предмета в каталоге. |
| DELETE | `/api/db/items/catalog/{itemId}` | Удаление предмета. |
| GET | `/api/db/items/catalog/export` | Экспорт каталога JSON. |
| POST | `/api/db/items/catalog/import` | Импорт каталога JSON. |
| GET | `/api/db/body-parts` | Части тела для боевой логики. |
| GET | `/api/db/obstacle-balance` | Настройки баланса препятствий. |
| PUT | `/api/db/obstacle-balance` | Обновление баланса препятствий. |
| GET | `/api/db/zone-shrink` | Настройки сжатия зоны. |
| PUT | `/api/db/zone-shrink` | Обновление сжатия зоны. |

### Резервное копирование БД

Требуется секрет из конфига `DatabaseBackup:Secret` — заголовок **`X-Database-Backup-Key`** или query-параметр с тем же значением.

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/api/db/backup/export` | Выгрузка JSON-снимка базы. |
| POST | `/api/db/backup/import` | Загрузка JSON-снимка (большие тела — настройте лимит на reverse proxy). |

### Логи

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/api/logs/recent` | Последние записи из таблицы `server_logs` (`take`). |
| GET | `/api/logs/stream` | Server-Sent Events: поток новых логов из памяти после подключения. |

### HTML-страницы (админка и справка)

| GET | Путь |
|-----|------|
| `/logs` | Просмотр логов |
| `/db` | Бои |
| `/users` | Пользователи |
| `/items` | Предметы |
| `/obstacle-balance` | Баланс препятствий |
| `/zone-shrink` | Сжатие зоны |
| `/alpha-guide` | Альфа-гайд |
| `/hit_formula.html` | Формула попадания |
