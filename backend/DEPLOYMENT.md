# Deployment Runbook — Student Council API

Production topology chosen for ~40–50 users on the **Azure for Students** ($100/yr) credit:

| Concern | Service | Why |
|---|---|---|
| Compute | **Azure Container Apps** (Consumption, scale-to-zero) | Mostly covered by the monthly free grant; pulls the image from GHCR |
| Database | **Neon** (free Postgres tier) | Keeps the credit untouched; the app already speaks Postgres |
| File uploads | **Azure Blob Storage** (private containers) | Pennies/year at this scale |
| Container image | **GHCR** (`ghcr.io/pchernaev/studentcouncilapp`) | Built + pushed by CI on `main` |
| Secrets | Container App secrets (env-injected) | Nothing sensitive is baked into the image |
| Push (FCM/APNs) | **Deferred** | No mobile client consumes it yet; the app degrades gracefully (logs instead of sends) |

Resource names used throughout (override as you like):

```
RG       = studentcouncil-rg
LOCATION = germanywestcentral      # Azure for Students restricts regions (see note below)
ENV      = studentcouncil-env
APP      = studentcouncil-api
STORAGE  = scfiles<unique>          # 3–24 lowercase alphanumerics, globally unique
IMAGE    = ghcr.io/pchernaev/studentcouncilapp:latest
```

---

## 1. Required runtime configuration

Set on the Container App as **secrets** (sensitive) and **env vars** (non-sensitive). Double underscore `__` maps to the nested config key.

| Key | Kind | Value |
|---|---|---|
| `ConnectionStrings__Default` | secret | Neon Npgsql connection string (see §3) |
| `Jwt__SigningKey` | secret | ≥256-bit random — `openssl rand -base64 48` |
| `Seed__AdminEmail` | env | Email of the first OrgPresident (the account you log in with) |
| `Seed__AdminPassword` | secret | Initial password (≥8 chars, 1 uppercase, 1 digit). `MustChangePassword` forces a change on first login |
| `Storage__Provider` | env | `AzureBlob` |
| `Storage__ConnectionString` | secret | Azure Storage account connection string |
| `ForwardedHeaders__Enabled` | env | `true` — honour the ingress's `X-Forwarded-Proto/For` |
| `ASPNETCORE_ENVIRONMENT` | env | `Production` |

Optional (email — temp passwords / reset links go to the log until set):

| Key | Kind | Value |
|---|---|---|
| `Email__Smtp__Host` / `__Username` | env | SMTP host / user |
| `Email__Smtp__Password` | secret | SMTP password |
| `Email__From` | env | From address |

> **Why `Seed__AdminPassword` matters:** in Production the app seeds the 4 departments + one `OrgPresident` on first boot. Without SMTP configured, the temporary password is *not* logged, so set a known one here to guarantee first login.

---

