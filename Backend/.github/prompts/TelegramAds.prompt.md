---
agent: agent
---
# Telegram Ads Marketplace (Mini App + Bot) — MVP

## Overview
This project is an MVP Telegram Mini App (WebApp) + Telegram Bot that powers a two-sided ads marketplace connecting:
- **Channel Owners (Influencers)** who list their channels and sell ad placements
- **Advertisers** who create campaign briefs and buy placements

The MVP is built around a unified deal workflow:
**Discover → Negotiate → Agree → Pay (TON escrow) → Creative approval → Auto-post → Verify → Release/Refund**

> **Messaging and approvals happen via a Telegram text bot** (no in-mini-app chat).
> The **Mini App** is used for browsing, forms, and managing entities (channels/campaigns/deals).

---

## Core Requirements (MVP)

### 1) Marketplace (two-sided)
- Channel owners can register a channel, set pricing, and add the bot as an admin.
- Advertisers can create campaign briefs; channel owners can apply.
- Both entry points converge to a single Deal workflow with negotiation and approvals.
- Practical filters: price, subscribers, average views, language, etc.
- PR manager flow: allow multiple users to manage a channel; verify admin rights at sensitive operations.

### 2) Verified Channel Stats
Fetch and show Telegram-provided analytics (minimum):
- Subscribers
- Average views/reach
- Language charts
- Premium share
- Other Telegram analytics fields available to bots/admins

### 3) Ad formats + pricing
- Multiple formats per channel (free-form).
- MVP can support only **"Post"** format, but data model must allow more.

### 4) Escrow deal flow on TON
- Advertiser pays → funds held by system → auto-post confirms delivery → release or refund.
- Recommended: a unique wallet/address per deal (or per user), plus a hot wallet.
- Lifecycle controls: auto-cancel / timeouts on inactivity, strict deal state transitions.

### 5) Creative approval workflow
- Advertiser posts brief → Owner accepts/rejects
- Owner submits draft creative → Advertiser approves or requests edits
- When approved → auto-publish at agreed time

### 6) Auto-posting + monitoring
- Bot auto-posts the approved creative to the channel
- Verify message exists and remains unedited/undeleted for a configured time
- Only then release funds

---

## Application & Deal Model

The system uses a two-phase model: **Applications** and **Deals**.

### Applications (Negotiation Phase)
Applications represent interest between parties before a confirmed agreement. There are two types:

**1. Campaign Application (Channel → Campaign):**
- Channel owner browses available campaigns
- Applies their channel to a campaign with a proposed price
- Campaign owner (advertiser) reviews and accepts/rejects

**2. Listing Application (Campaign → Listing):**
- Advertiser browses available listings (channels)
- Applies their campaign to a listing with a proposed price
- Listing owner (channel owner) reviews and accepts/rejects

Application statuses: `Pending`, `Accepted`, `Rejected`, `Withdrawn`

### Deals (Confirmed Partnership)
When an application is **accepted**, it automatically transitions into a Deal. A Deal represents:
- A confirmed agreement between advertiser and channel owner
- Linked to both a Campaign and a Listing
- Tracked separately from applications
- Starts at `Agreed` status (no negotiation phase in deals)

### Four Application Routes
1. **You apply your campaign to their listing** → ListingApplication
2. **They apply their channel to your campaign** → CampaignApplication
3. **They apply their campaign to your listing** → ListingApplication (you review)
4. **You apply your channel to their campaign** → CampaignApplication (they review)

---

## Product Flows

### A) Channel Owner — Register & List Channel
1. Owner opens Mini App → **Add channel**
2. Owner provides channel **@username** or forwards a message
3. System asks owner to add our bot as **Channel Admin** with required permissions
4. Backend verifies:
   - Bot is admin
   - User is admin (or a permitted channel manager)
5. Backend fetches channel stats (initial snapshot)
6. Owner sets pricing and publishes listing

