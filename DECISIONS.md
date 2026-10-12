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
| The tracking service returns `null` for an unknown waybill instead of throwing | Not found is an expected answer, not a failure; the controller turns it into a 404, and the service stays free of HTTP |
| Domain-to-response mapping written by hand in one method, no AutoMapper | Nine fields don't need a library, and leaving out the phone and address is visible in the code instead of hidden in a mapping profile |
| Events sorted newest first by timestamp, then by recorded order | Matches the tie-break rule, so the first event in the list is always the one that decided the status |
| The service converts every timestamp to UTC | The contract promises `+00:00` whichever repository supplied the shipment; the in-memory one keeps the seed data's `+02:00` |
| OpenAPI document from .NET's built-in generator, shown with Swagger UI, in every environment | Microsoft's current approach, with the UI reviewers already know; it's on in production because the deployed API is a demo to try |
| Status code pages turn bodiless errors into ProblemDetails | An unknown route or a 405 returns the same error shape as the API's own 400 and 404, so the client has one shape to read |
| CORS policy allows only `GET` from the configured origins | The API has no other methods, so allowing more would only widen what a browser can send |

## Data

| Decision | Why |
|---|---|
| MongoDB Atlas, one document per shipment with events embedded | One lookup by waybill returns everything the page needs |
| A dedicated Atlas user with `readWrite` on `deliverytracking` only | The repo is public; a leaked credential can't reach any other data |
| Unique index on `waybill`, created at startup; waybills stored uppercase | The database enforces uniqueness, and lookups are case-insensitive without a collation |
| `cancelledAt` instead of deleting shipments | Keeps an audit trail |
| Waybills are never reused; the unique index covers cancelled shipments too | A waybill identifies one parcel forever. `--seed` upserts the six demo waybills, which also resets them |
| `--seed` writes demo content with `$set` and `_id`/`createdAt` with `$setOnInsert` | Re-running restores edited or cancelled demo shipments, but never changes their identity or creation time, and an unchanged run modifies nothing |
| Mongo settings validated when the app starts | A missing connection string stops startup with a message that says how to set it, instead of failing on the first request |
| The API only reads; domain-to-document mapping is used only to create seed documents | Fields the domain doesn't know about (`_id`, `createdAt`, `cancelledAt`) can't be overwritten by an update |
| Separate document classes mapped to domain classes | Keeps MongoDB attributes out of the domain model |
| Connection string from user-secrets locally and an environment variable in production | Never committed |

## Frontend

| Decision | Why |
|---|---|
| Vite + React 18 + MUI v5 + Axios | MUI v5 is required by the brief and is built for React 18 |
| Mock mode (`VITE_USE_MOCK=true`) serving local data that matches the contract | The client can be built and tested without the API running |
| React Router 7 with `/track/:waybill`, in declarative mode | A tracking link can be shared and survives a refresh. Version 8 needs React 19, and `useTracking` already loads the data, so route loaders aren't needed |
| API base URL from `VITE_API_BASE_URL`; CORS allows only configured client origins | Client and API are on different origins; nothing else can call the API from a browser |
| npm installs only package versions at least a week old (`min-release-age=7` in `client/.npmrc`) | Hijacked releases of popular packages are usually caught and pulled within days. `npm ci` still installs exactly what the lockfile pins. Needs npm 11.10 or newer; older npm warns and ignores the setting |
| `react-is` held at React 18's version by an npm override | MUI 5.18 asks for react-is 19, which doesn't recognise React 18 elements, so MUI's development warnings would never fire. MUI's installation guide prescribes this override for React 18 |
| The mock answers a bad or unknown waybill with the same Axios error and ProblemDetails body as the API (400 and 404) | The hook and components handle one error shape, so mock mode can't hide a bug in error handling |
| `useTracking` aborts its request with `AbortController` when the waybill changes or the page unmounts, and ignores any answer once its request is aborted | No work continues for a page that's gone, and a late answer for an old waybill, even one that arrived just before the abort, can't replace the current result |
| `useTracking` gives every request a new id (each waybill change and each retry) and derives `'loading'` from whether the stored result has the current id | The previous shipment never shows under a new waybill, even for one render or when returning to an earlier waybill, and no state is set synchronously inside an effect |
| Axios timeout of 30 seconds | A hung request becomes an error with a retry, and the wait is long enough for the free API to wake up |
| Body text 16–20px on phones and 14–18px from `md` up; pending steps in `grey[600]` | Larger text where the page is read on a phone, switching at the same width as the stepper; `grey[600]` is the lightest MUI grey with 4.5:1 text contrast on white |
| The search box checks the waybill format before navigating; a URL typed by hand goes straight to the API | A typo gets instant feedback without a round trip, while the server stays the authority on what's valid |
| Each step's state is exposed as `aria-current` and an icon with an accessible name, on `role="list"` items | Screen readers hear "Completed" or "Current step", not just a colour, and tests find steps the way a user does. Not `<ol>`, because MUI renders connectors between the steps |
| Buttons and chips are at least 44px high below `md`, set once in the theme | The minimum comfortable touch target, applied to every button and chip, including later ones |

## Testing and delivery

| Decision | Why |
|---|---|
| xUnit with `WebApplicationFactory`; Vitest with React Testing Library | Unit and integration tests cover every functional test the brief lists |
| Tests ship in the same pull request as the code they test | CI runs them on every pull request, so every change is checked, not just the last one |
| One hand-written in-memory repository in the test project, no mocking library | Tests never touch Atlas, and no test code ships in the API |
| Render for both halves from one `render.yaml`, with the database on Atlas | One platform and one config file; the static site loads instantly while the free API wakes up |
| GitHub Actions builds and tests the server, and lints, tests and builds the client, on every pull request and every push to `main` | Problems show up on the pull request, on a clean Linux machine, before the merge |
| Client tests run with mock mode forced on in the Vitest config, and the API client is spied on rather than replaced | No test touches the network, even with a local `.env.local`, and the hook still goes through the real mock client and its Axios errors |
| Real-mode client tests swap the exported Axios instance's adapter; tests use fake timers for the mock's 400 ms delay | The URL, base URL, timeout and response handling are checked without a network, and no test races a real clock on a slow CI machine |
| Component tests render with the app's theme and a `MemoryRouter`, fake `matchMedia` by screen width, and check colours with computed styles | Colour checks compare against the app's real palette and fail if the red disappears, not only if a class name changes. Orientation is the one check that uses MUI's documented class names, because it has no accessible signal |
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
