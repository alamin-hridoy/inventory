# InventoryPilot

InventoryPilot is an ASP.NET Core MVC inventory management application. Users create inventories, define item fields and custom item ID formats, grant write access to other users, and manage items in table-first views.

## Features

- ASP.NET Core MVC web application written in C#.
- PostgreSQL database with Entity Framework Core ORM.
- Bootstrap-based responsive UI.
- Inventories and items displayed as tables by default.
- No per-row edit/delete button clutter in item tables; bulk actions use toolbar selection.
- Header search available across the app.
- Anonymous users can search and view inventories/items in read-only mode.
- Authenticated users can create inventories, add items, like items, and post discussions when permitted.
- Google and Facebook external login support.
- Admin page for viewing, blocking, unblocking, deleting users, and adding/removing the admin role.
- Admins can remove admin access from themselves.
- Owner/admin/write-access/public-write permission model.
- Personal page with owned inventories and write-access inventories tables.
- Inventory tabs: Items, Chat, Settings, Custom ID, Fields, Access, Stats, Export.
- Inventory autosave with optimistic version checks.
- Item optimistic locking with version checks.
- Markdown rendering for inventory descriptions and discussion posts.
- Cloud image upload through Cloudinary; only the cloud URL is stored in the database.
- Tag autocomplete.
- Access autocomplete by username and email, with sortable access list.
- Custom ID builder with reorder, remove, formatting help, live preview, and database uniqueness per inventory.
- Custom fields with reorder, title, description, show-in-table flag, and validation options.
- Discussion polling for near-real-time updates.
- One-like-per-user enforcement for items.
- English and Spanish UI language selection.
- Light and dark theme selection.
- PostgreSQL full-text search.

Additional features:

- Document/image previews for item link fields: JPG, PNG, WebP, GIF, and PDF.
- Email confirmation for form registration.
- Additional field tuning: required flag, max length, regex, numeric min/max.
- Select/list field type.
- Arbitrary number of custom fields.
- CSV export.

## Tech Stack

- .NET 8
- ASP.NET Core MVC
- Razor Views
- ASP.NET Core Identity
- Entity Framework Core
- PostgreSQL
- Bootstrap
- Markdig
- Cloudinary unsigned browser uploads
- Docker / Render-ready deployment


## Local Setup

### Prerequisites

- .NET 8 SDK
- Docker and Docker Compose

### 1. Start PostgreSQL

```bash
docker compose up -d
```

The included Compose file starts PostgreSQL on port `5432` with:

```text
Database: inventorypilot_dev
Username: inventorypilot
Password: inventorypilot
```

### 2. Create Local Development Config

```bash
cp appsettings.Development.example.json appsettings.Development.json
```

Then edit `appsettings.Development.json` with your local secrets. This file is ignored by Git.