Key outcomes:
- Channel becomes **Verified** only after bot admin verification succeeds.
- Any sensitive operation re-checks that the acting user is still an admin in Telegram.

---

### B) Advertiser — Create Campaign Brief
1. Advertiser opens Mini App → **Create campaign**
2. Provides: title, brief, budget, target criteria (subs/views/language), desired schedule window
3. Campaign becomes visible to channel owners to apply

---

### C) Match → Application to Deal Workflow

**Path 1 (Channel applies to Campaign):**
- Channel owner browses campaigns → applies to a campaign (CampaignApplication)
- Advertiser reviews application → accepts → Deal created in `Agreed` status

**Path 2 (Advertiser applies to Listing):**
- Advertiser browses listings → applies with their campaign (ListingApplication)
- Channel owner reviews application → accepts → Deal created in `Agreed` status

Both paths create a Deal linked to a Campaign and a Listing.

---

### D) Deal Lifecycle (Post-Application)
Once a deal is created (application accepted):
1. Deal starts in **Agreed** status
2. Can optionally exchange proposals to adjust terms (price, schedule)
3. Move to **AwaitingPayment** when ready
4. Continue through escrow and creative workflow

> Messaging for deal updates happens via Telegram Bot messages + inline keyboard actions.
> Mini App shows the deal details and history.

---

### E) Escrow + Creative + Auto-post (high level)
1. Advertiser pays TON to unique deal address
2. Payment confirmed → deal moves to **Paid**
3. Owner drafts creative → advertiser reviews and approves
4. On scheduled time → bot publishes in channel
5. Monitoring validates message integrity
6. Release funds to channel owner OR refund on failure

---

## Deal State Machine (MVP)
We enforce strict transitions. Deals are only created when applications are accepted.

Deal states (deals start at `AGREED` after application acceptance):
- `AGREED` (initial state - application was accepted)
- `AWAITING_PAYMENT`
- `PAID`
- `CREATIVE_DRAFT`
- `CREATIVE_REVIEW`
- `SCHEDULED`
- `POSTED`
- `VERIFIED`
- `RELEASED`
- `REFUNDED`
- `CANCELLED`
- `EXPIRED`

Application states (separate from deals):
- `PENDING` (awaiting review)
- `ACCEPTED` (deal created)
- `REJECTED`
- `WITHDRAWN`

Rules:
- Deals can move to `CANCELLED` only by allowed actors and only before `POSTED`.
- Auto-expire deals with no activity for X hours.
- Every transition creates an immutable `DealEvent` record.

---

## Architecture

### High-level Components
- **Telegram Mini App (WebApp)**: UI for lists/forms
- **Telegram Bot**: messaging + approvals + actions
- **Backend API**: business logic + persistence
- **TON Wallet Service**: escrow addresses + ledger + payouts
- **Workers**: stats refresh, outbox delivery, scheduled posting, verification

### Authentication & Identity (ASP.NET Identity)
We use **ASP.NET Identity** as the canonical identity system.
- Users are stored in Identity tables (GUID primary keys).
- We create or link an `ApplicationUser` on first Telegram login (WebApp `initData`).
- `ApplicationUser` includes Telegram identifiers (e.g., `TgUserId`, `Username`) and any app-specific profile fields.
- Authorization uses Identity roles/claims (e.g., Advertiser / ChannelOwner / ChannelManager) plus channel-scoped permissions stored in domain tables.
- All sensitive operations (pricing changes, deal acceptance, escrow actions) must re-verify Telegram admin rights when the user acts on behalf of a channel.

---

## Vertical Slice / Feature-based Structure (Rules)
This project follows **Vertical Slice Architecture**:
- No "fat" generic layers like `/Controllers`, `/Services`, `/Repositories` at root
- Each feature owns its API, domain models, handlers, validators, and persistence
- Cross-cutting concerns live in `Shared/` (logging, auth, db, telegram clients)
- Each command/query is one slice: request → handler → db → response

