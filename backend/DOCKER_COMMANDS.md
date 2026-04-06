# Docker команди для Marketplace Backend

Швидкий довідник для роботи з контейнерами у цьому проєкті.  
Виконувати з каталогу `backend/`, де лежить `docker-compose.yml`.

---

## Сервіси в `docker compose`

- `api`
- `postgres`
- `redis`
- `frontend` (лише з профілем `frontend`, див. корінь репозиторію `docker-compose.yml`)

---

## Базові команди

### Підняти все (з білдом)

```powershell
docker compose up -d --build
```

### Підняти тільки інфраструктуру (без API)

```powershell
docker compose up -d postgres redis
```

### Зупинити та прибрати контейнери

```powershell
docker compose down
```

### Зупинити та прибрати контейнери + мережі + volume

```powershell
docker compose down -v
```

### Перевірити стан контейнерів

```powershell
docker compose ps
```

### Логи всіх сервісів

```powershell
docker compose logs -f
```

### Логи конкретного сервісу

```powershell
docker compose logs -f postgres
docker compose logs -f api
docker compose logs -f redis
```

### Рестарт конкретного сервісу

```powershell
docker compose restart api
docker compose restart postgres
```

---

## PostgreSQL: робота з БД

> За `docker-compose.yml`:  
> `POSTGRES_USER=postgres`, `POSTGRES_PASSWORD=postgres`, `POSTGRES_DB=marketplace`.

### Зайти в psql всередині контейнера

```powershell
docker compose exec postgres psql -U postgres -d marketplace
```

### Подивитися таблиці

```powershell
docker compose exec postgres psql -U postgres -d marketplace -c "\dt"
```

### Видалити схему `public` та створити заново (повне очищення БД без видалення volume)

```powershell
docker compose exec postgres psql -U postgres -d marketplace -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"
```

### Видалити і створити саму БД заново

```powershell
docker compose exec postgres psql -U postgres -d postgres -c "DROP DATABASE IF EXISTS marketplace;"
docker compose exec postgres psql -U postgres -d postgres -c "CREATE DATABASE marketplace;"
```

### Повний reset PostgreSQL через volume (найчистіший варіант)

```powershell
docker compose down -v
docker compose up -d postgres
```

---

## Redis: очищення кешу

### Перевірка доступності Redis

```powershell
docker compose exec redis redis-cli ping
```

### Очистити поточну БД Redis

```powershell
docker compose exec redis redis-cli FLUSHDB
```

### Очистити всі БД Redis

```powershell
docker compose exec redis redis-cli FLUSHALL
```

---

## Прибирання Docker ресурсів (обережно)

### Прибрати зупинені контейнери

```powershell
docker container prune -f
```

### Прибрати невикористані image

```powershell
docker image prune -f
```

### Прибрати невикористані volume

```powershell
docker volume prune -f
```

### Агресивне очищення всього невикористаного

```powershell
docker system prune -a --volumes -f
```

---

## Швидкі сценарії

### 1) Перезапуск API після змін

```powershell
docker compose up -d --build api
```

### 2) Повний reset усього середовища

```powershell
docker compose down -v
docker compose up -d --build
```

### 3) Reset тільки PostgreSQL (без Redis)

```powershell
docker compose stop postgres
docker compose rm -f postgres
docker volume rm backend_postgres_data
docker compose up -d postgres
```

> Примітка: назва volume може відрізнятись залежно від імені проєкту Compose.  
> Перевірити можна командою:

```powershell
docker volume ls
```

---

## Seed тестових даних (окрема команда)

Скрипт: `backend/scripts/seed-test-data.sql`  
Запуск (з кореня репозиторію, де `docker-compose.yml`):

```powershell
docker compose --profile tools run --rm db-seed
```

Скрипт очищає і наповнює узгоджені дані для:
- `AspNetUsers`
- `marketplace_users`
- `categories`
- `companies`

Тестові креденшли після seed:
- `admin@marketplace.test` / `Admin123!`
- `seller@marketplace.test` / `Admin123!`
- `buyer@marketplace.test` / `Admin123!`

