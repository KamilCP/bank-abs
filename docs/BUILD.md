# Збирання, нумерація версій і робота з гілками

## 1. Середовище

| Інструмент | Версія | Призначення |
|---|---|---|
| Visual Studio 2026 Community (або `dotnet` CLI) | .NET SDK 10.0 | розробка, збирання, налагодження, запуск тестів |
| PostgreSQL + pgAdmin 4 | 18 (сумісно з 16+) | сервер БД |
| Git, GitHub | — | зберігання коду, Pull Request, рев'ю |
| GitHub Actions | — | автоматичне збирання і тести (`.github/workflows/build.yml`) |

## 2. Склад рішення і залежності модулів

```
BankAbs.slnx
├── src/BankAbs.Domain   — сутності, бізнес-правила, розрахунки (без залежностей)
├── src/BankAbs.Data     — репозиторії ADO.NET, вбудовані SQL-скрипти     → Domain
├── src/BankAbs.Web      — Blazor (SSR), автентифікація, сторінки, звіти   → Data, Npgsql
├── tests/BankAbs.Tests  — xUnit: модульні та інтеграційні тести            → Domain, Data, Npgsql
└── db/01..04_*.sql      — схема, функції і процедури, тригери, початкові дані
```

Спільні налаштування всіх проєктів — у `Directory.Build.props` (цільова платформа, версія, правила якості),
версії NuGet-пакетів — централізовано в `Directory.Packages.props`.

## 3. Обмін модулями між програмістами (Git Flow)

| Гілка | Призначення | Хто зливає |
|---|---|---|
| `main` | випущені версії, кожен випуск має тег `vX.Y.Z` | Scrum-майстер після приймання |
| `develop` | інтеграція завершених задач спринту | через Pull Request після рев'ю |
| `feature/BANK-N-опис` | робота над задачею Jira BANK-N | розробник задачі |
| `release/X.Y` | підготовка версії: лише виправлення дефектів | тестувальник / розробник |
| `hotfix/X.Y.Z` | термінове виправлення випущеної версії | розробник |

Порядок роботи розробника:

1. `git switch develop && git pull` — отримати останні версії модулів колег.
2. `git switch -c feature/BANK-15-deposits` — гілка для задачі.
3. Коміти з ключем задачі: `BANK-15: нарахування відсотків за вкладами`.
4. `git push` і Pull Request у `develop`; CI збирає рішення і запускає тести; колега виконує рев'ю.
5. Після злиття гілка видаляється, задача в Jira переходить у «Testing».

Зміни схеми БД вносяться лише через файли `db/*.sql` (разом з кодом, що їх використовує), тому будь-який
розробник отримує узгоджену версію БД разом з кодом.

## 4. Нумерація версій

Використовується семантичне версіонування: `MAJOR.MINOR.PATCH[-суфікс]+build.N.коміт`.

| Частина | Де задається | Коли змінюється | Приклад |
|---|---|---|---|
| MAJOR.MINOR.PATCH (`VersionPrefix`) | `Directory.Build.props` | під час підготовки випуску (план релізів ЛР № 3) | `0.2.0` |
| Суфікс стадії (`VersionSuffix`) | `Directory.Build.props` | `beta.N` — бета-версія, `rc.N` — кандидат, порожньо — промислова | `beta.1` |
| Номер збірки (`BuildNumber`) | параметр збирання; на CI — `github.run_number` | кожне збирання на сервері CI; локально `0` | `15` |
| Коміт (`GitCommit`) | обчислюється командою `git rev-parse --short=7 HEAD` | кожен коміт | `a1b2c3d` |

З цих значень MSBuild формує атрибути збірки:

| Атрибут | Значення для збірки № 15 |
|---|---|
| `AssemblyVersion` | `0.2.0.0` |
| `FileVersion` | `0.2.0.15` |
| `InformationalVersion` | `0.2.0-beta.1+build.15.a1b2c3d` |
| `AssemblyMetadata("BuildDate")` | дата і час збирання (UTC) |
| `AssemblyMetadata("BuildNumber")` | `15` |

## 5. Збирання

### Visual Studio 2026

1. Відкрити `BankAbs.slnx`, конфігурація **Debug** (розробка) або **Release** (випуск).
2. **Build → Build Solution** (Ctrl+Shift+B). Попередження аналізаторів і стилю коду є помилками.
3. **Test → Run All Tests** (Ctrl+R, A) — модульні тести; інтеграційні — за наявності змінної
   `BANKABS_TEST_CONNECTION`.
4. Запуск — F5 (профіль `http`, адреса `http://localhost:5180`).
5. Публікація — **Build → Publish BankAbs.Web → Folder**.

### Командний рядок

```bash
dotnet restore BankAbs.slnx
dotnet build BankAbs.slnx -c Release -p:BuildNumber=15
dotnet test BankAbs.slnx -c Release --no-build
dotnet publish src/BankAbs.Web -c Release -o publish -p:BuildNumber=15
```

### Сервер CI (GitHub Actions)

Кожен `push` у `main`, `develop`, `feature/*`, `release/*`, `hotfix/*` і кожен Pull Request запускає
`.github/workflows/build.yml`:

1. запуск контейнера PostgreSQL 18 і створення тестової БД скриптами `db/*.sql`;
2. відновлення пакетів, збирання `Release` з номером збірки `github.run_number`;
3. модульні та інтеграційні тести з результатом у форматі TRX;
4. публікація веб-застосунку і збереження артефакту `BankAbs-build-N`.

## 6. Номер версії у формі «Про програму»

Номер версії читається з атрибутів збірки під час виконання (клас `BankAbs.Web.Services.AppInfo`):

```csharp
var assembly = typeof(AppInfo).Assembly;
var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion; // 0.2.0-beta.1+build.15.a1b2c3d
var buildDate = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(a => a.Key == "BuildDate").Value;
```

Форма «Про програму» (`/about`) показує версію, повний номер збірки, версію файлу, номер збірки, коміт,
дату збирання, середовище виконання і версію сервера БД; номер версії і збірки також виводяться в рядку стану.

## 7. Випуск версії (чек-лист)

1. Заморожування коду за 2 робочі дні до кінця спринту, гілка `release/X.Y` з `develop`.
2. У `Directory.Build.props` встановити `VersionPrefix` і `VersionSuffix` (`beta.N` / `rc.N` / порожньо).
3. Регресійне тестування збірки з CI; дефекти — у Jira (тип Bug), виправлення — у гілці `release/X.Y`.
4. Злиття в `main` і `develop`, тег `vX.Y.Z`, GitHub Release з артефактом збірки і примітками до випуску.
5. У Jira версію позначити як Released.
