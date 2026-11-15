# Ashley.CleanTemplate

> A .NET 10 Clean Architecture template with DDD, CQRS, EF Core, and comprehensive testing.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-65%20passing-brightgreen)](#testing)

A solid starting point for building backend services in .NET. It implements a layered architecture that scales: a rich domain model, CQRS use-case handlers via MediatR, EF Core persistence, and a thin HTTP boundary with structured error responses. Every dependency points inward, and every business rule lives in one place.

---

## Table of contents

- [What this is](#what-this-is)
- [Architecture at a glance](#architecture-at-a-glance)
- [Technology stack](#technology-stack)
- [Features](#features)
- [Getting started](#getting-started)
- [Project structure](#project-structure)
- [Design decisions](#design-decisions)
- [Testing](#testing)
- [API reference](#api-reference)
- [Development notes](#development-notes)
- [Roadmap](#roadmap)
- [License](#license)

---

## What this is

Most "Clean Architecture" templates on GitHub are either too thin (an empty folder structure with no real code to learn from) or too heavy (a full CRM system with 40 entities and 200 files, where the architecture gets lost in the business logic).

**Ashley.CleanTemplate** sits in the middle. It implements a single realistic domain — `Order` with `OrderItem`s, `Money` as a value object, and a Draft → Placed → Fulfilled lifecycle — just rich enough to demonstrate the patterns that matter:

- A domain entity with invariants enforced in code, not just annotations
- A value object with factory validation and value equality
- Domain events raised by the aggregate when business-significant state transitions occur
- The CQRS split between commands (change state) and queries (read state)
- MediatR with a FluentValidation pipeline that short-circuits on invalid input
- EF Core with owned types, backing fields, and shadow foreign keys
- A WebApi layer that does nothing except translate HTTP to MediatR sends
- RFC 7807 ProblemDetails for error responses
- Integration tests that exercise the full stack against a real SQLite database

The result is a template you can clone, read in under an hour, and use as a starting point for real work.

---

## Architecture at a glance

The codebase follows Clean Architecture's "dependencies point inward" rule, enforced by the project reference graph:

```
┌─────────────────────────────────────────────────┐
│ WebApi                                          │
│ Controllers · Middleware · DI composition       │
│ depends on → Application, Infrastructure        │
├─────────────────────────────────────────────────┤
│ Infrastructure                                  │
│ EF Core · SQLite · DbContext · Migrations       │
│ depends on → Application                        │
├─────────────────────────────────────────────────┤
│ Application                                     │
│ MediatR handlers · FluentValidation · DTOs      │
│ depends on → Domain                             │
├─────────────────────────────────────────────────┤
│ Domain                                          │
│ Entities · Value Objects · Events · Rules       │
│ depends on → nothing                            │
└─────────────────────────────────────────────────┘
```

| Layer | What goes here | What never goes here |
|---|---|---|
| **Domain** | Entities, value objects, domain events, enums, exceptions | EF Core, HTTP, logging, any infrastructure concern |
| **Application** | CQRS commands/queries/handlers, validators, DTOs, pipeline behaviors | Concrete database setup, HTTP concerns |
| **Infrastructure** | DbContext, entity configurations, migrations, external service clients | Business logic, request handling |
| **WebApi** | Controllers, middleware, DI setup | Business logic, persistence details |

If you try to make Domain reference Infrastructure, the compiler refuses. The direction of dependencies is enforced by project references, not by discipline alone.

---

## Technology stack

| Concern | Choice | Version |
|---|---|---|
| Language | C# | 14 (.NET 10 default) |
| Runtime | .NET | 10.0 |
| HTTP framework | ASP.NET Core | 10.0 |
| CQRS dispatcher | MediatR | 12.5.0 (last MIT-licensed version — see [Design decisions](#why-mediatr-125-and-not-14)) |
| Validation | FluentValidation | 12.2.0 |
| ORM | Entity Framework Core | 10.0.13 |
| Database | SQLite | file-based, no server required |
| Testing | xUnit | 2.9.3 |
| Assertions | FluentAssertions | 7.2.2 (pinned below 8.x, which has a commercial license) |
| Integration test host | Microsoft.AspNetCore.Mvc.Testing | 10.0.13 |
| API documentation | Swashbuckle (Swagger) | 10.3.1 |

---

## Features

**Rich domain model**

- `Order` as an aggregate root with private setters and behavior methods (`CreateDraft`, `AddItem`, `RemoveItem`, `ChangeItemQuantity`, `Place`, `Fulfill`, `Cancel`)
- `OrderItem` as a child entity that can only be created or changed through its parent
- `Money` as a value object with factory validation, immutability, and value equality
- `OrderPlacedEvent` raised by `Order.Place()`
- Invariants enforced in code: currency consistency, status transitions, positive quantities

**CQRS with MediatR**

- One folder per use case, colocating command/query, validator, and handler
- `IRequest<T>` for commands with a return value, `IRequest` for void commands
- Thin handlers (typically 10–30 lines)
- A FluentValidation pipeline behavior that runs before every handler
- Output DTOs separate from domain entities; different queries return different shapes

**Persistence with EF Core**

- Code-first migrations
- `Money` mapped as an owned type (flattened into the parent table)
- Backing-field access for the private items collection
- Shadow foreign key (the child entity does not carry its parent's id)
- `HasConversion<string>()` for the status enum
- `DateTimeOffsetToBinaryConverter` for timestamps that SQLite can sort
- `IApplicationDbContext` as the Application layer's only view of the database

**HTTP layer**

- Thin controller: inject `IMediator`, send, return the result
- `[ApiController]` for automatic model-binding error handling
- RFC 7807 ProblemDetails for known error cases
- `ValidationProblemDetails` with camelCased, field-level error keys
- Lowercase REST URLs
- Swagger with XML doc comments and documented response types

**Error handling**

| Exception | HTTP status | Response body |
|---|---|---|
| `ValidationException` | 400 | ProblemDetails + `errors` dictionary |
| `NotFoundException` | 404 | ProblemDetails |
| `DomainException` | 400 | ProblemDetails with the domain rule message |
| `DbUpdateConcurrencyException` | 409 | ProblemDetails |
| Anything else | 500 | ProblemDetails (generic; details are logged, not leaked) |

**Testing**

- 65 tests across three projects: Domain (36), Application (20), Integration (9)
- Domain tests are plain unit tests with no mocks
- Application tests use the EF Core InMemory provider
- Integration tests use `WebApplicationFactory<Program>` against an isolated SQLite file

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- The [EF Core CLI tool](https://learn.microsoft.com/en-us/ef/core/cli/dotnet):
  ```bash
  dotnet tool install --global dotnet-ef --version 10.0.13
  ```

### Clone and run

```bash
git clone https://github.com/Richard-Ashley/Ashley.CleanTemplate.git
cd Ashley.CleanTemplate

# Restore packages and build
dotnet build

# Apply database migrations (creates a SQLite file in src/WebApi/)
dotnet ef database update \
  -p src/Infrastructure/Infrastructure.csproj \
  -s src/WebApi/WebApi.csproj

# Run the API
dotnet run --project src/WebApi
```

The API listens on `http://localhost:5122` (and `https://localhost:7111` with the `https` launch profile). Open [http://localhost:5122/swagger](http://localhost:5122/swagger) to explore it.

For HTTPS locally, trust the development certificate once with `dotnet dev-certs https --trust`.

### Run the tests

```bash
dotnet test
```

Expected: 65 tests passing, 0 failures. See [Development notes](#development-notes) if you are on Windows and the tests fail to load `Domain.dll`.

---

## Project structure

```
Ashley.CleanTemplate/
├── src/
│   ├── Domain/                         # Core business model
│   │   ├── Common/                     # Entity, ValueObject, IDomainEvent base types
│   │   ├── Entities/                   # Order, OrderItem
│   │   ├── ValueObjects/               # Money
│   │   ├── Events/                     # OrderPlacedEvent
│   │   ├── Enums/                      # OrderStatus
│   │   ├── Exceptions/                 # DomainException
│   │   └── Repositories/               # IOrderRepository (defined, currently unused)
│   │
│   ├── Application/                    # Use cases and orchestration
│   │   ├── Common/
│   │   │   ├── Behaviors/              # ValidationBehavior (MediatR pipeline)
│   │   │   ├── Exceptions/             # ValidationException, NotFoundException
│   │   │   └── Interfaces/             # IApplicationDbContext
│   │   ├── Orders/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateOrder/        # Command + Validator + Handler
│   │   │   │   └── PlaceOrder/         # Command + Handler
│   │   │   └── Queries/
│   │   │       ├── GetOrderById/       # Query + Handler + OrderDto
│   │   │       └── ListOrders/         # Query + Handler + OrderSummaryDto
│   │   └── DependencyInjection.cs      # AddApplication() extension
│   │
│   ├── Infrastructure/                 # External concerns
│   │   ├── Persistence/
│   │   │   ├── Configurations/         # IEntityTypeConfiguration classes
│   │   │   ├── Migrations/             # EF Core migrations
│   │   │   └── ApplicationDbContext.cs
│   │   └── DependencyInjection.cs      # AddInfrastructure() extension
│   │
│   └── WebApi/                         # HTTP boundary
│       ├── Controllers/                # OrdersController
│       ├── Middleware/                 # ExceptionMiddleware (ProblemDetails)
│       ├── appsettings.json            # SQLite connection string
│       └── Program.cs                  # Composition root
│
├── tests/
│   ├── Domain.Tests/                   # Plain unit tests (no mocks)
│   ├── Application.Tests/              # EF Core InMemory + FluentValidation TestHelper
│   └── Integration.Tests/              # WebApplicationFactory + real SQLite
│
├── .editorconfig
├── .gitattributes
├── .gitignore
├── Ashley.CleanTemplate.slnx
├── LICENSE
└── README.md
```

---

## Design decisions

These are the non-obvious choices, with their trade-offs.

### Why `DbSet<Order>` on `IApplicationDbContext` instead of a repository?

**The decision.** The Application layer's abstraction is `IApplicationDbContext`, which exposes `DbSet<Order>` directly. Handlers write LINQ against it.

**The reasoning.** EF Core's `DbContext` already implements the repository and unit-of-work patterns. Wrapping it in another repository interface adds code without adding much abstraction, and a repository tends to grow one method per query shape. The trade-off is that Application takes a dependency on `Microsoft.EntityFrameworkCore`.

**A loose end.** `Domain/Repositories/IOrderRepository.cs` is a leftover from an earlier design: it is defined but nothing implements or uses it. It is kept here as the shape a repository-based variant would take, and can be deleted without affecting anything.

**When to use repositories anyway.** If your team wants to keep LINQ out of handlers, needs to mock the data layer heavily, or may replace EF Core entirely, a repository interface pays off.

### Why MediatR 12.5 and not 14?

**The decision.** `src/Application/Application.csproj` pins MediatR to 12.5.0.

**The reasoning.** MediatR 14 moved to commercial licensing: running it in production without a license key logs a warning and requires a license from its vendor. For a public template, that forces anyone cloning it to make a vendor decision immediately. 12.5.0 is MIT-licensed and covers everything this template needs. The pipeline behavior signature (`next(cancellationToken)`) is the same in both versions.

**If you want a newer version.** Upgrade the package and obtain a license, or evaluate an alternative such as [Mediator](https://github.com/martinothamar/Mediator) (source-generator-based, MIT).

### Why store `TotalAmount` on `Order`?

**The decision.** `Order.TotalAmount` is a stored `decimal` column. The public `Total` property returns `Money.Create(TotalAmount, Currency)`. `RecalculateTotal()` runs from every method that changes items.

**The reasoning.** The list query originally summed item totals in LINQ (`o.Items.Sum(i => i.UnitPrice.Amount * i.Quantity)`). EF Core's SQLite provider cannot translate aggregates over `decimal`, because SQLite has no native decimal type, so that query would fail at runtime. Keeping the total as a column maintained by the domain methods lets the list projection read `o.TotalAmount` directly.

### Why `Money` as an owned type?

**The decision.** `OrderItem.UnitPrice` is a `Money` value object, mapped with `OwnsOne`, which flattens `Amount` and `Currency` into `UnitPriceAmount` and `UnitPriceCurrency` columns on `OrderItems`.

**The reasoning.** Value objects have no identity, so they do not need their own table. `OwnsOne` is EF Core's mechanism for exactly this pattern and avoids a join on every query.

### Why `HasConversion<string>()` for `OrderStatus`?

**The decision.** The status is stored as text (`"Draft"`, `"Placed"`) rather than as an integer.

**The reasoning.** Stored values are readable when you query the database directly, and renumbering the enum later cannot silently change the meaning of existing rows.

### Why `DateTimeOffsetToBinaryConverter`?

**The decision.** `Order.CreatedAt` and `Order.PlacedAt` are stored as `INTEGER` using `DateTimeOffsetToBinaryConverter`.

**The reasoning.** EF Core's SQLite provider does not support `ORDER BY` on a `DateTimeOffset` column stored as text and throws `NotSupportedException` at query time. The "newest first" order list needs it, so the timestamps are stored as sortable integers. The binary form keeps millisecond precision, so two orders created in the same millisecond may tie. A secondary sort on `Id` would be the next step if that ever matters.

### Why `sealed record` for domain events?

**The decision.** Domain events such as `OrderPlacedEvent` are `sealed record` types with primary-constructor syntax.

**The reasoning.** Events are data: two events with identical fields are the same event. Records provide value equality, concise construction, and immutability by default. `sealed` prevents a hierarchy that should not grow.

### Why validation in both the Application layer and the Domain?

**The decision.** `CreateOrderValidator` (FluentValidation) checks email format and input shape. `Order.CreateDraft()` only checks that the email contains an `@`.

**The reasoning.** Defense in depth. Validators run at the Application boundary and produce clean 400 responses with field-level errors. Domain invariants run inside the entity and refuse to construct invalid state no matter how it is called — background jobs, direct handler calls, tests. The asymmetry is intentional and is documented by a test (`CreateDraft_WithMalformedEmailThatContainsAt_StillCreatesOrder`).

### Why do controllers bind directly to Application commands?

**The decision.** `OrdersController.Create` takes `[FromBody] CreateOrderCommand` and sends it unchanged.

**The reasoning.** The commands are already clean, immutable records of primitives and nested DTOs. A separate request type would only be copied into the command. Separate HTTP DTOs pay off when several clients need different shapes, which this template does not have.

---

## Testing

### Test counts

| Project | Tests | What it covers |
|---|---|---|
| Domain.Tests | 36 | `Order` lifecycle, `Money` arithmetic and validation, domain events |
| Application.Tests | 20 | Handlers via EF Core InMemory, validators via the FluentValidation TestHelper |
| Integration.Tests | 9 | End-to-end HTTP through `WebApplicationFactory` against real SQLite |
| **Total** | **65** | |

### Running the tests

```bash
dotnet test
```

### How the integration tests work

`CustomWebApplicationFactory` boots the full WebApi in-process with a unique SQLite file per factory instance, applies the migrations, and deletes the file on disposal. Before each test, the `Orders` table is cleared.

These tests therefore exercise every layer at once:

- Routing and model binding
- The MediatR pipeline, including validators
- EF Core mapping (`OwnsOne`, backing fields, converters) against real SQLite
- ProblemDetails response shape and status codes

The InMemory provider used by the Application tests cannot catch provider-specific translation problems such as the SQLite `decimal` and `DateTimeOffset` limitations described above. The integration tests are what cover that.

### FluentAssertions version pin

The tests reference FluentAssertions 7.2.2. Version 8 and later is under a commercial license for some uses; pinning to 7.x keeps the template unambiguously open source.

---

## API reference

Base path: `/api/orders`

### `GET /api/orders`

Returns all orders, newest first.

**Response: 200 OK**

```json
[
  {
    "id": "7f3e0c52-0f4b-4a3b-9a55-3a1d0b2f6a10",
    "customerEmail": "customer@example.com",
    "status": "Placed",
    "createdAt": "2026-11-15T03:14:22.123+00:00",
    "total": 31.0,
    "currency": "USD",
    "itemCount": 1
  }
]
```

### `GET /api/orders/{id}`

Returns the full order with its items.

- **200 OK** — the `OrderDto`, including the items array
- **404 Not Found** — ProblemDetails

### `POST /api/orders`

Creates a draft order with items.

**Request body**

```json
{
  "customerEmail": "customer@example.com",
  "currency": "USD",
  "items": [
    { "productName": "Widget", "quantity": 2, "unitPrice": 15.50 }
  ]
}
```

- **201 Created** — `Location: /api/orders/{id}`, body is the new order's id
- **400 Bad Request** — ValidationProblemDetails:

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation failures occurred.",
  "instance": "/api/orders",
  "errors": {
    "customerEmail": ["Customer email must be a valid email address."],
    "currency": ["Currency must be a 3-letter ISO code."],
    "items": ["Order must have at least one item."]
  }
}
```

### `POST /api/orders/{id}/place`

Transitions a draft order to Placed and raises `OrderPlacedEvent`.

- **204 No Content** — success
- **404 Not Found** — the order does not exist
- **400 Bad Request** — the domain refused (already placed, no items, and so on)

---

## Development notes

### Windows and Smart App Control

On the author's Windows machine, Smart App Control blocked the freshly built, unsigned `Domain.dll` (error `0x800711C7`, an "Application Control policy" failure) and made `dotnet test` and `dotnet ef` fail to load it. Smart App Control is reputation-based and has no per-file allow list, and turning it off is irreversible, so during development the build, tests and EF tooling were run in WSL 2 (Ubuntu). All 65 tests pass there.

If you hit the same error, build and test in WSL or on Linux. A machine without Smart App Control is not affected.

### Line endings

`.gitattributes` normalizes line endings: LF in the repository, with `.sln`, `.slnx` and `.ps1` files kept as CRLF.

### Code style

`.editorconfig` sets modern C# conventions: `var` where the type is apparent, expression-bodied members for single-line methods, pattern matching, braces, and underscore-prefixed private fields. All severities are `suggestion`, so they never fail the build. `dotnet format` applies them.

### Database

The connection string in `src/WebApi/appsettings.json` points at `Ashley.CleanTemplate.db`, a SQLite file created in the WebApi project folder. `.gitignore` excludes `*.db`, `*.db-shm` and `*.db-wal`.

To reset your local database:

```bash
rm src/WebApi/Ashley.CleanTemplate.db
dotnet ef database update \
  -p src/Infrastructure/Infrastructure.csproj \
  -s src/WebApi/WebApi.csproj
```

### Known limitations

- Domain events are raised by the aggregate but nothing dispatches them yet.
- The `DbUpdateConcurrencyException` → 409 mapping exists but has no test.
- Malformed JSON bodies are rejected by ASP.NET Core's built-in `[ApiController]` handling, not by `ExceptionMiddleware`, so their response shape is the framework's default.
- `IOrderRepository` is defined in the Domain layer but unused.

---

## Roadmap

Deliberately out of scope for the current template; natural next steps for a derived project:

- **Authentication and authorization** (JWT bearer, policy-based authorization, user-scoped data)
- **Caching** (`IMemoryCache` in-process, `IDistributedCache` with Redis when distributed)
- **Observability** (structured logging with Serilog, OpenTelemetry traces, health checks)
- **Domain event dispatching** (a post-`SaveChanges` dispatcher that publishes events through MediatR's `INotification`)
- **Idempotency keys** for safe POST retries
- **Containerization** (Dockerfile and a docker-compose file with PostgreSQL)
- **Alternative persistence** (swap SQLite for PostgreSQL by changing the provider in `DependencyInjection.cs`)

---

## License

[MIT](LICENSE) © Richard Nii Asuku Ashley
