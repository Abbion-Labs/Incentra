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

Migracije se automatski primenjuju pri pokretanju aplikacije (`MigrateAndSeedAsync`).

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
- `POST /api/auth/refresh` — javno; refresh token čita iz kolačića
- `POST /api/auth/logout` — javno; odjavljuje celu sesiju i briše kolačić
- `GET /api/auth/me` — zahteva JWT (`Authorize`)
- `POST /api/auth/select-role` — zahteva JWT; `{ roleCode }`; zatvara tekuću sesiju i otvara novu u izabranoj ulozi
- `POST /api/auth/register` — samo `ADMIN` role; vraća profil novog korisnika, bez tokena

Korisnik može imati **više uloga** istovremeno. U administraciji (`/admin/crud/users`) pri kreiranju ili izmeni korisnika izaberite jednu ili više uloga (checkbox). Promena uloga odmah odjavljuje sve sesije tog korisnika, pa se mora ponovo prijaviti da bi JWT sadržao nove uloge.

Sesija radi u **jednoj ulozi**: JWT nosi samo nju (`activeRole` u profilu), refresh token je pamti kroz rotaciju, a nalog pamti poslednju izabranu ulogu za sledeću prijavu. Prijava počinje u poslednjoj korišćenoj ulozi, a ako je nema, po prioritetu `EVALUATOR → CONTROLLER → EMPLOYEE → PAYROLL → ADMIN`. Ulogu menja prekidač **Aktivna uloga** u zaglavlju: `select-role` gasi tekuću sesiju i otvara novu samo za izabranu ulogu, pa stari access token prestaje da važi odmah, a ostali tabovi (dele kolačić) pređu u novu ulogu čim ih kanal obavesti. Uz promenu statusa ocene beleži se i uloga u kojoj je korisnik tada radio.

> **Sesije otvorene pre ove izmene nose sve uloge u tokenu, pa nemaju aktivnu ulogu.** Korisnik sa više uloga do prvog osvežavanja tokena vidi samo svoj zapis i prazne liste; token se osvežava pri svakom učitavanju aplikacije, a najkasnije kroz sat vremena, i tada dobija ulogu po pravilu iz prethodnog pasusa.

Ekrani ocenjivača i kontrolora imaju svoje liste: `GET /api/evaluator/{evaluations,employees,analytics}` i `GET /api/controller/{evaluations,employees,evaluators/{id}/analytics}`. Zajedničke liste (`/api/evaluations`, `/api/employees`) koriste admin ekrani i ekran „Moje ocene“.

### Tokeni

| Token | Trajanje | Gde se čuva |
|---|---|---|
| Access (JWT) | 60 min | u telu odgovora; frontend ga drži **samo u memoriji stranice** |
| Refresh | 7 dana, rotira se | kolačić `vc_refresh` (`HttpOnly`, `SameSite=Strict`, `Path=/api/auth`) |

Refresh token se **ne vraća u telu odgovora**, pa ga skripte na stranici ne mogu pročitati. `SameSite=Strict` znači da se kolačić nikad ne šalje sa tuđeg sajta, pa poseban CSRF token nije potreban.

#### Sesije

Svaka prijava otvara **sesiju**. Svi refresh tokeni nastali iz te prijave nose isti `SessionId`, a access token ga nosi u claim-u `sid`.

- **Svaki zahtev proverava sesiju** (jedan upit po indeksu): sesija mora imati bar jedan važeći refresh token, a korisnik mora biti aktivan. Zato odjava, deaktivacija naloga, promena uloga i promena lozinke deluju **odmah**, a ne tek kad access token istekne. Access token bez `sid`-a se odbija.
- **Odjava** gasi celu sesiju, sa svim tabovima tog browsera. Tabovi se međusobno obaveštavaju (`BroadcastChannel`), pa se odjava ili prijava u jednom tabu odmah vidi i u ostalima.
- **Promena sopstvene lozinke** odjavljuje sve ostale uređaje, a trenutni ostaje prijavljen. Admin reset lozinke, promena uloga i deaktivacija odjavljuju sve sesije korisnika.

#### Rotacija i ukraden token

Refresh token se menja pri svakoj upotrebi, a zamena je atomična: dva istovremena refresh-a istim tokenom ne mogu oba da ga iskoriste.

- Ako isti token stigne ponovo **u roku od 30 sekundi** posle zamene, dobija novi token u istoj sesiji. To pokriva reload stranice dok je odgovor na refresh još bio na putu.
- Ako stigne **kasnije**, to znači da postoji kopija tokena: **cela sesija se odjavljuje** (i kod napadača i kod korisnika), a u log ide upozorenje.