Minimum local config for database only:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=inventorypilot_dev;Username=inventorypilot;Password=inventorypilot"
}
```

### 3. Optional Local Provider Config

Google/Facebook/Cloudinary/SMTP are only needed if you want to test those integrations locally.

```json
"Authentication": {
  "Google": {
    "ClientId": "your-google-client-id",
    "ClientSecret": "your-google-client-secret"
  },
  "Facebook": {
    "AppId": "your-facebook-app-id",
    "AppSecret": "your-facebook-app-secret"
  }
},
"Cloudinary": {
  "CloudName": "your-cloudinary-cloud-name",
  "UploadPreset": "your-unsigned-upload-preset"
},
"Email": {
  "From": "yourgmail@gmail.com",
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "yourgmail@gmail.com",
    "Password": "your-google-app-password"
  }
}
```

If SMTP is not configured, confirmation links are written to the application logs for local testing.

### 4. Build and Run

```bash
dotnet restore
dotnet build inventory.sln
dotnet run
```

Open the local URL printed by the app, usually:

```text
http://localhost:5080
```

The app applies migrations and seeds initial data on startup.

## Seeded Admin Account

The app seeds one confirmed admin account:

```text
Email: admin@inventorypilot.local
Password: Admin123!
```

It also seeds categories, tags, and a starter inventory.

## Render Deployment

Deploy as a Docker web service on Render.

Add these environment variables in the Render dashboard. Render variables use double underscores `__`; ASP.NET maps them to nested config keys.

```text
ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=...;Username=...;Password=...
Authentication__Google__ClientId=...
Authentication__Google__ClientSecret=...
Authentication__Facebook__AppId=...
Authentication__Facebook__AppSecret=...
Cloudinary__CloudName=...
Cloudinary__UploadPreset=...
Email__From=yourgmail@gmail.com
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__EnableSsl=true
Email__Smtp__Username=yourgmail@gmail.com
Email__Smtp__Password=your-google-app-password
Resend__ApiKey=...
Resend__From=InventoryPilot <onboarding@resend.dev>
Salesforce__LoginUrl=https://login.salesforce.com
Salesforce__ClientId=...
Salesforce__ClientSecret=...
Salesforce__Username=...
Salesforce__Password=...
Salesforce__SecurityToken=...
Integrations__TokenSecret=long-random-secret
SupportTickets__Provider=Dropbox
SupportTickets__AdminEmails=admin@example.com
SupportTickets__DropboxFolder=/inventorypilot-support
Dropbox__AccessToken=...
ASPNETCORE_ENVIRONMENT=Production
```

For OAuth providers, add these deployed callback URLs:

```text
https://your-render-app.onrender.com/signin-google
https://your-render-app.onrender.com/signin-facebook
```

For local OAuth testing, use:

```text
https://localhost:5443/signin-google
https://localhost:5443/signin-facebook
```

## Testing Notes

A reviewer can run the app locally with only Docker and .NET. The seeded admin account works immediately.

External integrations require provider credentials:

- Google OAuth credentials for Google login.
- Facebook app credentials for Facebook login.
- Cloudinary cloud name and unsigned preset for image upload.
- Resend or SMTP credentials for real email delivery.
- Salesforce connected app credentials for creating Account and Contact records from the user profile page.
- Dropbox access token for support ticket JSON uploads used by the Power Automate flow.

Without email credentials, confirmation links are written to the application logs for local testing.

## Third-Party Integrations

### Salesforce

Users can open their profile page and create a Salesforce Account with a linked Contact. Admins may do this for users they manage. Configure a direct access token, a connected app username/password flow, or a server-to-server client credentials flow.

For External Client Apps with client credentials enabled:

```text
Salesforce__AuthFlow=ClientCredentials
Salesforce__LoginUrl=https://login.salesforce.com
Salesforce__ClientId=...
Salesforce__ClientSecret=...
Salesforce__ApiVersion=v59.0
```

The Salesforce app must have client credentials flow enabled, an assigned run-as user, API access, and an OAuth scope such as `api`.

For username/password flow:

```text
Salesforce__AuthFlow=Password
Salesforce__LoginUrl=https://login.salesforce.com
Salesforce__ClientId=...
Salesforce__ClientSecret=...
Salesforce__Username=...
Salesforce__Password=...
Salesforce__SecurityToken=...
Salesforce__ApiVersion=v59.0
```

For short demos, you can also provide:

```text
Salesforce__InstanceUrl=...
Salesforce__AccessToken=...
```

### Odoo

Inventory owners/admins can open an inventory's Export tab and copy the aggregate API URL and token. The endpoint is:

```text
GET /api/inventory-aggregates?token=...
```

Set a strong shared signing secret in production:

```text
Integrations__TokenSecret=long-random-secret
```

An Odoo addon is included in `integrations/odoo_inventory_viewer`. Install it in Odoo, create an InventoryPilot Import record, paste the API URL and token, then click Import inventory. Odoo stores the inventory title, field list, item count, and aggregate values.

### Support Tickets / Power Automate

Authenticated users can use the Create support ticket link in the footer from any page. The app generates a JSON payload containing the current user, current page link, inventory title when available, priority, summary, and admin emails. With Dropbox configured, the JSON is uploaded to Dropbox for a Power Automate flow to watch and notify admins.

```text
SupportTickets__Provider=Dropbox
SupportTickets__AdminEmails=admin@example.com,owner@example.com
SupportTickets__DropboxFolder=/inventorypilot-support
Dropbox__AccessToken=...
```

Create the Power Automate flow separately with a Dropbox trigger for new files in that folder, then send email or mobile notifications using the JSON file content.

## Local Test Checklist

- Log in with the seeded admin account.
- Register a new form user and confirm email.
- Log in with Google and Facebook if credentials are configured.
- Create an inventory with Markdown description, category, image upload, tags, and public/private access.
- Add, reorder, edit, and remove custom ID elements; check preview and item generation.
- Add many custom fields, including text, multiline, number, link, boolean, and select fields.
- Configure field validation options and verify item validation.
- Add, edit, like, and delete items using table toolbar actions.
- Search from the header by inventory title, tag, item ID, and field values.
- Add discussion posts and verify polling refresh.
- Test access grants by adding a user via username/email autocomplete.
- Test My page owned and writable inventory tables.
- Test admin block/unblock/delete and admin role add/remove.
- Switch language and theme, then refresh.
- Log out and verify anonymous read-only search and viewing.
- Export inventory data as CSV.
