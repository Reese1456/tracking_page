# Diagrams

GitHub renders these Mermaid diagrams directly. To export one as an image, paste it into [mermaid.live](https://mermaid.live).

## System overview

```mermaid
flowchart LR
    user(["Customer<br/>tracks a parcel in a browser"])
    subgraph system["Delivery Tracking system"]
        spa["React SPA<br/>Vite, MUI v5<br/>Render static site"]
        api["Tracking API<br/>ASP.NET Core, .NET 10<br/>Render web service, Docker"]
        db[("MongoDB Atlas<br/>deliverytracking database")]
    end
    user -->|"Opens /track/:waybill over HTTPS"| spa
    spa -->|"GET /api/tracking/{waybill}<br/>JSON over HTTPS"| api
    api -->|"Find shipment by waybill<br/>MongoDB.Driver"| db
```

## Request flow

```mermaid
sequenceDiagram
    autonumber
    actor U as Customer
    participant SPA as React SPA
    participant C as TrackingController
    participant S as TrackingService
    participant R as MongoShipmentRepository
    participant DB as MongoDB Atlas

    U->>SPA: Enter waybill or open /track/:waybill
    SPA->>SPA: Light format check, show loading state
    SPA->>C: GET /api/tracking/{waybill}
    alt Invalid waybill format
        C-->>SPA: 400 ProblemDetails
        SPA-->>U: Show the expected format
    else Valid format
        C->>S: GetTrackingAsync(waybill)
        S->>R: GetByWaybillAsync(waybill)
        R->>DB: Find by waybill where cancelledAt is null
        DB-->>R: ShipmentDocument or nothing
        R-->>S: Shipment or null
        alt Not found
            S-->>C: null
            C-->>SPA: 404 ProblemDetails
            SPA-->>U: Show not found
        else Found
            S->>S: Derive status from latest event, map step index, build DTO
            S-->>C: TrackingResponse
            C-->>SPA: 200 JSON
            SPA-->>U: Render stepper, cards and history
        end
    end
    Note over SPA,DB: Network error, timeout or 500 - the SPA shows an error state with a retry button
```

## Backend classes

```mermaid
classDiagram
    namespace Api {
        class TrackingController {
            +GetTracking(string waybill, CancellationToken ct) Task~IActionResult~
            +GetDemoWaybills(CancellationToken ct) Task~IActionResult~
        }
    }
    namespace Application {
        class ITrackingService {
            <<interface>>
            +GetTrackingAsync(string waybill, CancellationToken ct) Task~TrackingResponse?~
            +GetDemoWaybillsAsync(CancellationToken ct) Task~IReadOnlyList~string~~
        }
        class TrackingService {
            -IShipmentRepository repository
            -ToResponse(Shipment shipment) TrackingResponse
        }
        class IShipmentRepository {
            <<interface>>
            +GetByWaybillAsync(string waybill, CancellationToken ct) Task~Shipment?~
            +GetAllWaybillsAsync(CancellationToken ct) Task~IReadOnlyList~string~~
        }
        class WaybillFormat {
            <<static>>
            +IsValid(string input) bool
            +Normalize(string input) string
        }
        class TrackingResponse {
            <<record>>
            +Waybill string
            +Status TrackingStatus
            +StepIndex int
            +IsException bool
            +ExceptionReason string?
            +EstimatedDelivery DateOnly
            +LastUpdated DateTimeOffset
        }
        class ReceiverDto {
            <<record>>
            +Name string
            +City string
        }
        class TrackingEventDto {
            <<record>>
            +Status TrackingStatus
            +Description string
            +Location string
            +Timestamp DateTimeOffset
        }
    }
    namespace Domain {
        class Shipment {
            +Waybill string
            +Receiver Receiver
            +EstimatedDelivery DateOnly
            -events List~TrackingEvent~
            +Events IReadOnlyList~TrackingEvent~
            +/CurrentEvent TrackingEvent
            +/CurrentStatus TrackingStatus
            +/LastUpdated DateTimeOffset
            +Shipment(string waybill, Receiver receiver, DateOnly estimatedDelivery, TrackingEvent initialEvent)
            +AddEvent(TrackingEvent trackingEvent) void
        }
        class TrackingEvent {
            +Status TrackingStatus
            +Description string
            +Location string
            +Timestamp DateTimeOffset
            +ExceptionReason string?
        }
        class Receiver {
            +Name string
            +City string
            +Phone string
            +StreetAddress string
        }
        class TrackingStatus {
            <<enumeration>>
            Packed
            Processing
            InTransit
            OutForDelivery
            Delivered
            Exception
        }
        class StatusStepMapper {
            <<static>>
            +GetStepIndex(TrackingStatus status) int
            +IsException(TrackingStatus status) bool
        }
    }
    namespace Infrastructure {
        class MongoShipmentRepository {
            -IMongoCollection~ShipmentDocument~ collection
        }
        class ShipmentDocument {
            +Id ObjectId
            +Waybill string
            +CancelledAt DateTime?
            +CreatedAt DateTime
        }
    }
    namespace Tests {
        class InMemoryShipmentRepository {
            -Dictionary~string, Shipment~ shipments
        }
    }
    TrackingController --> ITrackingService : uses
    TrackingController ..> WaybillFormat : validates with
    ITrackingService <|.. TrackingService
    TrackingService --> IShipmentRepository : uses
    TrackingService ..> StatusStepMapper : uses
    TrackingService ..> TrackingResponse : creates
    TrackingResponse "1" *-- "1" ReceiverDto
    TrackingResponse "1" *-- "1..*" TrackingEventDto
    IShipmentRepository <|.. MongoShipmentRepository
    IShipmentRepository <|.. InMemoryShipmentRepository
    MongoShipmentRepository ..> ShipmentDocument : maps to and from Shipment
    Shipment "1" *-- "1..*" TrackingEvent : events
    Shipment "1" *-- "1" Receiver
    TrackingEvent --> TrackingStatus
    StatusStepMapper ..> TrackingStatus
    note for TrackingController "GET /api/tracking/{waybill} returns 200, 400 or 404\nGET /api/tracking returns the demo waybills"
    note for InMemoryShipmentRepository "Lives in the test project"
```

Notation: `/` marks a derived value (computed, never stored), `1..*` means a shipment always has at least one event, and `Phone` and `StreetAddress` never leave the API because `ReceiverDto` doesn't include them.
