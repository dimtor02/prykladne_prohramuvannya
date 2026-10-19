# A-01. Огляд raw-SQL місць

Огляд виконано 19.10.2026 після виправлення пошуку. Обсяг для рівня «Добре»: пошук подібних raw-SQL місць у src/tests і висновок щодо кожного збігу. Каталоги bin/obj виключені.

Використано наданий `resources/copyable/data-access-scan.py` з розпакованого scaffold. Додатковий пошук охопив FromSql, SqlQuery, ExecuteSql, DbCommand, NpgsqlCommand, CreateCommand, CommandText, ExecuteReader, ExecuteNonQuery та ExecuteScalar.

| Файл / метод | Збіг / категорія | Висновок |
|---|---|---|
| `src/SecureLab.Api/Data/DatabaseBootstrap.cs`, InitializeAsync | ExecuteSqlRawAsync; службовий reset | SQL є сталою командою з фіксованими іменами навчальних таблиць. Користувацькі q/sortBy та HTTP-поля не приєднуються. Виконання reset потребує Development і Database:AllowReset. Це свідомо руйнівна службова операція навчального стенда, а не динамічний пошук; підстав SQLi у цьому місці не виявлено |
| `tests/SecureLab.Api.Tests/IncidentEndpointTests.cs`, GetSeveritySummary_ReturnsOnlyOrderedAggregateFields | Шаблон `+ query` | Хибний позитивний збіг: складається URL тестового HTTP-запиту, не SQL. Значення query задані InlineData |

Розширений пошук інших raw-SQL/ADO.NET викликів не виявив. Збіг із ExecuteSqlRawAsync повторно вказав на той самий reset.

Додатково прочитано точки, пов'язані з виправленням:

| Файл / метод | Спосіб доступу | Висновок |
|---|---|---|
| `Application/Incidents/IncidentQueries.cs`, SearchAsync | LINQ, параметризований ILike, типізований OrderBy/ThenBy/Take | Значення pattern відокремлено від SQL-структури. %, _ та backslash екрануються для буквального substring. Сортування обирає один із трьох наперед заданих виразів |
| `Scaffolding/Lab02Endpoints.cs`, SearchIncidentsAsync | Allowlist до виклику query | Невідомий sortBy повертає 400 і не передається в запит до БД |
| `Scaffolding/Lab02Endpoints.cs`, CreateIncidentAsync | AnyAsync та SaveChangesAsync | Title порівнюється параметризованим LINQ; entity створюється з явним призначенням дозволених і серверних полів. Залишається зазначене у звіті обмеження конкурентного створення |
| `Application/Incidents/IncidentQueries.cs`, GetListAsync/GetDetailsAsync/GetSeveritySummaryAsync | Типізований LINQ та DTO-проєкції | Клієнтські значення не конкатенуються з SQL. Summary виконує агрегацію на стороні БД |
| `Data/DbSeeder.cs`, SeedAsync; `Scaffolding/Lab02Seed.cs`, EnsureAsync | EF Core, сталі навчальні дані | Зовнішнє введення не формує SQL-структуру |

Шляхи в додатковій таблиці наведено від `src/SecureLab.Api/`. Це огляд рівня «Добре», а не твердження про повний аудит безпеки застосунку. Авторизація та production-налаштування цим оглядом не оцінюються.
