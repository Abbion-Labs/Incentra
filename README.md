# Incentra

Sistem za varijabilno nagrađivanje zaposlenih — backend API (.NET 8) sa PostgreSQL bazom.

## Preduslovi

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Brzi start

### 1. Pokreni PostgreSQL

```bash
cd docker
cp .env.example .env
docker compose up -d
```

### 2. Pokreni API

```bash
cd src/VariableCompensation.Host
dotnet run
```

### 3. Proveri

- Health: http://localhost:5000/health
- API health: http://localhost:5000/api/health
- Swagger: http://localhost:5000/swagger

> **Napomena:** Docker PostgreSQL koristi port **5433** (ne 5432) da ne bi došlo do konflikta sa lokalno instaliranim PostgreSQL.

## Struktura

```
src/
  VariableCompensation.Host/          # Entry point
  VariableCompensation.Api/           # REST controllers
  VariableCompensation.Application/   # CQRS handlers
  VariableCompensation.Domain/        # Entities, enums
  VariableCompensation.Infrastructure/# EF Core, PostgreSQL
```

## Migracije

```bash
dotnet ef migrations add <Name> \
  --project src/VariableCompensation.Infrastructure \
  --startup-project src/VariableCompensation.Host

dotnet ef database update \
  --project src/VariableCompensation.Infrastructure \
  --startup-project src/VariableCompensation.Host
```

Migracije i seed se pokreću kroz `IDbInitializator`. Podrazumevana `DbInit:Policy` je `OnStart`, pa lokalni razvoj inicijalizaciju izvršava pri svakom pokretanju. `DbInit:Seed` određuje da li se nakon migracija izvršava seed.

Na Vercelu je `DbInit__Policy=OnDeploy`. `OnDeployDbInitPolicy` koristi `VERCEL_DEPLOYMENT_ID` kao identitet deployment-a i PostgreSQL store da samo jedna instanca za taj identitet dobije dozvolu za inicijalizaciju.

## pgAdmin

- URL: http://localhost:5050
- Server: `host.docker.internal` (Windows/Mac) ili `db` iz Docker mreže
- Port: **5433** (izbegava konflikt sa lokalno instaliranim PostgreSQL na 5432)
- Database: `variable_compensation`
- User/Password: iz `docker/.env`

## Troubleshooting

### MSB3027 — DLL fajl je zaključan (`VariableCompensation.Host`)

API je već pokrenut u pozadini (npr. iz prethodnog `dotnet run`).

```bash
bash scripts/stop-api.sh
# ili: taskkill /F /IM VariableCompensation.Host.exe
```

Zatim ponovo `dotnet run` iz `src/VariableCompensation.Host`.

### `password authentication failed for user "vc_app"`

- `docker ps` — treba `vc-postgres` na portu **5433**
- Connection string u `appsettings.json` mora koristiti port **5433**
- `cd docker && docker compose up -d`

### Port 5000 je zauzet

Pokreni `bash scripts/stop-api.sh` ili promeni port u `Properties/launchSettings.json`.

## Auth (Faza 3)

Seed admin nalog:
- **Email:** `admin@local.dev`
- **Password:** `Admin123!`

Endpointi:
- `POST /api/auth/login` — javno
- `POST /api/auth/refresh` — javno
- `GET /api/auth/me` — zahteva JWT (`Authorize`)
- `POST /api/auth/register` — samo `ADMIN` role

Korisnik može imati **više uloga** istovremeno. U administraciji (`/admin/crud/users`) pri kreiranju ili izmeni korisnika izaberite jednu ili više uloga (checkbox). Nakon promene uloga, korisnik se mora ponovo prijaviti da bi JWT sadržao nove uloge.

U Swagger-u klikni **Authorize** i unesi: `Bearer <accessToken>`

## HR modul (Faza 4)

Lookup podaci se seed-uju pri inicijalizaciji baze (org. jedinice, radna mesta, nivoi obrazovanja).

Svi GET endpointi zahtevaju JWT. POST/PUT zahtevaju `ADMIN` ulogu.

### Organizacione jedinice
- `GET /api/organization-units` — lista (`?isActive=true`)
- `GET /api/organization-units/{id}`
- `POST /api/organization-units` — ADMIN
- `PUT /api/organization-units/{id}` — ADMIN

### Radna mesta
- `GET /api/job-positions` — lista (`?isActive=true`)
- `POST /api/job-positions` — ADMIN
- `PUT /api/job-positions/{id}` — ADMIN

