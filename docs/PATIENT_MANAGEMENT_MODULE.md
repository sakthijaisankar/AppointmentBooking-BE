# Module 2: Patient Management

**Dependency:** Module 1 (Authentication & Authorization)

## Business Rule

A **Patient** is a `User` with role `Patient`. The clinical profile in `Patients` is linked 1:1 via `Patients.UserId → Users.UserId`.

## Business Flow

```
Register (Module 1) → Role = Patient
    → Login → JWT with UserId
    → Create Patient Profile (POST /api/patients/profile)
    → Update Profile / Add Emergency Contact / Add Medical History / Upload Documents
    → PatientId available for Appointments & ML Priority modules
```

Staff (Admin, Receptionist, Doctor) can list and view patient records by `PatientId`.

## Database Schema

```
Users (UserId)
  └── Patients (PatientId, UserId UNIQUE)
        ├── PatientMedicalHistory
        ├── EmergencyContacts
        └── PatientDocuments
```

## SQL Scripts (run order)

| Script | Purpose |
|--------|---------|
| `003_Create_Patients.sql` | Patients table with UserId FK |
| `008_Create_PatientManagement_Module.sql` | Medical history, contacts, documents + migration |
| `Insert/005_Seed_Sample_Patient.sql` | Link patient user to profile |
| `Insert/008_Seed_Sample_Patient_Data.sql` | Sample contact & history |

## API Endpoints

| Method | Route | Role | Description |
|--------|-------|------|-------------|
| POST | `/api/patients/profile` | Patient | Create profile |
| GET | `/api/patients/me` | Patient | Get own full profile |
| PUT | `/api/patients/me` | Patient | Update profile |
| GET | `/api/patients` | Staff | Paginated list |
| GET | `/api/patients/{patientId}` | Staff | Get patient detail |
| POST | `/api/patients/me/emergency-contacts` | Patient | Add contact |
| GET/PUT/DELETE | `/api/patients/emergency-contacts/{id}` | Patient/Staff | Manage contacts |
| POST | `/api/patients/me/medical-history` | Patient | Add history |
| GET/PUT/DELETE | `/api/patients/medical-history/{id}` | Patient/Staff | Manage history |
| POST | `/api/patients/me/documents` | Patient | Upload document (multipart) |
| GET | `/api/patients/documents/{id}/download` | Patient/Staff | Download file |
| DELETE | `/api/patients/documents/{id}` | Patient/Staff | Soft-delete document |

## PatientId for Future Modules

| Module | FK Reference |
|--------|----------------|
| Appointments | `Appointments.PatientId` |
| ML Priority | `PatientPriorityClassifications.PatientId` |
| Billing (future) | `Invoices.PatientId` |

Extract own PatientId after login:
- JWT → `UserId` → `GET /api/patients/me` → `PatientId`

## Frontend Routes

| Route | Role |
|-------|------|
| `/patient/profile` | Patient |
| `/staff/patients` | Admin, Doctor, Receptionist |

## Folder Structure

```
src/
├── api/patientService.js
├── hooks/usePatient.js
├── pages/patients/
│   ├── PatientProfilePage.jsx
│   ├── PatientListPage.jsx
│   └── PatientPages.css
```