Podešavanja:

- `RefreshCookie__Secure` — kolačić se šalje samo preko HTTPS-a. Podrazumevano je `true` i u produkciji se **ne podešava**. Isključen je jedino u `appsettings.Development.json`, jer lokalni razvoj radi preko HTTP-a, gde bi browser odbio `Secure` kolačić.

  > Namerno se ne izvodi iz `Request.IsHttps`: iza Vercel proxy-ja backend prima običan HTTP (proxy završava TLS), pa bi se `Secure` tiho isključio u produkciji.
- Kolačić radi dok su frontend i API na istom domenu (lokalno preko Vite proxy-ja, na Vercelu preko `/api` rewrite-a). Ako API pređe na poseban domen, traži `SameSite=None`, CORS sa kredencijalima i zasebnu CSRF zaštitu.
- `POST /api/auth/refresh` i `/logout` primarno čitaju kolačić, a kao rezervu prihvataju i `{ "refreshToken": "..." }` u telu, radi Swagger-a i ručnog testiranja.
- `Jwt__RefreshTokenReuseGraceSeconds` — koliko dugo posle zamene token sme ponovo da stigne (podrazumevano `30`).

> **Pri prelasku na kolačić i sesije svi prijavljeni korisnici moraju jednom ponovo da se prijave.** Tokeni su im ostali u `localStorage`, koji se više ne koristi (frontend te ključeve briše pri učitavanju), migracija `AddRefreshTokenSessions` briše stare refresh tokene, a stari access tokeni nemaju `sid`.

U Swagger-u klikni **Authorize** i unesi: `Bearer <accessToken>`

## Istovremene izmene

Svaki zapis koji se menja kroz formu (zaposleni, korisnici, podešavanja ocenjivača, organizacione jedinice, radna mesta, nivoi obrazovanja, opisne ocene, parametri kompenzacije, plate) ima polje `version`. API ga vraća uz zapis, a svaka izmena (`PUT`) mora da pošalje verziju sa koje je napravljena:

- bez verzije: `400` sa `vn-0106`,
- zapis je u međuvremenu izmenjen: `400` sa `vn-0090` (ili `409` ako su se dve izmene sudarile u istom trenutku),
- inače izmena prolazi i verzija raste za 1.

Kod plata je to verzija trenutno važeće plate, a izostavlja se samo pri unosu prve plate. Ocene imaju svoj `version` sa istim značenjem. Forma koja dobije konflikt prikazuje poruku i učitava novo stanje.

Izuzeci su akcije nad jednim poljem koje ne mogu da pregaze tuđe podatke: promena lozinke, reset lozinke, obaveštenja i avatar.

## HR modul (Faza 4)

Lookup podaci se seed-uju pri pokretanju (org. jedinice, radna mesta, nivoi obrazovanja).

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
- `POST /api/evaluations` — EVALUATOR
- `PUT /api/evaluations/{id}` — izmena draft-a (version obavezan)
- `PUT /api/evaluations/{id}/goals` — zamena ciljeva dok plan nije postavljen; posle toga samo ocena i komentar postojećih ciljeva (po `id`)
- `PUT /api/evaluations/{id}/measures` — zamena merila (draft)
- `PUT /api/evaluations/{id}/criteria` — zamena kriterijuma (draft)
- `PUT /api/evaluations/{id}/conditions` — zamena uslova (draft)
- `PUT /api/evaluations/{id}/training` — obuka/razvoj (draft)
- `POST /api/evaluations/{id}/submit` — EVALUATOR; ocena ocenjivača bez kontrolora je odmah odobrena
- `POST /api/evaluations/{id}/start-review` — CONTROLLER
- `POST /api/evaluations/{id}/approve` — CONTROLLER
- `POST /api/evaluations/{id}/return-for-revision` — CONTROLLER

Svaka izmena zahteva `version` za optimističko zaključavanje. Prosek se računa automatski pri čuvanju ciljeva/merila i pri submit-u.

## Evaluator settings i RBAC (Faza 6)

### Podešavanja ocenjivača (dodeljeni kontrolor)

