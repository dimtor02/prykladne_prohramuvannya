# Лабораторна робота № 3. Автентифікація та безпечна робота з обліковими даними

**Технічну частину рівня «Добре» виконано: збірка без помилок, два reset з 80/80 успішними тестами після кожного, реальний HTTP flow та браузерний сценарій успішні.**

## 1. Відтворення

- Автор: Торонченко Дмитро, КБ-41; baseline 2-A «Трекер інцидентів».
- Обраний рівень: «Добре»; поглиблення «Відмінно» не входять до обраного рівня.
- Репозиторій: https://github.com/dimtor02/prykladne_prohramuvannya.
- Гілка: `lab/3-authentication`.
- Base tag: `v0.2.0`, commit `23d2b36c1af2995d6e66d40ef97ff98ead40c178`.
- Scaffold: `lab-03-start-v1`, [квитанція](../.scaffolds/lab-03.json).
- `base_commit` і `entry_parent`: `23d2b36c1af2995d6e66d40ef97ff98ead40c178`.
- Окремий scaffold commit: `386d1e6`.
- Перевірений commit власної реалізації: `26d82c615b19385d6a3104c3117d550092292df5` ([код](https://github.com/dimtor02/prykladne_prohramuvannya/commit/26d82c615b19385d6a3104c3117d550092292df5);.
- Windows, .NET 10 SDK; збірка Release. Успадкований стан до scaffold: 54 успішні тести.
- Після scaffold було запущено два reset і повний suite після кожного: **55/55 успішних у кожному**, без невдалих і пропущених.
- Після останньої зміни коду: **збірка Release з 0 помилок і 0 попереджень; два reset, після кожного 80 passed, 0 failed, 0 skipped**. Це 54 успадковані випадки ЛР2, 1 scaffold mechanics і 25 власних auth/security випадків.
- Реальний HTTP flow: anonymous + Bob header → 401; Alice login → 204; me із cookie Alice та Bob header → 200, Alice; create без owner → 201, ownerDisplayName Alice; logout → 204; повторний me тим самим клієнтом → 401. Власний HTTP-запис після перевірки видалено, інші записи не змінювалися.
- Браузер: register 201; Alice login 204 і profile 200; create 201; logout 204 і повторний profile 401, профіль та результати очищено. Console не містив повідомлень. Збережено чотири скриншоти без credentials. Було введено паролі; Заповнено решту синтетичних даних і виконано create/logout.
- Після браузерного прогону видалено лише його окремий synthetic account і створений інцидент. Збережений owner інциденту перед видаленням — AliceId. Підсумок локальної БД знову 4 seed users і 5 seed incidents; нових reset після фінального suite не виконувалося.
- Перший непідсумковий suite дав 79 passed, 1 failed через пізню конфігурацію Production-тесту. Налаштування перенесено в ранні host settings; окремий повтор і обидва фінальні suite успішні.

Застосунок працює лише з локальною навчальною PostgreSQL і синтетичними даними. Reset застосовує надану migration, очищує визначені доменні та Identity-таблиці й повторює seed. Пароль передається через `SeedUsers__Password`, без збереження у Git. Перший невдалий reset через відсутню велику латинську літеру не зараховано; після виправлення пароля обидва повні прогони успішні.

Команди з кореня проєкту, у терміналі із заданим локальним паролем; API перед reset зупинений:

```powershell
dotnet build SecureLab.sln -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
dotnet run --project src/SecureLab.Api -c Release --no-build --no-restore -- --reset-database
dotnet test tests/SecureLab.Api.Tests -c Release --no-build --no-restore
```

## 2. Власна реалізація та CP-01

[RegisterRequest, LoginRequest і UserProfileResponse](../src/SecureLab.Api/Presentation/Contracts/AuthContracts.cs) відокремлюють зовнішній контракт від ApplicationUser. DTO реєстрації приймає лише userName, email, displayName, password. Невідомі id/role/passwordHash не використовуються. Сервер генерує Guid та призначає лише Reporter; створення користувача й призначення ролі виконуються в одній транзакції.

| Поле | Правило |
|---|---|
| userName | Обов’язкове, до 256 символів до Trim; після Trim лише латинські літери, цифри та -._@+ |
| email | Обов’язкове, до 256 символів до Trim; Trim; MailAddress.TryCreate, без display-name обгортки |
| displayName | Обов’язкове, до 120 символів до Trim; Trim, без керівних символів |
| password | 10–128 символів; без Trim; стандартні вимоги Identity до великої/малої латинських літер, цифри та спецсимволу |

Хибний формат/довжина/пароль дають 400 ValidationProblem із ключем поля, без введеного значення. DuplicateUserName або DuplicateEmail дають однаковий 409 Problem Details; нормалізацію регістру здійснює Identity. Успіх — 201, Location /api/me, лише id/userName/displayName. UserManager.CreateAsync виконує стандартне хешування, код не обчислює PasswordHash самостійно.

UserManager перевіряє унікальність email, але наданий EmailIndex не є унікальним обмеженням БД. Послідовний duplicate-контракт перевіряється тестом; абсолютна гарантія email-унікальності за конкурентних register потребує додаткового обмеження схеми й не заявляється. Username має унікальний індекс; очікувана unique violation перетворюється на той самий 409.

## 3. Маршрут довіри та CP-02/CP-03

Login JSON → LoginRequest → SignInManager.PasswordSignInAsync → стандартний Identity verifier → захищена cookie Identity.Application → автоматичне надсилання браузером → cookie authentication handler → claims у HttpContext.User → RequireAuthorization → NameIdentifier як джерело власника.

Наданий /api/me довіряв X-Demo-UserId, без header повертав 501; create призначав сталого власника Alice. Початковий дефект установлено оглядом scaffold-коду, без заяви про окремий HTTP-доказ стану «до». У власній реалізації /api/me захищений RequireAuthorization і знаходить користувача лише за principal через UserManager.GetUserAsync. Повертає явний UserProfileResponse. Header іншого користувача не читається.

POST /api/incidents також має RequireAuthorization. Handler читає Guid із ClaimTypes.NameIdentifier, перевіряє наявність такого Identity user і встановлює OwnerUserId. Поле ownerUserId не входить до CreateIncidentRequest, тому ігнорується. Перевірки ЛР2 на 201/400/409, UTC та cross-field межу збережено; їхні клієнти тепер входять як Alice. Для A-04/A-05 виконавець — Bob, а підроблений owner — Alice; тест перевіряє саме збережений OwnerUserId.

Login приймає userName, а не email; обов’язкові непорожні userName/password, довжини до 256/128. Username обрізається, password не змінюється. Невідомий user та неправильний password дають однакові 401, application/problem+json і стабільні type/title/status/detail. Trace id може відрізнятися. Рівність часу відповіді не заявляється.

Logout викликає SignInManager.SignOutAsync і повертає 204; той самий cookie client після видалення основної cookie має одержати 401. Це вихід поточного клієнта, а не гарантія відкликання скопійованих квитків чи всіх сеансів.

## 4. Зберігання пароля та cookie

Єдина модель — ApplicationUser : IdentityUser<Guid>, таблиця identity_users. Вона має PasswordHash text і не має відкритого Password; StudyUser не зареєстрований як другий EF store. Доменні Guid-зв’язки вказують на ApplicationUser. Чотири seed Guid, ролі Reporter/Analyst/Administrator та власники Alice/Bob збережені наданою migration/seed.

Стандартний password hasher використовує випадкову сіль і навмисно дорогий PBKDF2, а не швидкий SHA-256. UserManager.CreateAsync зберігає представлення, PasswordSignInAsync перевіряє введений пароль через стандартний verifier. У тестах перевіряється відмінність солених представлень для чотирьох акаунтів з однаковим навчальним паролем, без друку значень.

Фіксований профіль у AddLab03Identity: HttpOnly=true, SameSite=Lax, у Development SecurePolicy=SameAsRequest, поза Development — Always. Login непостійний: cookie сеансова; стандартний строк квитка Identity — 14 днів зі sliding expiration. Строк спеціально не змінено. Закриття браузера не замінює logout, зокрема за відновлення браузерного сеансу.

HttpOnly обмежує читання cookie через JavaScript; Secure — передавання через HTTPS; Lax — частину cross-site надсилань. Локальний HTTP не доводить Secure. Тест фактичних атрибутів використовує HTTPS TestServer для Development і Production; це не доказ налаштованого production TLS. Redirect handlers повертають 401/403 замість HTML redirect; Bearer-схема не додається.

## 5. Обмеження входів, події та залишкові ризики

Лише login має fixed-window limiter: **10 запитів за 60 секунд**, QueueLimit=0. Ключ — transport RemoteIpAddress або unknown у TestServer. Довільний X-Forwarded-For не враховується. Це не дозволяє обійти квоту зміною введеного userName, але спільний NAT об’єднує користувачів. Лічильник у пам’яті одного host; це не розподілений захист, перезапуск його очищує. 429 має загальний Problem Details і Retry-After.

Тест реального login перевіряє стандартні 10 запитів та 429 на одинадцятому; окремий host із тестовими options 3 запити/3 секунди перевіряє спільну квоту двох клієнтів, неможливість обходу чужим forwarding header, незалежний новий host та 204 після завершення вікна. Скорочене тестове вікно не змінює стандартні налаштування застосунку.

Authentication events: eventType, UTC time, result, subject (anonymous/authenticated), traceId. Свідомо не журналюються введене ім’я/email, password, hash, cookie, token, raw Authorization або request body. Події success/failure/logout та rate_limited дають мінімальний контекст без credentials; тест перехоплює лише власну категорію журналювання й не друкує секретних артефактів.

Канали account enumeration: register із 409 прямо розкриває зайнятість імені/email — навчальний компроміс послідовного duplicate-контракту. Register поки без rate limit; abuse-monitoring і rate limiting реєстрації залишаються можливими контрзаходами. Login має однакову зовнішню відмову та IP-quota, але різниця часу виконання й розподілені спроби залишаються ризиком.

SameSite=Lax, HttpOnly та same-origin fetch **не є повною CSRF-моделлю**. Login/register/logout/create не мають antiforgery-перевірки; це явно названий навчальний борг до ЛР6. Також не реалізовано resource authorization ЛР4, масове відкликання сеансів, багатовузловий limiter або повний production deployment.

## 6. Докази і поточний стан

| ID | Передумова/дія | Очікувано | Фактично |
|---|---|---|---|
| T-01, CP-01 | Register; зайві role/id; login нового user | 201, лише безпечний DTO, Reporter, серверний id | Пройдено незалежним тестом |
| T-02 | 11 випадків формату/довжини/policy | 400 із ключем поля, акаунт не створено | Пройдено всі 11 |
| Duplicate | Повтор; те саме ім’я та інший email; інше ім’я та той самий email, з іншим регістром | Послідовний 409 | Пройдено в T-01 |
| T-03 | Модель, чотири seed users і стандартний verifier | Один store, сольовані представлення, сталі Guid/roles/owners | Пройдено в обох фінальних suite |
| A-01 | Без cookie, з/без чужого header; GET me і POST create | 401 без redirect, БД не змінена | Обидва випадки пройдено |
| A-02, CP-02 | Alice login → me | 204 → 200, тільки дозволені поля | Пройдено в обох suite та реальним HTTP |
| A-03 | Alice cookie + Bob header | Профіль Alice незмінний | Пройдено в обох suite та реальним HTTP |
| A-04 | Bob create без owner | 201, збережений owner Bob | Пройдено в обох suite, owner перевірено в БД |
| A-05, CP-03 | Bob create із owner Alice | 201, збережений owner Bob | Пройдено в обох suite, owner перевірено в БД |
| A-06 | Logout тим самим клієнтом → me/create | Cookie видалено, наступні requests 401 | Пройдено в обох suite; me після logout також підтверджено реальним HTTP |
| A-07 | Невідомий user / хибний password | Однакова стабільна відмова | Пройдено в обох suite |
| Cookie flags | HTTPS Development/Production login | Secure/HttpOnly/Lax/Path=/, session | Пройдено в обох suite, значення cookie не друкувалися |
| Rate limit | Стандартні 10 спроб, 11-та; ізольоване вікно й відновлення | 429, потім 204 після вікна | Обидва сценарії пройдено в обох suite |
| S-01, події | DTO/відмови/auth events/storage review | Без credentials і службових Identity полів | Автотести пройдено; код не використовує localStorage/sessionStorage/Console/HTML sinks; runtime Console порожній |
| Browser flow | Anonymous → register → login Alice → profile → create → logout → me | П’ять спостережень етапу 4 | Усі п’ять підтверджено; register 201, login 204, profile 200, create 201, logout 204, повторний profile 401 |

Безпечні результати: [фінальні suite й HTTP flow](evidence/lr03-test-results.md), [браузерний сценарій із чотирма скриншотами](evidence/lr03-browser.md), [історія початкових перевірок](evidence/lr03-evidence.md). Повні diagnostic logs і TRX залишаються поза репозиторієм, до подання переносяться лише обрані перевірені безпечні результати.

## 7. Декларація та журнал ШІ


| OpenAI Codex | Вхідний стан, code assistance
