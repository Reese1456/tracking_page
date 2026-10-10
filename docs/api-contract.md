# API contract and data model

The React client and the C# API are both built against this document. If either side needs a change, update this file in the same PR.

## Endpoints

### `GET /api/tracking/{waybill}`

Returns the tracking details for one shipment. A waybill is `WB` followed by 8 digits, for example `WB10000004`. The lookup is case-insensitive and ignores surrounding whitespace.

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
  "lastUpdated": "2026-10-08T07:42:00Z",
  "events": [
    {
      "status": "OutForDelivery",
      "description": "Parcel out for delivery",
      "location": "Midrand Depot",
      "timestamp": "2026-10-08T07:42:00Z"
    }
  ]
}
```

| Field | Notes |
|---|---|
| `status` | One of `Packed`, `Processing`, `InTransit`, `OutForDelivery`, `Delivered`, `Exception`. Sent as a string, not a number |
| `stepIndex` | 0–4, the stepper step to highlight. `Delivered` and `Exception` both map to 4. The client uses this value and never calculates it |
| `isException` | `true` only when `status` is `Exception` |
| `exceptionReason` | Set only when `isException` is `true`, otherwise `null` |
| `receiver` | Name and city only. Phone numbers and street addresses are never returned |
| `estimatedDelivery` | Date only, `yyyy-MM-dd` |
| `lastUpdated` | Timestamp of the newest event, ISO 8601 in UTC |
| `events` | Newest first. Always at least one |

**400 Bad Request**: the waybill isn't in the `WB` + 8 digits format.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Invalid waybill format",
  "status": 400,
  "detail": "A waybill is WB followed by 8 digits, for example WB10000004."
}
```

**404 Not Found**: the format is valid but no active shipment has that waybill.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Shipment not found",
  "status": 404,
  "detail": "No shipment was found for waybill WB99999999."
}
```

Errors use the standard `ProblemDetails` shape (RFC 9457), so the client can always read `title` and `detail`.

### `GET /api/tracking`

Returns the demo waybills, sorted, so there is something to try.

```json
["WB10000001", "WB10000002", "WB10000003", "WB10000004", "WB10000005", "WB10000006"]
```

## Demo data

Six shipments, `WB10000001` to `WB10000006`: one at each of `Packed`, `Processing`, `InTransit`, `OutForDelivery` and `Delivered`, plus one `Exception` with the reason "Recipient not available".

## MongoDB data model

Database `deliverytracking`, collection `shipments`. Each shipment is one document with its events embedded, so a single lookup by waybill returns everything the page needs.

```json
{
  "_id": "ObjectId(...)",
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
      "timestamp": "2026-10-08T05:42:00Z",
      "exceptionReason": null
    }
  ],
  "cancelledAt": null,
  "createdAt": "2026-10-06T07:10:00Z"
}
```

| Rule | Why |
|---|---|
| The current status is not stored. It is derived from the newest event | One source of truth, matching how couriers send scan events |
| A dedicated database user with `readWrite` on `deliverytracking` only | The repository is public; a leaked credential can't reach other data |
| Unique index on `waybill`, created at startup | The database enforces uniqueness, and lookups are fast |
| Waybills stored uppercase | Case-insensitive lookup without a collation |
| Timestamps stored as UTC dates, statuses as strings | Readable in Atlas, and avoids the driver storing `DateTimeOffset` as an array |
| `cancelledAt` set instead of deleting documents | Keeps an audit trail; queries exclude cancelled shipments |
| Separate `ShipmentDocument` classes, mapped to the domain `Shipment` in the repository | Keeps database attributes out of the domain classes |
| Connection string from user-secrets locally and an environment variable in production | Never committed |