Rules:
1. **Feature folder is the unit of modularity**
2. **One slice per use-case**
3. **Shared is only for true cross-cutting**
4. **No domain leakage across features** (prefer referencing IDs over importing feature internals)
5. **Transactional boundaries are per handler**
6. **All state transitions are validated in the handler**
7. **Idempotency for external calls** (TON, Telegram)
8. **Every external side-effect uses Outbox** to guarantee delivery

---

## Suggested Project Structure
```
/src
  /AppHost                # ASP.NET Core host (composition root)
    Program.cs
    appsettings.json

  /Shared
    /Auth                 # Telegram initData validation + ASP.NET Identity integration (AuthN/AuthZ)
    /Db                   # DbContext, migrations, interceptors
    /Telegram             # Telegram Bot API client wrapper, retry, rate limit
    /Ton                  # TON RPC client wrapper, wallet ops
    /Outbox               # Outbox dispatcher infrastructure
    /Time                 # Clock abstraction
    /Validation           # FluentValidation, shared primitives
    /Errors               # ProblemDetails, error codes
    /Observability        # Logging, tracing, metrics

  /Features

    /Identity
      /Login
        Endpoint.cs
        Request.cs
        Response.cs
        Handler.cs
        Validator.cs

    /Channels
      /AddChannel
      /VerifyBotAdmin
      /SyncChannelAdmins
      /GetChannel
      /SearchChannels
      /UpdateChannelProfile

    /Listings
      /CreateListing
      /PublishListing
      /UpdatePricing
      /SearchListings
      /ApplyToListing           # Advertiser applies campaign to listing
      /ListListingApplications  # List applications to listings

    /Campaigns
      /CreateCampaign
      /UpdateCampaign
      /SearchCampaigns
      /ApplyToCampaign          # Channel owner applies to campaign
      /ListCampaignApplications # List applications to campaigns

    /Applications
      /ReviewApplication        # Accept/Reject any application (creates deal on accept)

    /Deals
      /GetDeal
      /ListDeals
      /SendProposal             # Propose changes to agreed terms
      /AcceptProposal           # Accept proposed changes
      /CancelDeal
      /ExpireStaleDeals

    /Stats
      /FetchChannelStats
      /GetChannelStats

    /Bot
      /HandleUpdate           # Webhook update handler
      /Templates             # message templates
      /Actions               # inline keyboard actions mapping to feature commands

    /Payments
      /CreateEscrowAddress
      /ConfirmPayment
      /ReleaseFunds
      /RefundFunds

    /Posting
      /SchedulePost
      /PublishPost
      /VerifyPostIntegrity
      /FinalizeDelivery

  /Workers
    /OutboxDispatcher
    /StatsRefresher
    /PostScheduler
    /PostVerifier
    /DealExpiry

/tests
  /FeatureTests
  /IntegrationTests
```

---

## Slice Template (per use-case)
Each folder under `/Features/<FeatureName>/<UseCase>/` contains:
- `Endpoint.cs` — Minimal API route mapping
- `Request.cs` / `Response.cs` — DTOs
- `Handler.cs` — business logic
- `Validator.cs` — FluentValidation rules
- `Models.cs` — feature-local types (optional)
- `Data.cs` — EF projections/query helpers (optional)

---

## Service Boundaries (Logical Modules)
Even if deployed as a modular monolith in MVP, we keep clear boundaries:
1. Identity/Auth
2. Channels + Admin verification
3. Listings + Pricing
4. Campaigns + Applications
5. Deals + Negotiation
6. Stats
7. Bot messaging & actions
8. Payments (TON escrow)
9. Posting + verification
10. Workers

Later, each can be split into microservices without rewriting the domain.

---