### Nivoi obrazovanja
- `GET /api/education-levels` — lista (`?isActive=true`)
- `POST /api/education-levels` — ADMIN
- `PUT /api/education-levels/{id}` — ADMIN

### Zaposleni
- `GET /api/employees` — paginacija (`?page=1&pageSize=20&organizationUnitId=&search=&isActive=`)
- `GET /api/employees/{id}`
- `POST /api/employees` — ADMIN
- `PUT /api/employees/{id}` — ADMIN

Primer kreiranja zaposlenog:
```json
{
  "firstName": "Marko",
  "lastName": "Marković",
  "organizationUnitId": 1,
  "jobPositionId": 1,
  "educationLevelId": 1,
  "evaluatorEmployeeId": null,
  "hiredAt": "2024-01-15"
}
```

## Evaluation modul (Faza 5)

Workflow statusa: `Draft` → `Submitted` → `UnderReview` → `Approved` (ili vraćanje na doradu u `Draft`)

### Lookup podaci
- `GET /api/lookups/rating-levels`
- `GET /api/lookups/descriptive-ratings`
- `GET /api/lookups/measure-types` (`?includeDescriptions=true`)

### Ocenjivanja
- `GET /api/evaluations` — paginacija (`?page&pageSize&year&quarter&status&employeeId&evaluatorEmployeeId&organizationUnitId`)
- `GET /api/evaluations/{id}`
- `GET /api/evaluations/{id}/status-history`
- `POST /api/evaluations` — ADMIN, EVALUATOR
- `PUT /api/evaluations/{id}` — izmena draft-a (version obavezan)
- `PUT /api/evaluations/{id}/goals` — zamena ciljeva (draft)
- `PUT /api/evaluations/{id}/measures` — zamena merila (draft)
- `PUT /api/evaluations/{id}/criteria` — zamena kriterijuma (draft)
- `PUT /api/evaluations/{id}/conditions` — zamena uslova (draft)
- `PUT /api/evaluations/{id}/training` — obuka/razvoj (draft)
- `POST /api/evaluations/{id}/submit` — ADMIN, EVALUATOR
- `POST /api/evaluations/{id}/start-review` — ADMIN, CONTROLLER
- `POST /api/evaluations/{id}/approve` — ADMIN, CONTROLLER
- `POST /api/evaluations/{id}/return-for-revision` — ADMIN, CONTROLLER

Svaka izmena zahteva `version` za optimističko zaključavanje. Prosek se računa automatski pri čuvanju ciljeva/merila i pri submit-u.

## Evaluator settings i RBAC (Faza 6)

### Podešavanja ocenjivača (pragovi i procenti)
- `GET /api/evaluator-settings` — ADMIN (lista svih)
- `GET /api/evaluator-settings/me` — EVALUATOR (sopstvena podešavanja)
- `GET /api/evaluator-settings/{employeeId}` — ADMIN ili evaluator (samo svoj `employeeId`)
- `POST /api/evaluator-settings` — ADMIN
- `PUT /api/evaluator-settings/{employeeId}` — ADMIN

### Povezivanje korisnika sa zaposlenim
- `PUT /api/employees/{id}/user` — ADMIN — body: `{ "userId": 2 }` ili `{ "userId": null }`

`/api/auth/me` sada vraća `employeeId` i `employeeFullName` kada je korisnik povezan sa zaposlenim.

### Role-based pristup ocenama
| Uloga | Pristup |
|-------|---------|
| `ADMIN` | pun pristup |
| `EVALUATOR` | vidi/uređuje ocene gde je dodeljen kao ocenjivač |
| `CONTROLLER` | vidi/review/approve ocene gde je dodeljen kao kontroler; može vratiti na doradu |
| `EMPLOYEE` | read-only sopstvene ocene |

Korisnik mora biti povezan sa zaposlenim (`userId` na employee) da bi scoped pristup radio.

## Varijabilna kompenzacija (Faza 7)

Kalkulacija koristi **samo odobrene** (`Approved`) kvartalne ocene i `EvaluatorSettings` pragove za procenat.

### Parametri
- `GET /api/compensation-parameters` — PAYROLL
- `GET /api/compensation-parameters/{id}`
- `POST /api/compensation-parameters` — PAYROLL
- `PUT /api/compensation-parameters/{id}` — PAYROLL
- `POST /api/compensation-parameters/{id}/calculate` — PAYROLL
- `POST /api/compensation-parameters/{id}/finalize` — PAYROLL

Primer kreiranja parametara:
```json
{
  "organizationUnitId": 1,
  "year": 2026,
  "monetaryPool": 500000,
  "upperLimitCoefficient": 0.25,
  "dependencyWeight": 1.0,
  "exponent": 1.5
}
```

