# Google OAuth 2.0 Authentication & Configuration Guide

VitaCernita integrates with Google's Gmail REST API via OAuth 2.0. This guide details how to configure Google Cloud credentials, set up authentication, manage tokens, and adhere to development and production isolation policies.

---

## Table of Contents

1. [Authentication Architecture](#authentication-architecture)
2. [Google Cloud Console Setup (Step-by-Step)](#google-cloud-console-setup-step-by-step)
3. [CLI Commands: `login` & `logout`](#cli-commands-login--logout)
4. [Token Lifecycle & Background Refresh](#token-lifecycle--background-refresh)
5. [Credential & Token Storage](#credential--token-storage)
6. [Critical Policy: Production Environment Isolation](#critical-policy-production-environment-isolation)
7. [Headless, CI, and Testing Alternatives](#headless-ci-and-testing-alternatives)

---

## Authentication Architecture

Connecting to a personal Gmail account requires authorization from the account owner. Google OAuth 2.0 uses two distinct credential tiers:

| Credential | Purpose | Storage / Location |
| :--- | :--- | :--- |
| **OAuth Client Credentials** (`ClientId`, `ClientSecret`) | Identifies the VitaCernita application to Google's authorization servers. | `~/.config/vitacernita/credentials.json` |
| **User Authorization Tokens** (`access_token`, `refresh_token`) | Authorizes VitaCernita to access the specific user's mailbox labels, filters, and vacation responder. | `~/.config/vitacernita/tokens/` |

VitaCernita uses Google's official [`Google.Apis.Auth`](https://www.nuget.org/packages/Google.Apis.Auth) library (`GoogleWebAuthorizationBroker`) to handle the loopback local web server authorization flow ([RFC 8252](https://datatracker.ietf.org/doc/html/rfc8252)) and manage automatic refresh token rotation.

---

## Google Cloud Console Setup (Step-by-Step)

To connect VitaCernita to a live Gmail account, create a free OAuth 2.0 Client in Google Cloud:

### Step 1: Create a Google Cloud Project
1. Navigate to the [Google Cloud Console](https://console.cloud.google.com/).
2. Click the project dropdown in the top bar and select **New Project**.
3. Name your project (e.g. `VitaCernita`) and click **Create**.

### Step 2: Enable the Gmail API
1. In the left navigation menu, go to **APIs & Services > Library**.
2. Search for **Gmail API**.
3. Select **Gmail API** and click **Enable**.

### Step 3: Configure the OAuth Consent Screen
1. Go to **APIs & Services > OAuth consent screen**.
2. Select **External** (or **Internal** if using a Google Workspace domain) and click **Create**.
3. Fill in the required fields:
   - **App name**: `VitaCernita`
   - **User support email**: Your email address
   - **Developer contact email**: Your email address
4. Click **Save and Continue**.
5. On the **Scopes** page, click **Add or Remove Scopes** and add the following two scopes:
   - `https://www.googleapis.com/auth/gmail.settings.basic` *(to manage filters and vacation auto-reply)*
   - `https://www.googleapis.com/auth/gmail.labels` *(to manage custom mailbox labels)*
6. Click **Save and Continue**.
7. On the **Test users** page, click **Add Users** and enter the Gmail address of the account you wish to test with.
   > [!IMPORTANT]
   > While your Google Cloud app is in "Testing" status, only accounts explicitly added to **Test users** can sign in.
8. Click **Save and Continue**.

### Step 4: Create OAuth 2.0 Client ID
1. Go to **APIs & Services > Credentials**.
2. Click **Create Credentials** at the top and select **OAuth client ID**.
3. Under **Application type**, select **Desktop app**.
   > [!WARNING]
   > **Application type MUST be "Desktop app"** (not "Web application").
   > Desktop applications allow dynamic localhost loopback redirect URIs (`http://127.0.0.1:<ephemeral_port>`) without hardcoding port numbers in the Google Cloud Console. Selecting "Web application" will cause Google to reject requests with `redirect_uri_mismatch`.
4. Name the client (e.g. `VitaCernita CLI`) and click **Create**.
5. In the confirmation dialog, click **Download JSON** and save the file (e.g. `client_secret_xxx.json`).

---

## CLI Commands: `login` & `logout`

### Logging In (`vitacernita login`)

#### Method A: Using the Downloaded JSON File (Recommended)
Import your downloaded Google Cloud credentials file directly:
```bash
dotnet run --project src/VitaCernita -- login --credentials ~/Downloads/client_secret_xxx.json
```

#### Method B: Providing Client ID & Secret Flags
```bash
dotnet run --project src/VitaCernita -- login \
  --client-id "123456789-abcdef.apps.googleusercontent.com" \
  --client-secret "GOCSPX-xxxxxxxxxxxx"
```

#### Method C: Interactive Terminal Prompt
Running `vitacernita login` without arguments interactively prompts you to enter your Client ID and Client Secret once and securely stores them.

```bash
dotnet run --project src/VitaCernita -- login
```

Once credentials are provided:
1. VitaCernita writes them to `~/.config/vitacernita/credentials.json` with user-only (`0600`) permissions.
2. A browser window opens displaying the Google OAuth consent screen.
3. Upon approval, Google redirects to the local loopback listener.
4. VitaCernita writes the access and refresh tokens to `~/.config/vitacernita/tokens/`.

---

### Logging Out (`vitacernita logout`)

To remove cached user tokens:
```bash
# Log out all cached user sessions
dotnet run --project src/VitaCernita -- logout

# Log out a specific account
dotnet run --project src/VitaCernita -- logout --account user@example.com

# Clear tokens AND remove stored credentials.json
dotnet run --project src/VitaCernita -- logout --all
```

---

## Token Lifecycle & Background Refresh

VitaCernita automatically manages OAuth token expiration:
- **Valid Tokens**: If an access token is less than 1 hour old, VitaCernita reads it from the local cache with **zero network latency** and **no browser prompts**.
- **Expired Tokens**: When the access token expires, the client uses the long-lived `refresh_token` to retrieve a fresh access token from Google in the background and updates the cache.
- **Subsequent Commands**: Commands such as `vitacernita init --account user@example.com` and `vitacernita test --diff --user user@example.com` automatically utilize the cached session without requiring re-authentication.

---

## Credential & Token Storage

By default, VitaCernita stores authentication data in the platform user directory:

```
~/.config/vitacernita/ (or %APPDATA%/vitacernita on Windows)
├── credentials.json        # Client ID & Secret (Owner read/write: 0600)
├── gmail.lua               # User Lua configuration file
└── tokens/                 # Encrypted token store managed by FileDataStore
    └── Google.Apis.Auth.OAuth2.Responses.TokenResponse-user
```

### Security
On Unix/Linux/macOS platforms, `credentials.json` and the `tokens/` directory are written with `chmod 600` / `0600` permissions (restricted to the current OS user only).

---

## Critical Policy: Production Environment Isolation

> [!CAUTION]
> **`~/.config/vitacernita` is effectively LIVE PRODUCTION.**
> Automated test suites, CI/CD runners, and AI coding agents must **NEVER** read from, write to, or delete files inside `~/.config/vitacernita`.

### Isolation Mechanisms
1. **`VITACERNITA_CONFIG_DIR` Environment Variable**:
   Directs VitaCernita to read and write all configurations, credentials, and tokens from an isolated directory:
   ```bash
   export VITACERNITA_CONFIG_DIR="/tmp/test_vitacernita"
   dotnet run --project src/VitaCernita -- test
   ```
2. **`--config-dir <dir>` CLI Option**:
   All commands (`login`, `logout`, `init`, `test`) accept `--config-dir`:
   ```bash
   dotnet run --project src/VitaCernita -- init --config-dir /tmp/isolated_test
   ```
3. **Automated Test Guard (`ConfigPathResolver.IsTestEnvironment`)**:
   VitaCernita's internal path resolver detects when code is executed within an automated test runner (`VitaCernita.Tests`, `xunit`, `testhost`). In test mode, resolving paths without an explicit config directory automatically routes to an isolated temporary sandbox (`/tmp/vitacernita_test_isolated`) and strictly blocks interactive browser launches.

---

## Headless, CI, and Testing Alternatives

### 1. In-Memory Mock Account (`--mock`)
For unit testing, demonstrations, or syntax verification without credentials:
```bash
dotnet run --project src/VitaCernita -- init --mock -o /tmp/my_test.lua
dotnet run --project src/VitaCernita -- test --mock --diff
```

### 2. Ephemeral Access Token (`GMAIL_ACCESS_TOKEN`)
If you already have a bearer access token (e.g. from the Google OAuth 2.0 Playground):
```bash
export GMAIL_ACCESS_TOKEN="ya29.a0AfH6SM..."
dotnet run --project src/VitaCernita -- init --account user@example.com
```

### 3. Environment Variable Credentials
You can provide client credentials via environment variables rather than a file:
```bash
export GMAIL_CLIENT_ID="123456789-...apps.googleusercontent.com"
export GMAIL_CLIENT_SECRET="GOCSPX-..."
dotnet run --project src/VitaCernita -- login
```
