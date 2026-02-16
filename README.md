# Telegram Ads Marketplace

A two-sided marketplace connecting Telegram channel owners with advertisers through a unified deal workflow.

**MVP is live at `@Ads_mpbot`**

---

## Table of Contents

- [Overview](#overview)
- [Backend Architecture](#backend-architecture)
- [Frontend](#frontend)
- [Setup & Installation](#setup--installation)

---

## Overview

**The common ground: you either want to advertise, or be a place for others to advertise**

- **Channel owners** add their Telegram channels, verify ownership, receive analytics via built-in Telegram stats, create listings with ad formats and pricing
- **Advertisers** create campaigns describing what they want to promote, then apply to channel listings (or receive applications from channel owners)

When an application is accepted, it becomes a **deal** that goes through: escrow payment (TON) → creative review → posting → verification → fund release.

---

## Backend Architecture

### Tech Stack

- **ASP.NET Core 10.0** — Vertical Slice Architecture
- **PostgreSQL** — Primary database
- **Entity Framework Core** — ORM with migrations
- **Telegram.Bot** — Bot API for notifications & commands
- **WTelegramClient** — User API for channel analytics
- **TON Blockchain** — Payment escrow
- **Carter** — Minimal API endpoints
- **Coravel** — Background job scheduling

### Architecture Pattern: Vertical Slices

Each feature is self-contained in `Features/<FeatureName>/<UseCase>/`:
```
/Features
  /Channels
    /AddChannel
      Endpoint.cs       # HTTP route
      Handler.cs        # Business logic
      Request.cs        # Input DTO
      Response.cs       # Output DTO
      Validator.cs      # FluentValidation rules
```

Cross-cutting concerns (`Auth`, `Db`, `Telegram`, `Ton`) live in `Shared/`.

### Key Components

**1. Deal State Machine**
```
Agreed → AwaitingPayment → Paid → CreativeDraft → CreativeReview → Scheduled → Posted → Verified → Released
```
Terminal states: `Cancelled`, `Refunded`, `Expired`

**2. Telegram Bot Integration**
- **Commands**: `/start`, `/help`, `/chat` (deal chat system), `/quit` (exit chat)
- **Callbacks**: Accept/reject proposals, approve/reject creatives, start conversations
- **Notifications**: Sent via transactional outbox pattern
- **Chat System**: In-bot messaging between deal parties with conversation state tracking

**3. Payment Flow (TON)**
- Advertiser pays → Escrow holds funds → Deal completes → Release to channel owner
- Fallback: Auto-refund on deal cancellation/expiry
- Withdrawals: Manual admin approval (MVP), planned auto-approval for small amounts

**4. Channel Verification**
- Bot must be channel admin (any permission level)
- User must be channel admin
- Fetches Telegram analytics: subscribers, avg views/reach, languages, premium share

**5. Background Workers**
- `OutboxDispatcherWorker` — Sends queued bot notifications (5s interval)
- `PostSchedulerWorker` — Publishes scheduled posts (30s interval)
- `PostVerifierWorker` — Verifies posts at campaign end (1min interval)
- `DealExpiryWorker` — Expires stale deals (1min interval)
- `PaymentVerificationWorker` — Polls TON payment status (30s interval)

### Database Schema

Core tables:
- `channels`, `channel_admins`, `channel_stats`
- `listings`, `listing_ad_formats`
- `campaigns`, `campaign_applications`, `listing_applications`
- `deals`, `deal_proposals`, `deal_events`
- `payments`, `escrow_balances`, `withdrawals`
- `chat_messages` — Deal partner messaging history
- `outbox_messages` — Transactional outbox for bot notifications
- ASP.NET Identity tables (`AspNetUsers`, `AspNetRoles`, etc.)

---

## Frontend

### Channels

A user adds a Telegram channel by username. The backend resolves it and pulls stats (subscribers, avg views, premium subscribers, language distribution). The channel starts unverified. To verify, the user must add the platform bot and a **pre-defined user** as an admin to their channel, then hit verify. Only verified channels can create listings or apply to campaigns.

The said user is to pull specific stats that telegram provides **(user MAY be an admin with absolutely no permissions, no permission are required)**

### Campaigns

An advertiser creates a campaign with a title, brief, budget, minimum subscriber/view requirements, target languages, and a schedule window. upon making the campaign, it's set to DRAFT, user can choose to publish, pause and unpause afterwards

### Listings

A channel owner creates a listing under a verified channel. A listing defines ad formats (Post, Story, Repost (Story and Repost are planned for future developments)) with per-format pricing in TON, duration in hours, and terms. Listings have statuses: Draft, Published, Paused, Archived. The owner can publish, pause, and unpause from the listing detail page.

### The connection

- A **campaign** is what an advertiser wants.
- A **listing** is what a channel owner offers.
- Applications bridge the two. An advertiser can apply to a listing (picking one of their campaigns), or a channel owner can apply to a campaign (picking one of their verified channels).

### Applications and Deals

Applications carry a proposed price and an optional message. The receiving party can accept or reject. Accepted applications create deals.

## Deal lifecycle

Deals move through these statuses in order:

```
Agreed → AwaitingPayment → Paid → CreativeDraft → CreativeReview → Scheduled → Posted → Verified → Released
```

Terminal statuses that end the deal early: **Cancelled**, **Refunded**, **Expired**.

said statuses are pretty self-explanatory, for further clarification

**CreativeDraft → CreativeReview** happens inside the chat bot itself, it's the procedure of coming up with (for e.g.) a post that both parties agree to (Creative Loop)

**Released** after the deal has reached the point of verification, the funds will be released to the owner

## Payment

Payments go through TON. The advertiser creates a payment via the backend, gets a payment URL, and pays. The frontend polls the payment status until it resolves. Payment sessions are persisted in sessionStorage so they survive page navigations.

## Wallet and withdrawals

Profile page shows TON wallet connection via TON Connect. Balance is fetched from the backend. Users can withdraw to their connected wallet (minimum 0.1 TON).

---

# **ATTENTION**

# The MVP is already up and running at (@Ads_mpbot), the following instrucitons are for running the project locally

---

## To run :

```sh
bun install
```

(or any other package manager of your choice)

Create a `.env` file:

```
VITE_BACKEND_BASE_URL=https://your-backend-url.com
VITE_BOT_USERNAME=your_bot_username
```

`VITE_BACKEND_BASE_URL` -- base URL for all API requests
`VITE_BOT_USERNAME` -- the Telegram bot username that needs to be added as a channel admin for verification

## Run

```sh
bun run dev
```

The app requires the Telegram Mini App environment. Outside of Telegram it shows an error page.

## Build

```sh
bun run build
```

## Docker

```sh
docker build -t adsbot .
docker run -p 80:80 adsbot
```
---

## Setup & Installation

### Prerequisites

- **.NET 10 SDK**
- **PostgreSQL 17+**
- **Docker & Docker Compose** (optional but recommended)
- **Telegram Bot** (via BotFather)
- **Telegram API credentials** (api_id, api_hash from my.telegram.org)
- **TON wallet** for testing payments

### Backend Setup

#### 1. Clone & Navigate

```bash
cd TelegramAds
```

#### 2. Configure `appsettings.json`

Edit `TelegramAds/appsettings.json` with your credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=telegramads;Username=telegramads;Password=telegramads_secret"
  },
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN",
    "ApiId": YOUR_API_ID,
    "ApiHash": "YOUR_API_HASH",
    "PhoneNumber": "+YOUR_PHONE_NUMBER",
    "SessionPath": "/opt/telegramads/WTelegram.session"
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyAtLeast32CharactersLong!",
    "Issuer": "TelegramAds",
    "Audience": "TelegramAds"
  },
  "Ton": {
    "Network": "testnet",
    "WalletMnemonic": "YOUR_24_WORD_MNEMONIC",
    "DepositWalletAddress": "YOUR_TON_WALLET_ADDRESS"
  },
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN",
    "BotWebhookUrl": "https://yourdomain.com/bot",
    "SecretToken": "YOUR_WEBHOOK_SECRET"
  }
}
```

**Required:**
- `Telegram.BotToken` — from [@BotFather](https://t.me/BotFather)
- `Telegram.ApiId` & `ApiHash` — from [my.telegram.org](https://my.telegram.org)
- `Telegram.PhoneNumber` — for WTelegram session
- `Ton.WalletMnemonic` — 24-word seed phrase
- `BotConfiguration.BotWebhookUrl` — full URL for bot webhook

#### 3. Run with Docker Compose (Recommended)

```bash
docker-compose up -d
```

**Services:**
- **PostgreSQL** → `:5432`
- **pgAdmin** → http://localhost:5050
- **API** → http://localhost:5000

Create `.env` in project root:
```env
TELEGRAM_BOT_TOKEN=your_bot_token
TON_API_KEY=your_ton_api_key
```

#### 4. Run Manually

**Start PostgreSQL:**
```bash
docker-compose up -d postgres
```

**Run migrations:**
```bash
cd TelegramAds
dotnet ef database update
```

**Run API:**
```bash
dotnet run
```

API: http://localhost:5000 | Swagger: http://localhost:5000/swagger

---

## Production Deployment

1. Set `ASPNETCORE_ENVIRONMENT=Production`
2. Use strong DB password & JWT secret
3. Configure webhook URL in `appsettings.json`
4. Enable HTTPS with reverse proxy (nginx/caddy)
5. Set proper CORS origins

---

## License

MIT
