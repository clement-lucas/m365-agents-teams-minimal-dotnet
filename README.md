# m365-agents-teams-minimal-dotnet (Cosmos + SSO)

End-to-end .NET 8 (Azure Functions Isolated) sample that:

- Sends **scheduled proactive messages** to Teams using a **TimerTrigger**
- Persists **ConversationReference** in **Azure Cosmos DB** (for durable proactive messaging)
- Implements **Teams SSO (OAuthPrompt + Token Exchange)** to call **Microsoft Graph** (`/me`) when user types `me`
- Includes **Teams manifest** (with `webApplicationInfo`) and **infra/Bicep** to provision Azure resources (Function App, Storage, App Insights, Cosmos DB). 

## Repository Structure

```
.
├─ README.md
├─ src/
│  └─ FunctionApp/
│     ├─ Program.cs
│     ├─ appsettings.json.example
│     ├─ Functions/
│     │  ├─ BotControllerFunction.cs
│     │  └─ TimerNotifyFunction.cs
│     ├─ Bot/
│     │  ├─ AdapterWithErrorHandler.cs
│     │  ├─ TeamsBot.cs
│     │  ├─ DialogBot.cs
│     │  ├─ Dialogs/
│     │  │  └─ MainDialog.cs
│     │  └─ State/
│     │     ├─ BotStateAccessors.cs
│     │     └─ StorageKeys.cs
│     ├─ Services/
│     │  ├─ ConversationStoreCosmos.cs
│     │  ├─ GraphClientFactory.cs
│     │  └─ TokenHelpers.cs
│     ├─ Models/
│     │  └─ ConversationReferenceDocument.cs
│     └─ m365-agents-teams-minimal.csproj
├─ teams-app/
│  └─ manifest.json
└─ infra/
   └─ main.bicep
```

## Prerequisites

- .NET SDK 8.x
- Azure CLI (`az`) / PowerShell
- Azure subscription permissions to deploy Function App, Cosmos DB, App Insights
- **Microsoft Entra ID** admin to:
  - Register an **App** (Client ID/secret)
  - Configure **API permissions** (`User.Read`, plus any others you need)
  - **Expose an API** & set **Application ID URI** (e.g., `api://<domain-or-guid>/<client-id>`) for Teams SSO
  - Create/Configure **Bot Channels Registration** (set Messaging endpoint later)
- Teams Developer Portal (for sideloading the manifest)

## Setup – High Level

1. **Create Entra App** (for SSO & Graph delegated calls)
   - Add **API permissions**: delegated `User.Read` (grant admin consent)
   - **Expose an API** → set **Application ID URI** (capture it)
2. **Create OAuth Connection** in your **Bot Channels Registration**
   - Connection name: `TeamsSSO` (use this name in code)
   - Service Provider: **Azure AD v2**
   - Client ID/Secret: from Entra app
   - Tenant ID/Issuer: your tenant
   - Scopes: `User.Read offline_access openid profile` (plus others as needed)
3. **Deploy Azure infra** (Function App, Storage, App Insights, Cosmos) via `infra/main.bicep`
4. **Configure App Settings** on Function App
   - `Bot:MicrosoftAppId`, `Bot:MicrosoftAppPassword` (from Bot Channels Registration)
   - `Bot:TenantId`
   - `OAuth:ConnectionName` = `TeamsSSO`
   - Cosmos settings, etc. (see `appsettings.json.example`)
5. **Set Bot Messaging endpoint** to `https://<FUNCTION_HOST>/api/messages`
6. **Sideload Teams app** with `teams-app/manifest.json` (ensure `webApplicationInfo` matches Entra App)
7. **Test**
   - Chat the bot: send `hi` to store conversation reference; wait for Timer to push proactive message
   - Send `me` to trigger SSO → Graph `/me`

## Local Run

```bash
cd src/FunctionApp
cp appsettings.json.example appsettings.json
# fill settings
func start  # or: dotnet build && func start
```

## Deploy

```bash
# provision infra
az deployment group create   --resource-group <rg>   --template-file infra/main.bicep   --parameters namePrefix=<prefix> location=<region>

# publish function app (from src/FunctionApp)
dotnet publish -c Release
func azure functionapp publish <functionAppName> --dotnet-isolated

# set application settings (examples)
az functionapp config appsettings set -g <rg> -n <functionAppName>   --settings   "Bot__MicrosoftAppId=<botAppId>"   "Bot__MicrosoftAppPassword=<botPassword>"   "Bot__TenantId=<tenantId>"   "OAuth__ConnectionName=TeamsSSO"   "Cosmos__Endpoint=<cosmosEndpoint>"   "Cosmos__Key=<cosmosKey>"   "Cosmos__Database=botdb"   "Cosmos__Container=conversationRefs"   "Timer__Cron=0 */15 * * * *"
```

## What to Expect

- Send any message → conversation reference is saved in Cosmos
- Timer fires (default every 15 minutes) → proactive message delivered
- Send `me` → OAuthPrompt triggers SSO if needed → calls Graph `/me` and replies with DisplayName/UPN

## Hardening Ideas

- Replace `MemoryStorage` with Cosmos-based **Bot State**
- Add **retry** around Cosmos/Graph calls
- Use **Managed Identity** for Cosmos
- Multi-tenancy partitioning: use `tenantId` as `partitionKey`
- Add **/healthz** http-trigger for liveness
- Add **Queues** for high-volume proactive sends

## License
MIT
