# Architecture

## Overview

Modular monolith following Clean Architecture (inspired by Augure MS Teams backend).

```
Host → Api → Application → Domain ← Infrastructure
```

## Layers

| Layer | Responsibility |
|-------|----------------|
| **Host** | Composition root, middleware, configuration |
| **Api** | REST controllers, DTO mapping, HTTP concerns |
| **Application** | Commands, Queries, Handlers (MediatR), validation |
| **Domain** | Entities, enums, domain rules (no EF/ASP.NET refs) |
| **Infrastructure** | EF Core, PostgreSQL, external services |

## Modules (bounded contexts)

1. **Identity** — users, roles, refresh tokens
2. **HR** — employees, org units, job positions
3. **Evaluation** — quarterly evaluations (aggregate root)
4. **Compensation** — variable pay parameters and results

## Key rules

- Dependencies point inward only
- No business logic in controllers
- Writes = Command + Handler; Reads = Query + Handler
- `Evaluation` is aggregate root for goals, measures, criteria, etc.
- Status transitions: Draft → Submitted → UnderReview → Approved (return for revision → Draft)
- Schema changes via EF Core migrations only

## Database

- PostgreSQL 16 (Docker)
- Snake_case table names
- Optimistic locking on `evaluations.version`

## API

- REST (Swagger in Development)
- JWT auth (Phase 3)
