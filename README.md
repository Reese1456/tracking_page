# Delivery Tracking

A single-page parcel-tracking app. Enter a waybill number to see the parcel's progress on a five-step stepper (Order Packed, Processing, In Transit, Out for Delivery, Delivered or Delivery Exception), the shipment details and its scan history. The ASP.NET Core API (.NET 10) owns all delivery logic and stores shipments in MongoDB Atlas; the React client (Vite, MUI v5) displays what the API returns.

Work in progress. The full README, with setup instructions and live links, comes with the final part.

- API contract and data model: [docs/api-contract.md](docs/api-contract.md)
- Architecture diagrams: [docs/diagrams.md](docs/diagrams.md)
- Decisions and reasons: [DECISIONS.md](DECISIONS.md)