## Persistence Model (high level tables)
Main tables (domain + identity):
- ASP.NET Identity tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`)
- channels, channel_admins, channel_stats
- listings, listing_ad_formats
- campaigns, campaign_applications, listing_applications
- deals, deal_proposals, deal_events
- bot_outbox
- **payments** (TON Console invoice tracking)
- **escrow_balances** (user earnings & available funds)
- **withdrawals** (channel owner withdrawal requests)
- **payment_webhooks** (TON Console webhook logs)
- **deal_escrow_transactions** (payment flow audit trail)

**See:** [Payment System Implementation Plan](../../docs/PAYMENT_SYSTEM_IMPLEMENTATION_PLAN.md) for complete payment architecture.

---

## Payment System Integration

The system uses **TON Console Invoice Service** for blockchain payment tracking.

### Two Payment Flows

**1. Deal Payment (Advertiser → Escrow)**
- Advertiser initiates payment for a deal in `Agreed` status
- System creates TON Console invoice
- User pays via Tonkeeper wallet (QR code / deep link)
- Webhook notifies system when payment confirmed
- Funds held in escrow until deal verified
- Deal status: `Agreed` → `AwaitingPayment` → `Paid` → ... → `Verified` → `Released`

**2. Earnings Withdrawal (Escrow → Channel Owner)**
- Channel owner accumulates earnings from completed deals
- Balance types: Total Earned, Locked (active deals), Available (completed deals)
- Owner requests withdrawal to their TON wallet
- Admin reviews and approves (initially manual, auto for small amounts later)
- System executes TON transfer to owner's wallet
- Blockchain confirmation tracked

### Key Features

**Payments:**
- `POST /api/payments/create` - Create invoice for deal payment
- `GET /api/payments/{id}` - Check payment status
- `POST /api/webhooks/ton` - TON Console webhook handler (public)

**Balance & Withdrawals:**
- `GET /api/payments/balance` - View earnings & available balance
- `POST /api/payments/withdraw` - Request withdrawal
- `GET /api/payments/withdrawals` - List withdrawal history
- `POST /api/admin/withdrawals/{id}/process` - Approve/reject withdrawal (admin)

**Integration Details:**
- **Currency:** TON (USDT support planned)
- **Amount Units:** Stored as decimal (TON), API uses nano-tons (1 TON = 1e9 nano-tons)
- **Invoice Lifetime:** 30 minutes (configurable)
- **Withdrawal Fee:** 0.5 TON (configurable)
- **Minimum Withdrawal:** 1 TON

**See:** Complete implementation plan in [docs/PAYMENT_SYSTEM_IMPLEMENTATION_PLAN.md](../../docs/PAYMENT_SYSTEM_IMPLEMENTATION_PLAN.md)

---

## Known Limitations (MVP)
- Only "Post" ad format is required (model supports more).
- Initial stats are stored as snapshot for filtering; full analytics may be extended.
- Escrow uses TON testnet for demo.
- Advanced fraud detection and disputes are out of scope for MVP.

---

## Development Guidelines

### Error Handling
- Never return exceptions directly
- Use ErrorOr pattern throughout the project
- All handlers should return ErrorOr<TResponse>

### Enum Serialization
- When returning enums in API responses, convert them to strings
- Never return or accept enum types directly in DTOs
- Use `.ToString()` when mapping enums to response objects

### Swagger Schema Validation
**CRITICAL:** After creating or modifying any API endpoint or response DTO:
1. Build the project (`dotnet build`)
2. Run the application and verify Swagger endpoint works: `GET /swagger/v1/swagger.json`
3. Check for schema ID conflicts in Swagger generation

**Common Issue:** Duplicate DTO names across features cause Swagger schema conflicts.

**Solution:** Use feature-specific DTO naming:
- ✅ `GetDealCampaignDto`, `ListDealCampaignDto` (feature-prefixed)
- ❌ `DealCampaignDto` (generic, causes conflicts)

**Naming Pattern:**
- `{FeatureName}{Purpose}Dto` - e.g., `ListDealCampaignDto`, `GetDealListingDto`
- Ensures uniqueness across all features
- Makes DTO source immediately clear 