Podešavanja nastaju dodelom uloge `EVALUATOR` i nose samo kontrolora ocenjivača. Ocenjivač na vrhu organizacije može biti **bez kontrolora**: njegove ocene niko ne kontroliše, pa su odobrene čim ih pošalje. Ocenjivač ne može biti sam sebi kontrolor, a kontrolor ne može kontrolisati ocenjivača koji ocenjuje njega (niko ne odobrava sopstvenu ocenu). Opsezi ocena i preporučeni udeli su zajednički za celu organizaciju i podešavaju se na opisnim ocenama.

- `GET /api/evaluator-settings` — ADMIN (lista svih)
- `GET /api/evaluator-settings/me` — EVALUATOR (sopstvena podešavanja)
- `GET /api/evaluator-settings/{employeeId}` — ADMIN ili evaluator (samo svoj `employeeId`)
- `PUT /api/evaluator-settings/{employeeId}` — ADMIN — body: `{ "controllerEmployeeId": 5 }` ili `{ "controllerEmployeeId": null }` (bez kontrolora)

### Povezivanje korisnika sa zaposlenim
- `PUT /api/employees/{id}/user` — ADMIN — body: `{ "userId": 2 }` ili `{ "userId": null }`

`/api/auth/me` sada vraća `employeeId` i `employeeFullName` kada je korisnik povezan sa zaposlenim.

### Role-based pristup ocenama
| Uloga | Pristup |
|-------|---------|
| `ADMIN` | vidi sve ocene, ali ne učestvuje u toku: ne kreira, ne menja, ne šalje, ne odobrava i ne vraća ocene |
| `EVALUATOR` | vidi/uređuje ocene gde je dodeljen kao ocenjivač |
| `CONTROLLER` | vidi/review/approve ocene gde je dodeljen kao kontroler; može vratiti na doradu |
| `EMPLOYEE` | read-only sopstvene ocene |

Korisnik mora biti povezan sa zaposlenim (`userId` na employee) da bi scoped pristup radio.

## Varijabilna kompenzacija (Faza 7)

Kalkulacija koristi **samo odobrene** (`Approved`) kvartalne ocene i parametre organizacione jedinice (prihvatljiva ocena, eksponent, težina zavisnosti, fond).

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

Formula `v1`: bodovi iz radnog mesta (`SortOrder × 10`), procenat iz evaluator settings, Z-score normalizacija i raspodela novčane mase.

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

1. **Postavljanje ciljeva** (`/evaluator/goals`) — razgovor, ciljevi, uslovi i kriterijumi; čuvanjem se plan zaključava: posle toga se ciljevi, uslovi i kriterijumi više ne menjaju, ni posle vraćanja na doradu
2. **Ocenjivanje** (`/evaluator/evaluations/:id`) — ocene ciljeva, merila, obuka, slanje kontroloru
3. **Kontrolor** (`/controller/evaluations/:id`) — odobrenje ili vraćanje na doradu
4. **Zaposleni** (`/employee`) — pregled ocena po statusu (neocenjene, vraćene, poslate, odobrene); filter po godini i kvartalu (uključujući „Svi kvartali“)

Istorija promena statusa dostupna je na detalju ocene (ocenjivač, kontrolor, zaposleni).

### Test nalozi

Demo seed pravi 6 organizacionih jedinica sa 120 zaposlenih: u svakoj rukovodioca koji ocenjuje ostale, dva menadžera koji kontrolišu rukovodioce, i ocene za 2024, 2025 i 2026. Za Q3 2026 otprilike svaki četvrti zaposleni kod svakog ocenjivača ima već poslatu ocenu, pa svaki kontrolor ima šta da pregleda; ostali imaju nacrt za planiranje.

| Nalog | Lozinka | Uloge |
|---|---|---|
| `admin@local.dev` | `Admin123!` | Administrator |
| `payroll@local.dev` | `Payroll123!` | Plate i varijabila |
| `evaluator@local.dev` | `Eval123!` | Zaposleni, Ocenjivač i Kontrolor: rukovodilac prve jedinice, koji kontroliše i rukovodioca četvrte (`evaluator4@`) |
| `evaluator2@` … `evaluator6@local.dev` | `Eval123!` | Zaposleni i Ocenjivač: rukovodioci ostalih jedinica |
| `controller@local.dev`, `controller2@local.dev` | `Control123!` | Zaposleni i Kontrolor: menadžeri |
| `zaposleni@local.dev` … `zaposleni6@local.dev` | `Zaposleni123!` | Zaposleni: po jedan zaposleni u svakoj jedinici |

Pri svakom pokretanju seed proverava demo naloge i vraća im uloge i vezu sa zaposlenim ako su promenjene.

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
