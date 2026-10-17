# GiysiGO Architecture (Modular Monolith)

GiysiGO is intentionally implemented as a **Modular Monolith**:

- **Single repository**
- **Single deployable application host** (`src/API/GiysiGo.API`)
- **Multiple business modules** with explicit boundaries

The agreed backend stack is ASP.NET Core / .NET 10, MSSQL and Dapper. Database objects live in the SSDT SQL project; repositories call stored procedures. Planned clients are Flutter for customers and React + TypeScript for stores.

These are module responsibilities, not a completion checklist. See [the documentation guide](README.md) and [shared implementation status](SHARED-START.md).

## Modules and responsibilities

- **Identity**: users, roles, authentication, addresses
- **Catalog**: garment types, services, pricing, active/passive visibility
- **Ordering**: cart, checkout, order and order items
- **Operations**: item inspection, service execution, delivery tasks and assignment using mock couriers in the MVP

## Boundary rules

- Modules do **not** directly use other modules' entities or repositories.
- Cross-module collaboration uses **in-process public contracts** and **integration events**.
- This is **not** a microservices setup: no separate deployable services, no external message bus requirement.

## Team branching model

For 4-person collaboration, use per-module feature branches:

- `feature/identity`
- `feature/catalog`
- `feature/ordering`
- `feature/operations`

See `docs/BRANCHING.md` for team workflow rules.