Primer kalkulacije:
```json
{ "isFinal": false, "requireAllQuarters": false }
```

### Rezultati
- `GET /api/compensation-results` — paginacija (`?parametersId&year&organizationUnitId&employeeId`)
- `GET /api/compensation-results/{id}` — detalj sa povezanim ocenama

Formula `v1`: bodovi iz radnog mesta (`SortOrder × 10`), procenat iz evaluator settings, Z-score normalizacija i raspodela novčane mase sa gornjim limitom po zaposlenom.

## Pregled baze podataka

### pgAdmin (preporučeno za vizuelni pregled)

1. Otvori http://localhost:5050
2. Login: `admin@local.dev` / `admin` (iz `docker/.env`)
3. **Register → Server**
   - Name: `Incentra`
   - Host: `host.docker.internal`
   - Port: `5433`
   - Database: `variable_compensation`
   - Username: `vc_app`
   - Password: `vc_dev_password_change_me`
4. Proširi: `Servers → Incentra → Databases → variable_compensation → Schemas → public → Tables`

### Terminal (psql)

```bash
docker exec -it vc-postgres psql -U vc_app -d variable_compensation
```

Korisne komande:
```sql
\dt                          -- lista tabela
SELECT "Code", "Name" FROM roles;
SELECT "Id", "Email" FROM users;
\q                           -- izlaz
```

### DBeaver (alternativa)

Besplatan GUI klijent — nova konekcija PostgreSQL sa istim parametrima kao pgAdmin (host `localhost`, port `5433`).

## Frontend (Faza 9)

React + Vite aplikacija u folderu `frontend/`.

### Pokretanje

1. API mora raditi na `http://localhost:5000`
2. U drugom terminalu:

```bash
cd frontend
npm install
npm run dev
```

3. Otvori http://localhost:5173

Vite proxy prosleđuje `/api` zahteve na backend. CORS je podešen za dev server.

### Uloge i ekrani

| Uloga | Početna putanja | Moduli |
|-------|-----------------|--------|
| `ADMIN` | `/admin/crud/employees` | CRUD zaposleni, korisnici, podešavanja ocenjivača, šifarnici, opisne ocene |
| `EVALUATOR` | `/evaluator` | Podređeni zaposleni, postavljanje ciljeva (`/evaluator/goals`), workflow ocenjivanja (`/evaluator/workflow`), analitika |
| `CONTROLLER` | `/controller/workflow` | Pregled i odluka o ocenama, lista ocenjivača, profili zaposlenih |
| `EMPLOYEE` | `/employee` | Pregled sopstvenih ocena (bez faze planiranja ciljeva) |
| `PAYROLL` | `/admin/varijabila/salaries` | Plate, parametri kompenzacije, kalkulacija, rezultati, analitika |

> **Napomena:** Modul varijabilne kompenzacije (`/admin/varijabila/*`) je namenjen ulozi `PAYROLL`, ne `ADMIN`. API endpointi za plate i kompenzaciju zahtevaju `PAYROLL` ulogu.

UI podržava **srpski i engleski** jezik (prekidač u gornjoj traci).

Korisnik sa više uloga vidi sve stavke menija za uloge koje poseduje (npr. `ADMIN` + `EVALUATOR` → CRUD i modul ocenjivanja). Početna stranica (`/`) vodi na prvi operativni modul po prioritetu: ocenjivač → kontrolor → zaposleni → plate → administracija.

### Tok ocenjivanja u UI

1. **Postavljanje ciljeva** (`/evaluator/goals`) — razgovor, ciljevi, uslovi i kriterijumi; automatsko čuvanje nacrta
2. **Ocenjivanje** (`/evaluator/evaluations/:id`) — ocene ciljeva, merila, obuka, slanje kontroloru
3. **Kontrolor** (`/controller/evaluations/:id`) — odobrenje ili vraćanje na doradu
4. **Zaposleni** (`/employee`) — pregled ocena po statusu (neocenjene, vraćene, poslate, odobrene); filter po godini i kvartalu (uključujući „Svi kvartali“)

Istorija promena statusa dostupna je na detalju ocene (ocenjivač, kontrolor, zaposleni).

### Test nalozi

- Admin: `admin@local.dev` / `Admin123!`
- Ocenjivač: `evaluator@local.dev` / `Eval123!` (povezan sa zaposlenim Jovan)
- Kontrolor: `controller@local.dev` / `Control123!` (povezan sa zaposlenim Milan)
- Zaposleni: `marko@local.dev` / `Marko123!` (Marko Marković)
- Plate/varijabila: korisnik sa `PAYROLL` ulogom (kreirati u administraciji ako nije u seed-u)

