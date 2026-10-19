# SecureLab starter: трекер інцидентів

Це baseline starter для дисципліни «Прикладні технології програмування в
інформаційній безпеці», варіант **2-A**. Він містить один простий
трирівневий моноліт, локальний PostgreSQL, мінімальний Vanilla JavaScript
клієнт, відтворювані seed/reset і готовий `WebApplicationFactory` test harness.

Це накопичуваний проєкт із розширенням **ЛР 1** та реалізацією **ЛР 2** на рівень «Добре». Звіт ЛР 2 і докази: [docs/report-lr02.md](docs/report-lr02.md). Далі студент послідовно розвиває власний проєкт:
результат завершеної ЛР стає основою наступної. Якщо попередню роботу технічно
не завершено, її потрібно завершити або виправити перед переходом далі.

Початковий starter не містив завершених рішень ЛР 1–6. У цій копії реалізовано
розширення ЛР 1, контракт створення інцидентів та виправлення навчального пошуку ЛР 2. Проєкт не містить автентифікації
або авторизації. Уразливий стан пошуку збережено в історії для порівняння. Потрібні для конкретної роботи зміни
студент виконує у власному проєкті за методичними вказівками й лише у
дозволеному локальному середовищі.

## 1. Передумови

- стабільна .NET 10 SDK; `global.json` обирає найвищу встановлену стабільну SDK 10.0 через `latestFeature`;
- Docker із підтримкою `docker compose`;
- Git;
- вільні локальні порти `5080` і `54329`.

Перевірте:

```bash
dotnet --version
docker --version
docker compose version
```

## 2. Перший запуск

У корені starter виконайте:

```bash
docker compose --env-file infra/.env.example -f infra/compose.yaml up -d --wait
dotnet tool restore
dotnet restore SecureLab.sln
dotnet run --project src/SecureLab.Api
```

Під час першого Development-запуску API застосує готову EF Core migration і
додасть фіксовані навчальні дані. Відкрийте:

- browser client: <http://localhost:5080/>;
- health/readiness: <http://localhost:5080/health>;
- інтерактивний OpenAPI: <http://localhost:5080/scalar/v1>;
- OpenAPI JSON: <http://localhost:5080/openapi/v1.json>.

Зупиніть API через `Ctrl+C`. Контейнер БД зупиняється командою:

```bash
docker compose --env-file infra/.env.example -f infra/compose.yaml down
```

## 3. Відомий початковий стан

Seed ЛР 2 містить Alice, Bob, Morgan (Analyst) та Admin, п'ять інцидентів, два
публічні коментарі й одну зміну статусу. UUID не випадкові: їх можна
використовувати у `.http`-сценаріях і тестах.

Scaffold ЛР 2 додає до трьох початкових інцидентів два записи Low для USB та O'Brien. Підсумок: Low=3, Medium=1, High=1, Critical=0.

Локальні значення `securelab / local-study-password` є лише відкритими
навчальними credentials для БД, прив'язаної до `127.0.0.1`. Це не секрет і
його заборонено повторно використовувати для іншої БД чи середовища. Реальний
connection string передавайте поза Git через `ConnectionStrings__SecureLab`.

Щоб застосувати наявні migrations, **очистити лише відомі навчальні таблиці**
локальної БД і повернути seed до відомого стану:

```bash
dotnet run --project src/SecureLab.Api -- --reset-database
```

Команда навмисно відмовляється працювати поза явно налаштованим Development
environment. Вона знищує поточні локальні навчальні дані у відомих таблицях
БД `securelab`, але не видаляє саму БД або її схему.

## 4. Перевірка

Коли PostgreSQL уже працює:

```bash
dotnet build src/SecureLab.Api/SecureLab.Api.csproj --configuration Release
dotnet test tests/SecureLab.Api.Tests/SecureLab.Api.Tests.csproj --configuration Release
```

На macOS/Linux або у Git Bash увесь сценарій підняття PostgreSQL і тестів
виконує одна команда:

```bash
bash scripts/test.sh
```

HTTP-сценарії для ручної перевірки розташовані у `tests/http/incidents.http`.
Вони охоплюють `200`, `400`, `404` і чотири summary-сценарії.

