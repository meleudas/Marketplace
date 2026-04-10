# Architecture Decision Matrix

Практичний додаток до `ARCHITECTURE_AND_REQUEST_FLOW.md`: куди саме класти новий код і як не змішувати відповідальності між шарами.

---

## 1) Швидка матриця рішень

| Якщо ти робиш... | Куди класти | Чого уникати |
|---|---|---|
| HTTP route, статуси, `ProblemDetails`, cookies | `API/Controllers`, `API/Extensions` | Бізнес-правил у контролері |
| Новий сценарій запису (create/update/delete/approve) | `Application/*/Commands` | Прямих SQL/EF-запитів у handler |
| Новий сценарій читання | `Application/*/Queries` | Зміни стану в query-handler |
| Правила формату/обов'язковості полів | `*CommandValidator` / `*QueryValidator` | Переносу доменних інваріантів у validator |
| Бізнес-інваріанти сутності | `Domain/Entities` / `ValueObjects` | Логіки в `Repository`/`Controller` |
| Зовнішній сервіс (email, sms, oauth, cache, identity) | `Application/Ports` + реалізація в `Infrastructure` | Виклику SendGrid/Twilio прямо з Application |
| Робота з БД (EF, міграції, конфіги, mapping record<->domain) | `Infrastructure/Persistence` | Залежності `Domain` від EF |
| Cross-cutting пайплайн (валідація, логування запиту use-case) | `Application/Common/Behaviors` | Дублювання у кожному handler |
| Фонові задачі/джоби | `Infrastructure/Jobs` + реєстрація в `Program`/DI | Бізнес-оркестрація в `Program.cs` |

---

## 2) Правила по шарах (коротко)

## API
- Приймає HTTP, дістає `userId/roles`, викликає `ISender.Send(...)`.
- Повертає `IActionResult` через `ResultExtensions`.
- Не приймає рішень типу “можна/не можна змінювати сутність” — це не його зона.

## Application
- Оркеструє use-case.
- Викликає доменні методи та порти/репозиторії.
- Не повинен знати про `DbContext`, `HttpContext`, конкретні SDK інтеграцій.

## Domain
- Містить бізнес-сенс: стани, інваріанти, transitions.
- Не залежить від ASP.NET/EF/Redis/SendGrid.
- Якщо правило важливе для коректності бізнесу — воно має бути тут.

## Infrastructure
- Реалізує порти та репозиторії.
- Несе відповідальність за технічну частину: SQL, кеш, Identity, мережа.
- Не повинен “вигадувати” нові бізнес-правила.

---

## 3) Validator vs Domain: де чия межа

Використовуй просте правило:

- **Validator**: “чи запит технічно валідний?”
  - обов'язкові поля
  - довжини
  - базові діапазони
- **Domain**: “чи бізнес-операція допустима?”
  - заборона списання понад available stock
  - заборона демоуту останнього owner
  - заборона зміни видаленої сутності

Якщо видалити API і викликати домен напряму — бізнес має лишатися захищеним.

---

## 4) Commands vs Queries (CQRS)

Використовуй:

- **Command**, коли є зміна стану.
- **Query**, коли тільки читання.

Антипатерни:
- `GET` endpoint, який всередині робить update.
- Query-handler, який викликає `UpdateAsync`.
- Command-handler, який повертає “пів-схему аналітики” з важкими join (краще read-модель через Query).

---

## 5) Ports & Adapters (Hexagonal) чекліст

Коли додаєш інтеграцію:

1. Описати інтерфейс у `Application/Ports`.
2. Використовувати його в handler/service.
3. Реалізувати у `Infrastructure`.
4. Зареєструвати в DI.

Якщо в `Application` з’явився `using SendGrid` або `using Microsoft.EntityFrameworkCore` — це сигнал, що межу порушено.

---

## 6) Коли створювати Service, а коли Handler

- Якщо це один чіткий use-case з одним входом — роби `Command/Query + Handler`.
- Якщо це набір дрібних read-операцій для одного контролера і без складної оркестрації — допустимий `Application Service`.
- Якщо сервіс почав рости в “бог-об'єкт” — декомпозуй на CQRS use-cases.

---

## 7) Стандартний потік зміни (best practice)

1. Додай/онови доменну поведінку.
2. Додай команду/запит + validator + handler.
3. Розшир порти/репозиторії за потреби.
4. Реалізуй у Infrastructure.
5. Додай endpoint в API.
6. Додай unit/integration тести.
7. Онови docs.

---

## 8) Типові антипатерни в layered-style і як їх уникати

- **Fat Controller**: контролер знає бізнес-правила.  
  Рішення: усе в handler/domain.

- **God Service**: один `Service` містить весь модуль.  
  Рішення: CQRS use-cases.

- **Anemic Domain**: сутність лише з полями.  
  Рішення: інваріанти та state transitions в Domain.

- **Smart Repository**: репозиторій вирішує бізнес-правила.  
  Рішення: репозиторій тільки persistence.

- **DTO-as-Entity**: API DTO протікає в Domain без мапінгу.  
  Рішення: явні mapping-и.

---

## 9) Definition of Done для нового use-case

- Є `Command/Query` + `Handler`.
- Є `Validator`.
- Бізнес-інваріанти захищені в Domain.
- Нема залежності Application від Infrastructure деталей.
- API повертає узгоджений `Result -> ActionResult`.
- Додано тести (мінімум: happy path + critical negative case).
- Документацію оновлено.