Demo seed pri inicijalizaciji baze kreira 8 podređenih zaposlenih (Marko, Ana, Petar, …) dodeljenih ocenjivaču Jovanu.

## Skladištenje avatara

Podrazumevani provider je lokalni filesystem (`Storage:Provider=Local`), tako da lokalni razvoj i testovi ne zahtevaju Supabase.

Za deployment sa Supabase Storage postaviti sledeće environment varijable:

```text
Storage__Provider=Supabase
Storage__Supabase__Url=https://<project-ref>.supabase.co
Storage__Supabase__SecretKey=<server-side secret key>
Storage__Supabase__Bucket=employee-avatars
```

Bucket mora već postojati i mora biti **public** da bi postojeći `AvatarUrl` mogao direktno da se koristi u frontendu. Upload i delete se i dalje izvršavaju isključivo kroz backend. Supabase secret key je server-side tajna i ne sme biti dostupan frontend-u.

Supabase integracija je izolovana iza `IEmployeeAvatarStorage`; Application i Domain slojevi ne zavise od Supabase-a, pa se provider može kasnije ukloniti ili zameniti bez promene poslovne logike.

## Email obaveštenja

Korisnici mogu uključiti email obaveštenja na stranici **Moj nalog** (`/account`). Podrazumevano su isključena (opt-in).

Obaveštenja se šalju kada:

- **Kontrolor** — ocena je predata na pregled
- **Ocenjivač** — ocena je odobrena ili vraćena na doradu

### Konfiguracija po okruženju

`appsettings.json` sadrži samo zajednička, deployment-neutral podešavanja. Lokalni hostovi, URL-ovi i razvojni/test kredencijali nalaze se u:

- `appsettings.Development.json` — lokalni razvoj
- `appsettings.Testing.json` — test okruženje

Deployment vrednosti se ne commituju u repozitorijum, već se prosleđuju kroz environment varijable koristeći ASP.NET Core `__` konvenciju, na primer:

```text
ConnectionStrings__DefaultConnection
Jwt__Secret
SalaryEncryption__Key
Email__Host
Email__Port
Email__UseSsl
Email__Username
Email__Password
Email__FromAddress
Email__FrontendBaseUrl
DbInit__Policy
DbInit__Seed
```

### SMTP konfiguracija

Za lokalni razvoj, `appsettings.Development.json` podrazumevano koristi SMTP na `localhost:1025` i frontend na `http://localhost:5173`.

1. Pokrenite SMTP alat (npr. [Papercut](https://github.com/ChangemakerStudios/Papercut-SMTP) ili MailHog) na portu `1025`
2. Postavite `Email:Enabled` na `true` u razvojnoj konfiguraciji
3. Uključite obaveštenja na nalogu kontrolora/ocenjivača

Kada je `Email:Enabled=false`, API samo loguje da bi poslao poruku (workflow i dalje uspeva).

## Testovi

### Brzo pokretanje

```bash
bash scripts/test-all.sh
```

Zahteva Docker Desktop (integration testovi koriste Testcontainers PostgreSQL).

### Pojedinačno

```bash
# Backend unit + arhitektura
dotnet test tests/VariableCompensation.Application.Tests
dotnet test tests/VariableCompensation.Architecture.Tests

# Backend integration (Docker)
dotnet test tests/VariableCompensation.Integration.Tests

# Frontend (Vitest)
cd frontend && npm test
```

### E2E (Playwright)

```bash
cd tests/e2e
npm install
npx playwright install chromium
npm test
```

E2E očekuje pokrenut API (`localhost:5000`) i frontend (`localhost:5173`), ili koristi `webServer` u `playwright.config.ts` lokalno.

### Struktura

| Projekat | Sloj | Broj testova (orientaciono) |
|----------|------|-----------------------------|
| `Application.Tests` | Unit + handler | workflow, scoring, kompenzacija, RBAC |
| `Integration.Tests` | API + PostgreSQL | auth, evaluation lifecycle |
| `Architecture.Tests` | NetArchTest | zavisnosti slojeva |
| `frontend` Vitest | UI utils/hooks | scoring, navigacija, autosave |
| `tests/e2e` | Playwright smoke | login po ulogama |

Golden fajlovi za scoring paritet: `tests/golden/scoring-cases.json` (backend, 4 dec.) i `frontend/tests/golden/scoring-cases.json` (frontend, 2 dec.).
