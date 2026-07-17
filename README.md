# Inventory & Stock Movements Tracker

A small ASP.NET Core MVC module for tracking products and their stock levels, built for the DevExcel IT Solutions .NET Developer practical assignment.

## Tech Stack

- **.NET 10**, C# (latest language version)
- **ASP.NET Core MVC** (server-rendered Razor views)
- **Entity Framework Core** with **SQL Server LocalDB** (application database)
- **Bootstrap** (ships with the default MVC template)
- **xUnit** for unit tests (using EF Core's In-Memory provider and an in-memory SQLite connection — test-only, not used by the application itself)

## How to Run

### Prerequisites

- .NET 10 SDK
- Visual Studio 2026 (or any editor with the .NET 10 SDK on PATH)
- SQL Server LocalDB (installed automatically alongside Visual Studio)

### Setup

1. Clone the repository.
2. Open `InventoryTracker.slnx` in Visual Studio.
3. Restore NuGet packages (Visual Studio does this automatically on open, or run `dotnet restore` from the solution root).
4. Apply the migration to create the database:

```bash
   cd InventoryTracker.Web
   dotnet ef database update
```

   This creates `InventoryTrackerDb` on `(localdb)\mssqllocaldb` automatically. No manual database creation is required.

5. Run the application:

```bash
   dotnet run
```

   or press `F5` in Visual Studio.

6. The app seeds itself automatically on first run with 5 sample products and their full movement history. No separate seed command is needed — subsequent runs skip seeding if data already exists.

7. Navigate to the **Products** tab in the navbar (or `/Products`) to start using the app.

### Running Tests

```bash
cd InventoryTracker.Tests
dotnet test
```

7 unit tests covering stock calculation and the zero-floor rule (all passing).

## Seed Data

Five products are seeded on first run, each deliberately covering a different scenario relevant to the assignment's edge cases:

| Product | Stock | Reorder Level | Demonstrates |
|---|---|---|---|
| Hex Bolt M8 | 500 | 50 | Normal stock — no badge |
| Hex Nut M8 | 80 | 100 | Below reorder level |
| Flat Washer M8 | 40 | 40 | **Exactly at** reorder level — tests the inclusive `<=` boundary |
| Ball Bearing 6204-2RS | 0 | 10 | Zero stock — good candidate for testing that an Out movement is correctly rejected |
| O-Ring Seal | 60 | 25 | Soft-deleted (`IsActive = false`) — hidden from the active product list |

## Screens

1. **Product List** (`/Products`) — table of active products with SKU, name, current stock, and reorder level; a "Low stock" badge on items at or below their reorder level; text search over SKU and name.
2. **Product Detail** (`/Products/Details/{id}`) — product info, current stock, full chronological movement history, and a form to record a new movement (In/Out, quantity, note).
3. **Create / Edit Product** (`/Products/Create`, `/Products/Edit/{id}`) — add or edit product fields with full server-side validation.
4. **Dashboard** (`/Products/Dashboard`) — a small summary view (stretch goal, see below).

## Design Decisions

### Stock is never stored — always derived

`Product` has no `CurrentStock` column, by design. Stock is calculated on demand as `sum(In) − sum(Out)` from the `StockMovements` table (`StockService.GetStockLevelAsync` / `GetStockLevelsAsync`). This is the core requirement of the assignment: a stored, directly-editable stock field can silently be overwritten with an incorrect value, with no way to know it happened or why. An append-only movement log means every change is auditable, and the current balance can never disagree with its own history — it *is* its history, summed.

### Preventing negative stock — Serializable transactions

The main risk with a derived balance is a race condition: two concurrent "Out" requests can both read the same stock level before either writes, and both pass the "is there enough stock?" check independently — resulting in negative stock even though each check was individually correct at the moment it ran.

`StockService.RecordMovementAsync` wraps the read-check-write sequence in a database transaction using `IsolationLevel.Serializable`. This specifically guards against a **phantom read**: the operation sums an existing set of rows and then inserts into that same set, so `Repeatable Read` isolation would not be sufficient on its own — it only locks rows already read, not the range that a new insert would join. Serializable is the correct isolation level for this exact pattern.

**Trade-off:** Serializable isolation is pessimistic — it takes range locks and can reduce throughput under high contention, and can occasionally surface a `DbUpdateException` when two transactions genuinely conflict. This is handled gracefully and returned to the user as a friendly retry message rather than a raw exception or stack trace. For a tool at this scale, correctness was prioritized over throughput. At a larger scale with heavier concurrent write volume, this would be worth reconsidering in favor of optimistic concurrency (a rowversion column with a clean conflict message) or a queue-based approach to processing movements sequentially.

### SKU uniqueness — enforced at two layers, not one

A unique index on `Product.Sku` (`entity.HasIndex(p => p.Sku).IsUnique()`) is the actual guarantee, because only the database can correctly serialize concurrent writes. An `AnyAsync` check in the controller runs first, purely to produce a fast, friendly error message before ever reaching the database. But if two requests race past that check at the same moment, the database's unique constraint still catches the duplicate, and the resulting `DbUpdateException` is caught and converted into the same user-facing message — never a raw exception or stack trace, per the assignment's requirement.

### Soft delete over hard delete

`Product.IsActive` is used instead of physically deleting rows. A hard delete would either cascade and destroy the associated movement history (defeating the entire audit-trail design this assignment is built around), or require blocking deletion outright with no path forward for the user. `IsActive = false` keeps history fully intact, removes the product from the active list and search results, and is enforced further at the database level: the foreign key from `StockMovement` to `Product` uses `DeleteBehavior.Restrict`, so a product with any movement history cannot be hard-deleted even by accident.

### Query performance

`GetStockLevelsAsync` (used by both the product list and the dashboard) computes stock for **all** products in a single grouped query, rather than looping per product and querying each one individually — avoiding an N+1 query pattern that would otherwise grow linearly with the product count. A composite index on `(ProductId, CreatedUtc)` supports both the stock-sum calculation and the chronological movement history shown on the detail page.

## Assumptions

- SKU (50 chars), Name (75 chars), and Description (200 chars) length limits were chosen as reasonable bounds for this domain; the brief didn't specify exact lengths.
- "At or below reorder level" is treated as `currentStock <= reorderLevel` (inclusive), matching the brief's wording exactly. This boundary is deliberately covered by seed data — Flat Washer M8 sits exactly at its reorder level — since an off-by-one here (`<` instead of `<=`) would silently miss the exact-threshold case.
- An "Out" movement equal to current stock is allowed (stock can legitimately reach exactly zero); only movements that would take stock *below* zero are rejected.
- Movements are treated as immutable once recorded — correcting a mistake means posting a compensating movement, not editing or deleting existing history.
- A deactivated (`IsActive = false`) product cannot have new movements recorded against it, since it's no longer considered part of active inventory.

## Testing

Unit tests use EF Core's In-Memory provider for read-only queries (`GetStockLevelAsync`, `GetStockLevelsAsync`), since those don't require transactional behavior. Tests that exercise `RecordMovementAsync` use an in-memory SQLite connection instead, because the In-Memory provider doesn't support transactions and throws rather than silently skipping them — and `RecordMovementAsync` depends on a real transaction for its correctness guarantee.

This means the test suite validates the business logic correctly (the sum calculation, the zero-floor rejection, input validation, inactive-product rejection), but the actual behavior under truly concurrent, simultaneous requests was verified manually against LocalDB rather than in an automated test — reliably simulating two genuinely concurrent requests inside a unit test is a non-trivial exercise in its own right, and out of scope for the time available here.

## Stretch Goals

One attempted: **a tiny dashboard** (`/Products/Dashboard`) showing total active SKUs, count of low-stock items, and total units currently in stock — all computed using the same `IStockService.GetStockLevelsAsync()` that powers the product list, so there's a single source of truth for stock calculations across the whole app.

Others (pagination/sorting, CSV export, optimistic concurrency, a JSON API endpoint, structured logging) were not attempted, per the assignment's own guidance to prioritize a solid, fully-working core over a partially-built set of extras.

## What I'd Improve With More Time

- Optimistic concurrency (a rowversion column) as an alternative to serializable locking, to compare behavior and throughput under heavier write contention.
- Pagination and sorting on the product list, for realistic catalogue sizes beyond the current 5 seeded products.
- Integration tests that simulate genuinely concurrent requests against the zero-floor rule, rather than relying on manual verification against LocalDB.
- A CSV export of a product's movement history, and a simple JSON API endpoint for stock levels.
- Consistent `[Display]` labels across all forms (e.g. "Reorder Level" instead of the raw property name) — currently applied inconsistently.