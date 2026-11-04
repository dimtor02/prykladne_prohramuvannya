# Відтворення браузерної регресії ЛР5

Тест виконує реальний DOM у Chrome/Chromium через Playwright. Це додаткова
автоматизація перевірок рівня «Добре», не заява про виконання всього «Відмінно».
Використовувати лише власний loopback-стенд зі штучними даними.

## Передумови

1. API у Development на `http://localhost:5080`, локальний PostgreSQL, актуальний seed.
2. Навчальні Alice/Bob/Morgan мають відомий локальний пароль. `SeedUsers__Password`
   передається через оточення, не через аргументи, tracked JSON або текст звіту.
3. Node.js, Playwright і Chrome/Chromium. У перевіреному середовищі використано
   Playwright 1.62.1 і Google Chrome 156.0.8078.80. NuGet-залежності не змінюються.
   Playwright можна встановити в окремий каталог поза репозиторієм і передати
   абсолютний шлях пакета через `LAB05_PLAYWRIGHT_PATH`.
4. Два окремі термінали з кореня проєкту:

```powershell
python -m http.server 4173 --bind localhost --directory demonstrations/cors-probe
python -m http.server 4174 --bind localhost --directory demonstrations/cors-probe
```

## Запуск із кореня проєкту

У цьому прикладі значення пароля вводиться приховано й не стає частиною shell history:

```powershell
$env:SeedUsers__Password = [System.Net.NetworkCredential]::new('', (Read-Host 'Локальний seed password' -AsSecureString)).Password
# Лише якщо пакет/браузер не доступні стандартним шляхом:
$env:LAB05_PLAYWRIGHT_PATH = 'ABSOLUTE_PATH_TO_PLAYWRIGHT_PACKAGE'
$env:LAB05_CHROME_PATH = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
node tests/browser/lab05-regression.cjs fixed
node tests/browser/lab05-regression.cjs cors-fixed
node tests/browser/lab05-regression.cjs csp
```

`fixed` створює записи з унікальними назвами, перевіряє лапки/апостроф, `< >`,
Unicode, HTML-like string і рівно 4000 символів; Alice/Morgan читають marker,
Bob отримує 404; Description/comments лишаються текстом; повторний render
коментарів не дублює DOM. Відсутній seed-коментар …0005 спричиняє FAIL.

Marker копіюється з виданого однорядкового файла, точна копія якого збережена
поруч як `lab05-marker.txt`. Він змінює тільки атрибут прихованого контрольного
елемента; credentials/storage не читає. Заздалегідь збережений marker Guid
повторно використовується, поки такий запис існує; після reset створюється новий.

`cors-fixed` запускає fetch із фактичних origins 4173/4174. CDP Network фіксує
OPTIONS, requested method/header, server response та факт фактичного відправлення
GET. Це відрізняє запланований, але заблокований GET від actual request.
Cookie записується лише як `cookiePresent: true/false`, без значення.

`csp` перевіряє чотири сторінки, список/details, старий same-origin iframe helper,
response headers, відсутність CSP-порушень legitimate flow та контрольоване
блокування зображення `http://localhost:4173/x.png` і cross-origin embedding.

## Вихідні файли й повторення

За замовчуванням JSON і PNG записуються в `docs/evidence/lr05`; `LAB05_OUTPUT_DIR`
дозволяє вибрати інший каталог, щоб не перезаписувати попередні докази.
`LAB05_RUN_DIR` задає каталог лише для не секретного fixture Guid; стандартно
використовується системний temp. Пароль до цього каталогу тест не записує.
Кожен login використовує окремий browser context; browser storage не експортується.

Після запуску fixture-записи залишаються в навчальній БД для ручного перегляду.
Повний .NET suite із фіксованими seed-count очікуваннями запускайте після reset,
до browser fixture creation. Не змінюйте очікування тестів, щоб приховати зайві записи.

Історичні режими `normal`, `vulnerable`, `cors-before`, `cors-no-credentials`
призначено для відповідних станів стенда; на фінальному fixed state вони
не повинні всі проходити. Для replay коментаря на vulnerable потрібна тільки
seed-зміна DbSeeder з fixture …0005, без safe DOM/CORS/CSP змін. Це уточнення
також зафіксовано у DEL-01: первинний scaffold commit сам цього seed не містить.

## Прямий HTTP-контроль

```powershell
curl.exe -sS -D - -o NUL -H "Origin: http://localhost:4173" http://localhost:5080/health
curl.exe -sS -D - -o NUL -H "Origin: http://localhost:4174" http://localhost:5080/health
```

Обидва запити мають HTTP 200. Тільки перший отримує ACAO 4173 і ACAC true.
HTTP client не застосовує браузерну CORS policy.
