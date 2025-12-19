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
  - Staff view/update as a “phiếu”: `POST/GET/PUT /api/inspection-forms`
  - Staff list forms by booking/verification: `GET /api/inspection-forms/booking/{id}`, `GET /api/inspection-forms/verification/{id}`
  - Row media (photos): `GET /api/inspections/{id}`, `PUT /api/inspections/{id}` (multipart)

## Run migration

After pulling code, generate and apply an EF Core migration (PostgreSQL):

```bash
dotnet ef migrations add AddInspectionChecklistTemplates -p CamRent_Infrastructure -s CamRent_Api
dotnet ef database update -p CamRent_Infrastructure -s CamRent_Api
```

If you already ran a previous migration for checklist templates, create a new one for:
- removing template `BranchId`
- adding methods tables + selections
- adding inspection forms (`InspectionForm`) and `Inspection.FormId`
- removing `Inspection.BranchId` and `Inspection.HandoverType` (moved to `InspectionForm`)

## Notes

- Upload ảnh theo từng dòng (row) qua `PUT /api/inspections/{id}` (multipart).
- Checklist template rows bind to methods by `AllowedMethodIds` (method IDs from `/api/inspection-methods`).
- For booking inspections, use `HandoverType` to distinguish pickup vs return (before renter takes vs after renter returns).
