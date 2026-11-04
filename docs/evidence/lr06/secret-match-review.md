# Контекстний огляд збігів пошуку секретів

Дата: 04.11.2026. Перелік `git ls-files --cached` експортував користувач; агент Git не викликав. Фактичний корінь: `starter-project`. Перевірено поточні bytes цих 147 paths після змін ЛР6, а не історичний blob початкового commit.

Команда: `python tests/security/scan-listed-files.py PATH_LIST OUTPUT_JSON`. Шаблон: `password|passwd|secret|api.?key`, case-insensitive. 147 файлів інвентаризовано; 129 UTF-8 text проскановано; 18 PNG пропущено текстовим пошуком, OCR не виконувався. 153 збіги. SHA-256 кожного файла, binary paths і точні рядки — [secret-scan.json](secret-scan.json); вхідний список — [tracked-file-list.txt](tracked-file-list.txt).

Нижче кожен text match має окремий рядок і рішення за переглянутим контекстом. Значення credentials не копіюються. Нові, ще не tracked source/test файли оглянуто окремо нижче. Історія, stash, інші клони й CI не перевірені.

| Path:line | Класифікація / рішення |
|---|---|
| `.gitignore:14` | Не секрет: правило/коментар виключення локальних конфігурацій. |
| `README.md:46` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `README.md:47` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `README.md:48` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `README.md:74` | Accepted, study-only: узгоджена відкрита loopback-конфігурація; заборона повторного використання в README. |
| `README.md:164` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/architecture.md:77` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/browser-security-evidence.md:192` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-browser.md:14` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-evidence.md:14` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-evidence.md:15` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-evidence.md:17` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-test-results.md:45` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-test-results.md:66` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-test-results.md:68` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-test-results.md:73` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-test-results.md:103` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-test-results.md:105` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-test-results.md:111` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-test-results.md:113` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-test-results.md:115` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-test-results.md:117` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-test-results.md:121` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr03-test-results.md:130` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/evidence/lr04-test-results.md:293` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/git-workflow.md:124` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/report-lab-04.md:94` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/report-lab-04.md:106` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/report-lr03.md:24` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/report-lr03.md:36` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/report-lr03.md:43` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/report-lr03.md:45` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/report-lr03.md:51` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/report-lr03.md:57` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/report-lr03.md:63` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/report-lr03.md:65` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/report-lr03.md:77` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `docs/report-lr03.md:97` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `infra/compose.yaml:9` | Accepted, study-only: узгоджена відкрита loopback-конфігурація; заборона повторного використання в README. |
| `src/SecureLab.Api/Client/auth-lab.html:30` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Client/auth-lab.html:41` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Client/auth-lab.js:63` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Client/auth-lab.js:71` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Client/auth-lab.js:72` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Client/auth-lab.js:84` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Client/auth-lab.js:85` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/DbSeeder.cs:39` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/DbSeeder.cs:42` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/DbSeeder.cs:45` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/DbSeeder.cs:49` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/DbSeeder.cs:51` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/DbSeeder.cs:53` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/DbSeeder.cs:55` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/DbSeeder.cs:182` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/DbSeeder.cs:187` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/DbSeeder.cs:189` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/DbSeeder.cs:190` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/DbSeeder.cs:208` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Data/Migrations/20260902062130_Lab03Identity.Designer.cs:197` | Не секрет: назва PasswordHash/тип колонки або коментар; значення hash відсутнє. |
| `src/SecureLab.Api/Data/Migrations/20260902062130_Lab03Identity.cs:52` | Не секрет: назва PasswordHash/тип колонки або коментар; значення hash відсутнє. |
| `src/SecureLab.Api/Data/Migrations/20260902062130_Lab03Identity.cs:210` | Не секрет: назва PasswordHash/тип колонки або коментар; значення hash відсутнє. |
| `src/SecureLab.Api/Data/Migrations/SecureLabDbContextModelSnapshot.cs:194` | Не секрет: назва PasswordHash/тип колонки або коментар; значення hash відсутнє. |
| `src/SecureLab.Api/Data/SecureLabDesignTimeDbContextFactory.cs:10` | Accepted, study-only: узгоджена відкрита loopback-конфігурація; заборона повторного використання в README. |
| `src/SecureLab.Api/Presentation/Contracts/AuthContracts.cs:4` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Presentation/Contracts/AuthContracts.cs:5` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:29` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:30` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:31` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:32` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:33` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:125` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:126` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:127` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:137` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:145` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:168` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:169` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/Scaffolding/Lab03Identity.cs:170` | Не збережений credential: DTO/form/config key або звернення до runtime password через Identity; відкритого значення немає. |
| `src/SecureLab.Api/appsettings.Development.json:3` | Accepted, study-only: узгоджена відкрита loopback-конфігурація; заборона повторного використання в README. |
| `tests/SecureLab.Api.Tests/AuthenticationMechanicsTests.cs:10` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationMechanicsTests.cs:11` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationMechanicsTests.cs:12` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:23` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:24` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:25` | Не збережений credential: тест генерує нове випадкове значення під час запуску. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:39` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:58` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:59` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:60` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:173` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:176` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:178` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:179` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:196` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:200` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:204` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:222` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:223` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:224` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:225` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:232` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:235` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:239` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:253` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:254` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:255` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:261` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:273` | Не credential: синтетичний invalid-input/log marker у тесті; не використовується як справжній секрет. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:274` | Не credential: синтетичний invalid-input/log marker у тесті; не використовується як справжній секрет. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:275` | Не credential: синтетичний invalid-input/log marker у тесті; не використовується як справжній секрет. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:290` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:294` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:308` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:334` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:337` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:341` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:348` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:351` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:362` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:363` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:364` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:376` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:377` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:378` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:383` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:390` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:394` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:397` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:422` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:423` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:424` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:432` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:433` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthenticationSecurityTests.cs:434` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/AuthorizationSecurityTests.cs:351` | Не credential: синтетичний invalid-input/log marker у тесті; не використовується як справжній секрет. |
| `tests/SecureLab.Api.Tests/AuthorizationSecurityTests.cs:365` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/CookieClient.cs:9` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/CookieClient.cs:10` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/CookieClient.cs:12` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/CookieClient.cs:18` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/SecureLab.Api.Tests/packages.lock.json:208` | Не секрет: ідентифікатор пакета UserSecrets / версія залежності. |
| `tests/SecureLab.Api.Tests/packages.lock.json:291` | Не секрет: ідентифікатор пакета UserSecrets / версія залежності. |
| `tests/browser/README.md:10` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `tests/browser/README.md:28` | Не секрет: опис механізму, назва поля/сценарію або безпечна environment-інструкція; значення credential відсутнє. |
| `tests/browser/lab05-regression.cjs:16` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/browser/lab05-regression.cjs:17` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/browser/lab05-regression.cjs:28` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/http/lab-04-access.http:7` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/http/lab-04-verify.py:1` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/http/lab-04-verify.py:37` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/http/lab-04-verify.py:38` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/http/lab-04-verify.py:39` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |
| `tests/http/lab-04-verify.py:45` | Не збережений credential: назва тесту/поля, перевірка або runtime-значення з environment/генератора; значення не друкується. |

## Нові source/test файли поза початковим tracked переліком

Додатково оглянуто нові Security/*.cs, csrf.js, AntiforgeryTestExtensions.cs, HardeningSecurityTests.cs, lab06-runtime.py, lab06-regression.cjs і scan-listed-files.py. Використовуються environment password, назва CSRF header, токени лише в оперативній пам’яті, стандартний study-only connection та явно вигаданий incorrect-password marker. Нові справжні credentials у файли не внесено. Генеровані докази перевірено окремо на точний локальний seed password і на довгі ASP.NET protected-token значення.

## Конфігурація й межі висновку

- `.env.example` містить лише локальний порт. `.gitignore` виключає `.env`, `.env.*` з винятком `.env.example`, `appsettings.Local.json`, ключі, dump/log/TestResults. Це огляд правил файла; агент не виконував `git check-ignore`.
- Compose публікує PostgreSQL лише на `127.0.0.1`; три baseline входи study credentials узгоджені й залишені працездатними. Runtime-test використовує той самий study-only profile. Реальна конфігурація має надходити поза Git.
- Реальних секретів за виконаним текстовим оглядом не виявлено. Це не доказ їх відсутності в усіх можливих форматах, binary screenshots, history або зовнішніх системах.
- При реальному витоку одного видалення з current file недостатньо: відкликати/rotate credential, перевірити відповідну історію, клони, CI та logs; не переписувати історію без окремо визначеної процедури.
