# Delivery Tracking: project rules

## Project
Take-home for a junior developer role: a delivery-tracking SPA. The C# API
owns all delivery logic; the React client only displays what the API returns.
Spec: docs/api-contract.md (endpoints, errors, MongoDB data model).
Decisions and reasons: DECISIONS.md.

## Stack
- server/: ASP.NET Core Web API (.NET 10), controllers, xUnit,
  MongoDB.Driver against MongoDB Atlas
- client/: Vite, React 18, MUI v5, Axios, React Router, Vitest,
  React Testing Library

## Architecture rules
- Controller -> ITrackingService -> IShipmentRepository. Controllers only
  reject a badly formatted waybill (400) and map service results to status
  codes; all other logic lives in the service.
- Current status is derived from the newest TrackingEvent (latest timestamp;
  on a tie, the one recorded later). Never store it.
- Step index mapping lives only in StatusStepMapper. The client never computes it.
- Errors use ProblemDetails. 400 = bad waybill format, 404 = not found.
- API responses never include receiver phone numbers or street addresses.

## MongoDB rules
- Database: deliverytracking. Collection: shipments. One document per
  shipment with events embedded.
- Domain classes have no MongoDB attributes. The repository maps
  ShipmentDocument <-> Shipment.
- Unique index on waybill. Waybills stored uppercase.
- Shipments are never deleted. Removal sets cancelledAt; queries exclude
  cancelled shipments.
- IMongoClient is a Singleton.
- The connection string comes from configuration (user-secrets locally,
  environment variable in production). Never commit it.
- Tests use InMemoryShipmentRepository (in the test project) and never
  touch Atlas. Integration tests replace IShipmentRepository, remove the
  index-creation hosted service and use dummy mongodb://localhost settings.

## Code style
- Prefer clear, conventional code over clever code.
- Comments explain why, never what. No line-by-line narration and no
  commented-out code.
- XML doc comments only where they add something the name doesn't, such as
  derived values or privacy filtering. Don't restate member names.

## Workflow
- Plan before coding and wait for approval at the start of each part.
- Make one commit-sized change at a time, then stop for review. The
  developer stages, commits, pushes and merges. Never run git commit,
  git push or gh pr merge.
- Only change files in the scope of the current task. Ask before touching others.
- Never add a NuGet or npm package without asking and explaining why.
- Before handing back, run the build and the relevant tests and show the result.
- Tests ship in the same part as the code they test. If a test exposes a
  bug, stop and report it instead of changing the test.
- Commit messages follow Conventional Commits: `type(scope): imperative
  subject` (72 characters max), blank line, body explaining what and why
  wrapped at 72.
- Branches are `<type>/<short-name>` from main, merged by PR with a merge commit.

## Commands
- Backend (from server/): dotnet build | dotnet test |
  dotnet run --project DeliveryTracking.Api
- Seed Atlas: dotnet run --project DeliveryTracking.Api -- --seed
- Frontend (from client/): npm run dev | npm test | npm run build | npm run lint