Сценарії ЛР 2: `tests/http/lab-02-checks.http` (створення/валідація) та
`tests/http/lab-02-security.http` (пошук до/після, allowlist і literal substring).
Перед повторним ручним POST змініть унікальний title або відновіть навчальний seed.
Власні автоматичні тести прибирають створені ними записи в finally.
Фінальний перевірений результат ЛР 2: **54 passed, 0 failed, 0 skipped**.

## 5. Розширення ЛР 1

`GET /api/incidents/severity-summary` повертає JSON-масив із лише
`severity` та `count`. Повертаються всі чотири рівні в порядку
Low → Medium → High → Critical; відсутні групи мають count: 0.
Необов'язковий `status` допускає точні назви New, Triaged, InProgress,
Resolved, Closed. Інші значення дають 400 Validation Problem Details.

У клієнті виберіть статус підсумку й натисніть «Показати підсумок».
У ЛР 1 seed без фільтра давав Low/Medium/High по 1 і Critical: 0; після scaffold ЛР 2 Low=3. Triaged — Medium: 1
та нулі для решти; Resolved — всі рівні з нулями і «Даних немає».
Загальний лічильник показує суму інцидентів, а не кількість категорій. UI має стани завантаження та безпечної помилки.
Дані відображаються через textContent. Схема БД залишилася без змін.

Перевірки запускаються тією самою командою `dotnet test` із розділу 4.
У ЛР 1 до чотирьох baseline-тестів додано шість summary-перевірок (усього 10); поточна ЛР 2 містить 54 тестові випадки.
Очікування не замінюють фактичний результат запуску тестів.

## 6. Структура

```text
src/SecureLab.Api/
  Presentation/   endpoints і зовнішні response contracts
  Application/    сценарії застосунку
  Data/           DbContext, entities, migration, seed/reset
  Client/         HTML/CSS/Vanilla JavaScript
tests/
  SecureLab.Api.Tests/   інтеграційні й security regression приклади
  http/                  ручні HTTP-сценарії
infra/                   локальний PostgreSQL
docs/                    карта архітектури, варіант, матриця доступу, звіт
```

Це не Clean/Onion Architecture, DDD, мікросервіси або вимога TDD. Каталоги
позначають відповідальність коду в одному server project.

## 7. Типові проблеми запуску

| Ознака | Перевірка |
|---|---|
| `connection refused` до PostgreSQL | `docker compose ... ps`, порт `54329`, стан healthcheck |
| SDK не знайдено | `dotnet --version` має повернути найвищу встановлену стабільну SDK `10.0` |
| порт API зайнятий | зупиніть інший процес на `5080` або задайте `ASPNETCORE_URLS` |
| migration не застосовується | перевірте `ASPNETCORE_ENVIRONMENT=Development` і connection string |
| змінили credentials у compose | узгоджено перевизначте `ConnectionStrings__SecureLab`; не записуйте реальне значення до Git |

Перед commit перегляньте `git status`, `git diff --staged` і переконайтеся, що
до Git не потрапили `.env`, cookies, tokens, реальні passwords, dumps або logs.

## 8. Розширення ЛР 2

- `POST /api/incidents`: окремі request/response DTO, required і довжини 160/4000 до Trim,
  допустимий enum, обов'язковий момент часу не більш ніж на 5 хвилин у майбутньому, запис у UTC.
- Для High/Critical опис після Trim має містити від 40 символів.
- Повторний title після Trim із тим самим регістром і не-Closed інцидентом дає 409.
- Id, власник Alice, статус New і службові часи задає сервер.
- `GET /api/incidents/search?q=...`: LINQ/ILike у Title і Description, буквальні %, _ та backslash.
- sortBy: createdAtUtc (default, новіші першими), severity (Critical → Low), status (New → Closed).
  Невідомий sortBy дає 400; другий ключ Id, потім максимум 50 результатів.
- Помилки полів: 400 Problem Details; предметний конфлікт: 409; відсутній ресурс: 404.

Відтворення лише локальне, зі штучними даними. Пояснення CP-01…CP-03,
HTTP-докази до/після, A-01, результати тестів та обмеження наведено у звіті.
Git-оформлення та подання виконує студент.
