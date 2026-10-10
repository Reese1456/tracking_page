# Decisions

One line per decision and the reason for it. Newest decisions are added at the bottom of their section.

## Architecture

| Decision | Why |
|---|---|
| One repo with `server/` and `client/` | One link shows both halves, and one PR can change the contract on both sides |
| ASP.NET Core Web API on .NET 10 (LTS) with controllers | Controllers map onto classes and attributes, which are easier to read and explain than minimal API lambdas |
| Layers: controller → `ITrackingService` → `IShipmentRepository` | Each layer has one job, and the data source can change without touching business logic |
| The API owns all delivery logic; React only displays it | Status rules are written and tested once, in C# |
| Current status derived from the newest tracking event, never stored | One source of truth that can't disagree with the event history, matching how couriers send scan events |
| Step index mapped in the backend by `StatusStepMapper` | A pure function tested once; the client uses the value instead of re-implementing the rule |
| Errors as `ProblemDetails`: 400 for a bad waybill format, 404 for an unknown waybill | A standard error shape the client can always read |
| Receiver returned as name and city only | A public tracking page shouldn't expose phone numbers or addresses (POPIA) |

## Data

| Decision | Why |
|---|---|
| MongoDB Atlas, one document per shipment with events embedded | One lookup by waybill returns everything the page needs |
| A dedicated Atlas user with `readWrite` on `deliverytracking` only | The repo is public; a leaked credential can't reach any other data |
| Unique index on `waybill`, created at startup; waybills stored uppercase | The database enforces uniqueness, and lookups are case-insensitive without a collation |
| `cancelledAt` instead of deleting shipments | Keeps an audit trail |
| Separate document classes mapped to domain classes | Keeps MongoDB attributes out of the domain model |
| Connection string from user-secrets locally and an environment variable in production | Never committed |

## Frontend

| Decision | Why |
|---|---|
| Vite + React 18 + MUI v5 + Axios | MUI v5 is required by the brief and is built for React 18 |
| Mock mode (`VITE_USE_MOCK=true`) serving local data that matches the contract | The client can be built and tested without the API running |

## Testing and delivery

| Decision | Why |
|---|---|
| xUnit with `WebApplicationFactory`; Vitest with React Testing Library | Unit and integration tests cover every functional test the brief lists |
| Tests ship in the same pull request as the code they test | CI checks every change, not just the last one |
| One hand-written in-memory repository in the test project, no mocking library | Tests never touch Atlas, and no test code ships in the API |
| Render for both halves from one `render.yaml`, with the database on Atlas | One platform and one config file; the static site loads instantly while the free API wakes up |

## Process

| Decision | Why |
|---|---|
| API contract written before any code (`docs/api-contract.md`) | Both halves build against the same target |
| One branch, issue and pull request per part, merged with merge commits | Every change is reviewed and tested before `main`, and the history shows each step |
| Conventional Commits with a body explaining why | The history explains itself |
| AI assistance through Claude Code with committed rules (`CLAUDE.md`) | Anyone can see what the assistant was told |
| Claude is denied `git commit`, `git push` and `gh pr merge` | Every commit and merge is made by a person who read the change |
| Hand-written `.gitignore` instead of the 400-line Visual Studio template | Every line can be explained |
