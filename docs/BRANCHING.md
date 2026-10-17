# Branching Strategy (4-Person Team)

Each developer works in one branch named after their module.

## Module branches

- `feature/identity` → @PnterNN
- `feature/catalog` → @sdnrkaleli
- `feature/ordering` → @AhmetEfe0
- `feature/operations` → @selincellkk

## Rules

- Nobody pushes directly to `main`.
- All changes go through Pull Request.
- CODEOWNERS review is required for module folders.
- Cross-module or `src/BuildingBlocks` changes require shared review.
- After a module PR is merged, update the module branch from `main` before starting more work.
- Shared changes such as issue #5 use a separate branch from current `main` (for example `feature/shared-contracts`) and a separate PR. They do not require an unfinished module PR to merge first; compare against open consumers and update whichever PR merges second.
