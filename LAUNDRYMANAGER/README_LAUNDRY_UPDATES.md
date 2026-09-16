# iWS Laundry Manager - Updated

## What was changed

- Residence-manager registration at `/Admin/Register`.
- Residence managers use Gmail addresses and get email verification.
- Managers create a residence name and address; each manager sees only their residence.
- Added the requested demo residence **Brandon Mansions**. Its demo address can be replaced when the residence manager claims it.
- Student registration now requires selecting a residence from a dropdown.
- Student dashboards show only machines belonging to the student's residence.
- Admin dashboard shows the residence name and address at the top.
- Machine management is residence-scoped.
- Scheduled bookings show the exact start/end time and duration.
- "Book Machine Now" is disabled when a future scheduled reservation exists.
- Scheduled bookings lock the machine when the slot starts.
- The student has **10 minutes** from the scheduled start to confirm arrival.
- Unconfirmed bookings become no-shows and the machine is released automatically.
- Added booking confirmation state and no-show tracking.
- Residence-specific email notifications are sent when an admin changes a machine status.
- Added periodic dashboard refresh so status changes become visible without manually reopening the page.
- Removed the security-question flow and the `SecurityAnswer` model field.
- Fixed several security/flow bugs: role-based admin access, residence isolation, CSRF protection on POST forms, broken hard-coded admin checks, and hard-coded student email in the shared layout.
- Email configuration no longer stores an SMTP password in source control.

## Database

A new migration was added:

`20260914190000_AddResidenceManagerAndBookingVerification`

Run:

```powershell
dotnet ef database update
```

If EF tools are not installed:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef database update
```

## Gmail email setup

The application expects Gmail SMTP on port 587.

Set the SMTP app password outside the source code. For example, with an environment variable on Windows PowerShell:

```powershell
$env:LAUNDRY_SMTP_PASSWORD="your-gmail-app-password"
dotnet run
```

Or put the secret in ASP.NET Core User Secrets during development.

The Gmail account used by `EmailSettings:SenderEmail` must have 2-Step Verification enabled and use a Gmail App Password. Do not use the normal Gmail account password.

## Push notifications

The VAPID private key is also read from:

`LAUNDRY_VAPID_PRIVATE_KEY`

The public key and subject remain in `appsettings.json`.

## Important security action

The original uploaded project contained an SMTP credential and a VAPID private key in `appsettings.json`. Those credentials should be **revoked/rotated** before deploying the updated application, because they have been exposed in source code. Replace them with new credentials stored as environment variables or User Secrets.

## Google sign-in

This update supports manager accounts using Gmail addresses and password authentication. It does **not** pretend that a Gmail address is the same thing as Google OAuth. If you want a real **Continue with Google** button, a Google Cloud OAuth client ID/secret is required and should be configured separately.
