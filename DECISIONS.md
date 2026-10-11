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
| The controller rejects a bad waybill format; the waybill is trimmed and uppercased before lookup | Input stays forgiving (`wb10000004 ` works) while a malformed request still gets a clear 400 |
| Timestamps returned in UTC as ISO 8601; the client converts to Johannesburg time | One unambiguous instant on the wire, local time only for display |
| `GET /api/tracking` lists every active waybill | Fine while the database holds only demo data; a real system wouldn't expose it |
| Receiver returned as name and city only | A public tracking page shouldn't expose phone numbers or addresses (POPIA) |

## Data

| Decision | Why |
|---|---|
| MongoDB Atlas, one document per shipment with events embedded | One lookup by waybill returns everything the page needs |
| A dedicated Atlas user with `readWrite` on `deliverytracking` only | The repo is public; a leaked credential can't reach any other data |
| Unique index on `waybill`, created at startup; waybills stored uppercase | The database enforces uniqueness, and lookups are case-insensitive without a collation |
| `cancelledAt` instead of deleting shipments | Keeps an audit trail |
| Waybills are never reused; the unique index covers cancelled shipments too | A waybill identifies one parcel forever. `--seed` upserts the six demo waybills, which also resets them |
| The API only reads; domain-to-document mapping is used only to create seed documents | Fields the domain doesn't know about (`_id`, `createdAt`, `cancelledAt`) can't be overwritten by an update |
| Separate document classes mapped to domain classes | Keeps MongoDB attributes out of the domain model |
| Connection string from user-secrets locally and an environment variable in production | Never committed |

## Frontend

| Decision | Why |
|---|---|
| Vite + React 18 + MUI v5 + Axios | MUI v5 is required by the brief and is built for React 18 |
| Mock mode (`VITE_USE_MOCK=true`) serving local data that matches the contract | The client can be built and tested without the API running |
| React Router with `/track/:waybill` | A tracking link can be shared and survives a refresh |
| API base URL from `VITE_API_BASE_URL`; CORS allows only configured client origins | Client and API are on different origins; nothing else can call the API from a browser |
| npm installs only package versions at least a week old (`min-release-age=7` in `client/.npmrc`) | Hijacked releases of popular packages are usually caught and pulled within days. `npm ci` still installs exactly what the lockfile pins. Needs npm 11.10 or newer; older npm warns and ignores the setting |
| `react-is` held at React 18's version by an npm override | MUI 5.18 asks for react-is 19, which doesn't recognise React 18 elements, so MUI's development warnings would never fire. MUI's installation guide prescribes this override for React 18 |

## Testing and delivery

| Decision | Why |
|---|---|
| xUnit with `WebApplicationFactory`; Vitest with React Testing Library | Unit and integration tests cover every functional test the brief lists |
| Tests ship in the same pull request as the code they test | CI runs them on every pull request, so every change is checked, not just the last one |
| One hand-written in-memory repository in the test project, no mocking library | Tests never touch Atlas, and no test code ships in the API |
| Render for both halves from one `render.yaml`, with the database on Atlas | One platform and one config file; the static site loads instantly while the free API wakes up |
| GitHub Actions builds and tests the server, and lints and builds the client, on every pull request and every push to `main` | Problems show up on the pull request, on a clean Linux machine, before the merge. Client tests join when Vitest is added |
| The API serves plain HTTP; HTTPS ends at Render's proxy | The public URL is still HTTPS, and the container needs no certificate or redirect of its own |

## Process

| Decision | Why |
|---|---|
| API contract written before any code (`docs/api-contract.md`) | Both halves build against the same target |
| One branch, issue and pull request per part, merged with merge commits | Every change is reviewed and tested before `main`, and the history shows each step |
| Conventional Commits with a body explaining why | The history explains itself |
| AI assistance through Claude Code with committed rules (`CLAUDE.md`) | Anyone can see what the assistant was told |
| Claude Code permission rules deny `git commit`, `git push` and `gh pr merge` | Every commit and merge is made by a person who read the change. The rules block the normal command forms; they're a policy, not a security boundary |
| Hand-written `.gitignore` instead of the 400-line Visual Studio template | Every line can be explained |
