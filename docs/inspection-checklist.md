# Inspection Checklist (Backend)

## What’s added

- DB entities for configurable checklist templates:
  - `InspectionChecklistTemplate` (per `ItemType`, optional `InspectionType` — global across system)
  - `InspectionChecklistSection`
  - `InspectionChecklistItem` (allowed methods via join table)
  - `InspectionMethod` (admin-defined “phương pháp thực hiện”)
  - `InspectionChecklistItemAllowedMethod` (bind methods to a checklist row)
  - `InspectionMethodSelection` (staff selected methods per `Inspection`)
- APIs:
  - Staff loads active template: `GET /api/inspection-checklists/active`
  - Admin CRUD templates: `GET/POST/PUT/DELETE /api/inspection-checklists`
  - Admin CRUD methods: `GET/POST/PUT/DELETE /api/inspection-methods`
  - Staff submits checklist result (creates many `Inspection` rows): `POST /api/inspections/checklist`

## Run migration

After pulling code, generate and apply an EF Core migration (PostgreSQL):

```bash
dotnet ef migrations add AddInspectionChecklistTemplates -p CamRent_Infrastructure -s CamRent_Api
dotnet ef database update -p CamRent_Infrastructure -s CamRent_Api
```

## Notes

- `POST /api/inspections/checklist` does not upload photos; upload per-row photos via existing `PUT /api/inspections/{id}` (multipart) using returned `InspectionIds`.
- Checklist template rows bind to methods by `AllowedMethodIds` (method IDs from `/api/inspection-methods`).
- For booking inspections, use `HandoverType` to distinguish pickup vs return (before renter takes vs after renter returns).
