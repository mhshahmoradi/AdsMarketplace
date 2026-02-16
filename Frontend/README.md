# AdsMarketplace Frontend

Frontend for the Ads Marketplace (Telegram Mini App)

**Mostly written by the help of AI, real magic lies beyond (the backend)**

**Backend** : https://github.com/backend (CHANGE THIS)

## ATTENTION

## the MVP is already up and running at **@ads_ekhsh_bot**

## How this even works

**The common ground : you either want to advertise, or be a place for other to advertise**

**you got 2 ways**, but unified

- **Channel owners** add their Telegram channels, verify ownership, recieve their analytics via the built-in telegram analytics, create listings with ad formats and pricing

- **Advertisers** create campaigns describing what they want to promote, then apply to channel listings (or receive applications from channel owners who want to run their campaign).

When an application is accepted by either party, it becomes a **deal**.
Those deals now go through a status lifecycle that includes escrow payment via TON, creative submission and review, posting confirmation, and fund release. **(ALL ALREADY IMPLEMENTED)**

## Core business logic

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

# The MVP is already up and running at (@ads_ekhsh_bot), the following instrucitons are for running the project locally

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
