# Module 1: Authentication & Authorization

## Business Flow

```
Registration (Patient self-service OR Admin creates staff)
    → Validate credentials
    → Hash password (BCrypt)
    → Create User record
    → Assign Role via UserRoles
    → Generate JWT (UserId + Roles in claims)
    → Client stores token

Login
    → Verify username/password
    → Generate JWT
    → Return token + UserProfile (includes UserId)

Authenticated Request
    → JWT validated (issuer, audience, signature, expiry)
    → UserId extracted from ClaimTypes.NameIdentifier
    → Role checked via [Authorize(Roles=...)] or policies

Logout
    → Client discards token (stateless JWT)

Change Password (authenticated)
    → Verify current password → update hash

Forgot/Reset Password
    → Create PasswordResetToken → (email in prod) → validate token → set new password

Update Profile (authenticated)
    → Update FullName, Email, PhoneNumber on Users
```

## Roles & Permissions

| Role | Description | Key Access |
|------|-------------|------------|
| Admin | Full system access | Create users, all modules |
| Receptionist | Front desk | Appointments, patient priority |
| Doctor | Clinical staff | Appointments, priority override |
| Patient | Self-service | Own profile, book appointments |

## Database Schema

```
Roles (RoleId PK)
Users (UserId PK) ──< UserRoles >── Roles
Users ──< PasswordResetTokens
```

## SQL Scripts (run in order)

1. `002_Create_Users_And_Roles.sql`
2. `007_Create_PasswordResetTokens.sql`
3. `Insert/001_Seed_Roles.sql`
4. `Insert/007_Seed_Sample_Users.sql`
5. `Update/002_Update_Roles_Remove_Nurse.sql` (if upgrading)

## API Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/register` | Anonymous | Register (Patient default) |
| POST | `/api/auth/login` | Anonymous | Login, get JWT |
| POST | `/api/auth/logout` | JWT | Acknowledge logout |
| GET | `/api/auth/profile` | JWT | Get profile |
| PUT | `/api/auth/profile` | JWT | Update profile |
| POST | `/api/auth/change-password` | JWT | Change password |
| POST | `/api/auth/forgot-password` | Anonymous | Request reset token |
| POST | `/api/auth/reset-password` | Anonymous | Reset with token |
| POST | `/api/auth/admin/users` | Admin | Create user with role |
| GET | `/api/auth/roles` | Anonymous | List roles |

## UserId Reference for Future Modules

All modules should use `UserId` from JWT claim `ClaimTypes.NameIdentifier`:

```csharp
var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
```

Used in: Patients.CreatedByUserId, Appointments.CreatedByUserId, PatientPriorityClassifications.ClassifiedByUserId, etc.

## Sample Credentials (after seed)

| Username | Password | Role |
|----------|----------|------|
| admin | Admin@123 | Admin |
| receptionist | Admin@123 | Receptionist |
| doctor | Admin@123 | Doctor |
| patient | Admin@123 | Patient |

## Frontend Routes

| Route | Access |
|-------|--------|
| `/login` | Public |
| `/register` | Public |
| `/forgot-password` | Public |
| `/reset-password` | Public |
| `/dashboard` | Authenticated |
| `/profile` | Authenticated |
| `/admin/patient-priority/:id` | Admin, Doctor, Receptionist |
