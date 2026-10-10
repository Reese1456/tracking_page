# API contract and data model

The React client and the C# API are both built against this document. If either side needs a change, update this file in the same PR.

## Endpoints

### `GET /api/tracking/{waybill}`

Returns the tracking details for one shipment. A waybill is `WB` followed by 8 ASCII digits (0–9), for example `WB10000004`.

Input is forgiving: surrounding whitespace and lowercase are accepted. The input is trimmed and uppercased first, and the format check runs on that normalised value against `^WB[0-9]{8}$` (`[0-9]` rather than `\d`, because .NET's `\d` also matches non-ASCII digits). A value that fails gets 400; one that passes is looked up as-is. The response's `waybill` field and the 404 `detail` always use the uppercase form.

The client may run the same check before sending a request, to give instant feedback. That check is advisory and must use this pattern; the API's 400 is the authority. A `/track/<anything>` URL opened directly skips the client check and still reaches the API's 400.

**200 OK**

```json
{
  "waybill": "WB10000004",
  "status": "OutForDelivery",
  "stepIndex": 3,
  "isException": false,
  "exceptionReason": null,
  "receiver": { "name": "T. Mokoena", "city": "Johannesburg" },
  "estimatedDelivery": "2026-10-09",
  "lastUpdated": "2026-10-08T05:42:00+00:00",
  "events": [
    {
      "status": "OutForDelivery",
      "description": "Parcel out for delivery",
      "location": "Midrand Depot",
      "timestamp": "2026-10-08T05:42:00+00:00"
    }
  ]
}
```

Every field is always present and non-null, except `exceptionReason`.

| Field | Notes |
|---|---|
| `status` | One of `Packed`, `Processing`, `InTransit`, `OutForDelivery`, `Delivered`, `Exception`. Sent as a string, not a number |
| `stepIndex` | 0–4, the stepper step to highlight. `Delivered` and `Exception` both map to 4. The client uses this value and never calculates it |
| `isException` | `true` only when `status` is `Exception` |
| `exceptionReason` | A non-empty string whenever `isException` is `true`, otherwise `null`. It comes from the newest event, so a later non-exception event clears it |
| `receiver` | Name and city only. Phone numbers and street addresses are never returned |
| `estimatedDelivery` | Date only, `yyyy-MM-dd`. Unchanged once the parcel is delivered or has an exception |
| `lastUpdated` | The `timestamp` of the newest event |
| `events` | Newest first, ordered by `timestamp` (not by the order they were recorded). Always at least one |

**"Newest event"** means the event with the latest `timestamp`. If two events share a timestamp, the one recorded later (later in the stored `events` array) counts as newer. This rule decides `status`, `stepIndex`, `isException`, `exceptionReason` and `lastUpdated`, and it's the same in every repository implementation.

**Timestamps** are ISO 8601 in UTC with an explicit offset, as .NET writes them: `2026-10-08T05:42:00+00:00`. That instant is 07:42 in Johannesburg (UTC+2). The client parses them as dates rather than comparing strings, and converts to Africa/Johannesburg time for display.

**400 Bad Request**: the waybill isn't in the `WB` + 8 digits format.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Invalid waybill format",
  "status": 400,
  "detail": "A waybill is WB followed by 8 digits, for example WB10000004."
}
```

**404 Not Found**: the format is valid but no active (non-cancelled) shipment has that waybill.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Shipment not found",
  "status": 404,
  "detail": "No shipment was found for waybill WB99999999."
}
```

**Other failures**: an unhandled server error returns 500 as ProblemDetails with a generic title and no internal details (no stack trace). The client treats a 500, any other unexpected status, a timeout or a network failure as an error and offers a retry.

All errors use the standard `ProblemDetails` shape (RFC 9457) with content type `application/problem+json`, so the client can always read `title` and `detail`.

### `GET /api/tracking`

Returns the waybills of all active (non-cancelled) shipments, sorted, so there is something to try. An empty list is a valid 200.

```json
["WB10000001", "WB10000002", "WB10000003", "WB10000004", "WB10000005", "WB10000006"]
```

This works because the database holds only demo data. A production system wouldn't expose this endpoint, since it would list every customer's tracking number.

## Calling the API from the client

The client reads the API's base URL from `VITE_API_BASE_URL`. The client and the API are served from different origins (separate Render services, or `localhost:5173` and the API's port locally), so the API allows cross-origin requests only from the origins listed in its `Cors:AllowedOrigins` setting.

## Demo data

Six shipments, `WB10000001` to `WB10000006`: one at each of `Packed`, `Processing`, `InTransit`, `OutForDelivery` and `Delivered`, plus one `Exception` with the reason "Recipient not available".

## MongoDB data model

Database `deliverytracking`, collection `shipments`. Each shipment is one document with its events embedded, so a single lookup by waybill returns everything the page needs.

```json
{
  "_id": { "$oid": "..." },
  "waybill": "WB10000004",
  "receiver": {
    "name": "T. Mokoena",
    "city": "Johannesburg",
    "phone": "...",
    "streetAddress": "..."
  },
  "estimatedDelivery": "2026-10-09",
  "events": [
    {
      "status": "OutForDelivery",
      "description": "Parcel out for delivery",
      "location": "Midrand Depot",
      "timestamp": { "$date": "2026-10-08T05:42:00Z" },
      "exceptionReason": null
    }
  ],
  "cancelledAt": null,
  "createdAt": { "$date": "2026-10-06T07:10:00Z" }
}
```

This is MongoDB Extended JSON: `$date` is a BSON Date (always UTC) and `$oid` is an ObjectId. `estimatedDelivery` is a plain `yyyy-MM-dd` string, because it's a date with no time of day. `cancelledAt` is a BSON Date once set.

| Rule | Why |
|---|---|
| The current status is not stored. It is derived from the newest event | One source of truth, matching how couriers send scan events |
| A dedicated database user with `readWrite` on `deliverytracking` only | The repository is public; a leaked credential can't reach other data |
| Unique index on `waybill`, created at startup | The database enforces uniqueness, and lookups are fast |
| Waybills stored uppercase | Case-insensitive lookup without a collation |
| Timestamps stored as BSON Dates in UTC, statuses as strings | Readable in Atlas, and avoids the driver storing `DateTimeOffset` as an array |
| `cancelledAt` set instead of deleting documents | Keeps an audit trail; queries exclude cancelled shipments |
| Separate `ShipmentDocument` classes, mapped to the domain `Shipment` in the repository | Keeps database attributes out of the domain classes |
| The API only reads shipments. Mapping from domain to document is used only to create the seed documents | `_id`, `createdAt` and `cancelledAt` aren't part of the domain, so there's no update path that could overwrite them |
| Waybills are never reused, so the unique index covers cancelled shipments too | A waybill identifies one parcel forever, as with real couriers |
| Connection string from user-secrets locally and an environment variable in production | Never committed |