> **Region note:** the Azure for Students subscription enforces an *Allowed resource deployment regions*
> policy. Observed allowed set: `germanywestcentral, polandcentral, francecentral, swedencentral,
> austriaeast`. `westeurope` is **not** allowed for resources (the resource group may still be westeurope —
> that's only metadata). We use `germanywestcentral` (Frankfurt), closest to Varna.

## 2. Prerequisites

- **Azure for Students** activated (university email) → an active subscription.
- **Neon** account (free).
- Local tools: `az` (Azure CLI), `gh` (authenticated), `dotnet` + `dotnet-ef`.

```bash
brew install azure-cli
dotnet tool install --global dotnet-ef   # for the first migration run
az login                                  # opens a browser
az account show -o table                  # confirm the right subscription
```

---

## 3. Provision Neon (database)

1. Create a project at <https://console.neon.tech> (region close to Azure, e.g. EU).
2. Copy the connection string. Neon gives a URI:
   `postgresql://USER:PASSWORD@HOST/DB?sslmode=require`
3. Convert to the **Npgsql keyword form** used by `ConnectionStrings__Default`:

   ```
   Host=HOST;Database=DB;Username=USER;Password=PASSWORD;SSL Mode=Require
   ```

4. Apply the schema (the app does **not** self-migrate in Production):

   ```bash
   cd backend
   dotnet ef database update \
     --project src/StudentCouncil.Infrastructure \
     --startup-project src/StudentCouncil.Api \
     --connection "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require"
   ```

---

## 4. Provision Azure (compute + storage)

```bash
RG=studentcouncil-rg; LOC=germanywestcentral; ENV=studentcouncil-env; APP=studentcouncil-api
STORAGE=scfiles$RANDOM            # ensure it's globally unique + lowercase
IMAGE=ghcr.io/pchernaev/studentcouncilapp:latest

az extension add --name containerapp --upgrade
az provider register -n Microsoft.App --wait
az provider register -n Microsoft.OperationalInsights --wait

az group create -n $RG -l $LOC

# --- Blob storage: account + two private containers ---
az storage account create -n $STORAGE -g $RG -l $LOC \
  --sku Standard_LRS --allow-blob-public-access false
STORAGE_CONN=$(az storage account show-connection-string -n $STORAGE -g $RG -o tsv)
az storage container create -n task-documents --connection-string "$STORAGE_CONN"
az storage container create -n avatars        --connection-string "$STORAGE_CONN"

# --- Container Apps environment ---
az containerapp env create -n $ENV -g $RG -l $LOC
```

The GHCR package must be pullable. Either make the package **public** (it contains no secrets) after the first CI push, or pass a PAT with `read:packages` via `--registry-*` below.

```bash
JWT_KEY=$(openssl rand -base64 48)
ADMIN_PASS="$(openssl rand -base64 18)Aa1"     # guarantees length + upper + digit
NEON_CONN="Host=...;Database=...;Username=...;Password=...;SSL Mode=Require"

az containerapp create -n $APP -g $RG --environment $ENV \
  --image $IMAGE \
  --target-port 8080 --ingress external \
  --min-replicas 0 --max-replicas 1 \
  --secrets \
      db-conn="$NEON_CONN" \
      jwt-key="$JWT_KEY" \
      admin-pass="$ADMIN_PASS" \
      storage-conn="$STORAGE_CONN" \
  --env-vars \
      ASPNETCORE_ENVIRONMENT=Production \
      ForwardedHeaders__Enabled=true \
      Storage__Provider=AzureBlob \
      Seed__AdminEmail=you@ue-varna.bg \
      ConnectionStrings__Default=secretref:db-conn \
      Jwt__SigningKey=secretref:jwt-key \
      Seed__AdminPassword=secretref:admin-pass \
      Storage__ConnectionString=secretref:storage-conn

# Enforce HTTPS at the edge (in-app redirect is disabled behind the proxy by design)
az containerapp ingress update -n $APP -g $RG --allow-insecure false

# Public FQDN
az containerapp show -n $APP -g $RG --query properties.configuration.ingress.fqdn -o tsv
```

> Keep `ADMIN_PASS` — it's your first login. Capture both generated values somewhere safe; they are not stored in the repo.

---

## 5. Wire continuous deployment (OIDC)

The CD workflow (`.github/workflows/cd.yml`) runs on a published GitHub Release (or manual dispatch): it applies migrations to Neon, then rolls the Container App to the new image. It needs:

**GitHub Actions secrets** (`gh secret set <NAME>`):

| Secret | Value |
|---|---|
| `PROD_DB_CONNECTION` | the Neon Npgsql string |
| `AZURE_CLIENT_ID` | OIDC app (federated) client id |
| `AZURE_TENANT_ID` | Azure AD tenant id |
| `AZURE_SUBSCRIPTION_ID` | subscription id |

**Federated credential** so the workflow logs in without a stored password:

```bash
APP_ID=$(az ad app create --display-name "studentcouncil-cd" --query appId -o tsv)
az ad sp create --id "$APP_ID"
SUB=$(az account show --query id -o tsv)
az role assignment create --assignee "$APP_ID" --role "Contributor" \
  --scope "/subscriptions/$SUB/resourceGroups/studentcouncil-rg"

az ad app federated-credential create --id "$APP_ID" --parameters '{
  "name": "github-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:pchernaev/StudentCouncilApp:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

gh secret set AZURE_CLIENT_ID --body "$APP_ID"
gh secret set AZURE_TENANT_ID --body "$(az account show --query tenantId -o tsv)"
gh secret set AZURE_SUBSCRIPTION_ID --body "$SUB"
gh secret set PROD_DB_CONNECTION --body "$NEON_CONN"
```

> The federated `subject` above also needs to match how CD is triggered. For `workflow_dispatch`/`release` runs from `main`, add an environment- or ref-scoped credential as needed (e.g. `repo:.../StudentCouncilApp:ref:refs/heads/main`).

---

## 6. First deploy & subsequent deploys

**First image:** merge `development` → `main`. CI builds + pushes `ghcr.io/pchernaev/studentcouncilapp:{latest,<sha>}`. (Docker isn't required locally — CI builds it.) Then run §4 to create the app against that image.

**Subsequent deploys:** publish a GitHub Release from `main` → CD migrates Neon, then updates the Container App revision.

---

## 7. Smoke test

```bash
FQDN=$(az containerapp show -n studentcouncil-api -g studentcouncil-rg \
  --query properties.configuration.ingress.fqdn -o tsv)

curl -fsS "https://$FQDN/health" | jq        # expect "Healthy"
curl -fsS -X POST "https://$FQDN/api/v1/auth/login" \
  -H 'Content-Type: application/json' \
  -d '{"email":"you@ue-varna.bg","password":"<ADMIN_PASS>"}'   # expect mustChangePassword: true
```

---

## 8. Operational notes

- **Scale-to-zero** keeps compute near-free. Trade-offs while idle: a few-second cold start on the first request, and the in-process background jobs (overdue marking, reminders, cleanup) pause. That's fine today (push is deferred, jobs aren't time-critical). When the mobile apps + push land, set `--min-replicas 1` so jobs run continuously.
- **Cost watch:** the only thing that would eat the credit is always-on compute or a managed DB. With ACA scale-to-zero + Neon free + Blob, expect ~$0–3/mo.
- **Secrets** live only in the Container App / GitHub Actions — never in the image or the repo.
