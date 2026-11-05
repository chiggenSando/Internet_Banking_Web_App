# MCBA Internet Banking

A web-based internet banking system for the fictional Most Common Bank of Australia (MCBA), built with ASP.NET Core MVC, a Web API, and Entity Framework Core on SQL Server.

Customers log in to view their accounts, move money, browse statements, schedule bill payments and manage their profile. Bank staff use a separate admin portal to manage payees and lock or unlock customer logins.

This was a group project for **Web Development Technologies**. See [About the project](#about-the-project--group-assignment).

## Contents

- [About the project / group assignment](#about-the-project--group-assignment)
- [How the app works](#how-the-app-works)
- [Business rules](#business-rules)
- [Architecture](#architecture)
- [Getting started](#getting-started)
- [Running the tests](#running-the-tests)
- [Admin API reference](#admin-api-reference)

## About the project / group assignment

This was a two-person group assignment, completed in Semester 2, 2026.

Team Member 
- Benjamin Norrie
- Seth Tootell 

**How we worked**

- **Shared repo, feature branches, pull requests.** Each feature (ATM transactions, statements, bill pay, profile, admin API and so on) was built on its own branch and merged through a pull request into `main`.
- **Trello for planning.** We tracked tasks on a Trello board. Screenshots of the board over time are in [Trello/](Trello/).
- **Tests alongside features.** Each of the three applications has its own unit test project (see [Running the tests](#running-the-tests)).

## How the app works

There are three runnable applications that share one database through a common class library.

### Customer website (`Mcba.CustomerWebsite`)

An MVC site for MCBA's customers. Everything except the login page requires a logged-in customer.

**Login.** A customer signs in with their 8-digit Login ID and password. Passwords are never stored in plain text: only a PBKDF2 hash (via SimpleHashing.Net) is kept, and the entered password is verified against it. A locked login is rejected with a message telling the customer to contact the bank. After a successful login the customer ID and name are kept in server-side session state, and an `[AuthorizeCustomer]` filter redirects anyone without a session back to the login page.

**Accounts.** The home page lists the customer's accounts (savings or checking) with their balances. Each account number links to its statement.

**Deposit, withdraw and transfer.** Money movements use a two-step flow:

1. The customer fills in the form (account, amount, optional comment, and a destination account for transfers). The server validates it: the account must belong to them, the amount can have at most 2 decimal places, a transfer needs a valid destination other than the source, and the balance must cover the amount plus any service charge.
2. The transaction is held in the session and shown on a confirmation page. Confirming applies it. Cancelling discards it.

On confirmation, the balance changes, a transaction record is written, and for a transfer a matching incoming record is written on the destination account. If a service charge applies, it is recorded as its own transaction. All of this is saved in a single database save, so it either all happens or none of it does.

**Statements.** Pick an account to see its balance, available balance, and its transactions newest first, four per page.

**BillPay.** Customers schedule payments to a payee, either one-off or annually. They can see all scheduled bills, cancel one, or retry one that has failed (see [Scheduled bill payments](#scheduled-bill-payments)).

**My profile.** Customers can view and edit their name, TFN, address, state, postcode and mobile number, and change their password. Changing the password requires the current password, and only the new hash is stored.

### Scheduled bill payments

BillPay runs in the background through a hosted service, `BillPayBackgroundService`.

- It runs once at startup and then every 10 seconds.
- Each run finds bills that are due and not failed, and pays them in order of scheduled time.
- A successful payment debits the account and records a `B` (BillPay) transaction. A one-off bill is then removed. An annual bill is rescheduled one year ahead.
- If the account lacks the funds, the bill is marked as **failed** instead. It stays in the customer's list so they can retry or cancel it.
- The schedule is stored in the database, so bills that fell due while the site was stopped are processed on the next startup.

The payment logic lives in `BillPayProcessor`, separate from the timer, so it can be unit tested with any "current time".

### Admin portal (`Mcba.AdminPortal`) and Admin API (`Mcba.AdminApi`)

The admin side is split in two:

- **Admin API** is a REST Web API that owns the database access for admin features. It uses the repository pattern, so its controllers depend on interfaces rather than on the database context.
- **Admin portal** is an MVC site for bank staff. It never touches the database. Every action is an HTTP call to the Admin API.

Staff log in with a fixed admin account (username `admin`, password `admin`). This is hard-coded rather than stored in the database, which the assignment specification allows. An `[AuthorizeAdmin]` filter protects every other page.

From the portal, an admin can:

- **List payees**, optionally filtered by Australian state, and **create** new payees. These are the payees customers choose from when scheduling a bill.
- **List customer logins** and **lock or unlock** them. A locked customer is blocked at the customer website's login.

### Shared data library (`Mcba.Data`)

Holds the Entity Framework Core `McbaContext`, the entity models (`Customer`, `Login`, `Account`, `Transaction`, `Payee`, `BillPay`) and the EF migrations. All three applications reference it.

Business rules are enforced twice: in the controllers, to give friendly validation messages, and as database check constraints, as a safety net. The constraints cover account types, minimum balances, positive amounts and similar rules.

### First-run seeding

On first startup, if the database has no customers, the customer website downloads customer, login and account data from a university-hosted web service (`WebService:CustomerUrl` in `appsettings.json`). Each account's opening balance is the sum of its imported deposit transactions. The seed step is skipped once customers exist.

## Business rules

| Rule | Detail |
| --- | --- |
| Account types | Savings (`S`) and Checking (`C`) |
| Minimum balance | Savings cannot go below $0. Checking cannot go below -$100. |
| Free transactions | Each account gets 2 free outgoing transactions (withdrawals and outgoing transfers). |
| Withdrawal fee | $0.05 after the free transactions are used |
| Transfer fee | $0.10 after the free transactions are used |
| Free of charge | Deposits, incoming transfers and BillPay payments never cost a fee or use up a free transaction |
| Amounts | Must be positive, with at most 2 decimal places |
| Transaction types | `D` Deposit, `W` Withdraw, `T` Transfer, `S` Service charge, `B` BillPay |
| BillPay periods | `O` One-off, `A` Annual |

## Architecture

```
┌──────────────────────┐   ┌──────────────────────┐
│  Mcba.CustomerWebsite │   │   Mcba.AdminPortal   │
│  MVC, direct DB access│   │  MVC, no DB access   │
└──────────┬───────────┘   └──────────┬───────────┘
           │                          │ HTTP
           │                ┌─────────▼───────────┐
           │                │    Mcba.AdminApi    │
           │                │  Web API + repos    │
           │                └─────────┬───────────┘
           │                          │
           └──────────┬───────────────┘
                      ▼
              ┌───────────────┐
              │   Mcba.Data   │  EF Core context, models, migrations
              └───────┬───────┘
                      ▼
                 SQL Server
```

| Project | Type | Purpose |
| --- | --- | --- |
| `Mcba.CustomerWebsite` | ASP.NET Core MVC | Customer-facing banking site, including the bill pay background service |
| `Mcba.AdminPortal` | ASP.NET Core MVC | Staff portal, talks to the Admin API only |
| `Mcba.AdminApi` | ASP.NET Core Web API | REST endpoints for payees and logins, repository pattern, OpenAPI docs in Development |
| `Mcba.Data` | Class library | EF Core context, entity models, migrations, shared DTOs |
| `Mcba.*.Tests` | xUnit v3 | Unit tests for each application |

**Tech stack:** .NET 10, ASP.NET Core MVC, Entity Framework Core 10 (SQL Server, lazy-loading proxies on the customer site), SimpleHashing.Net, X.PagedList, Newtonsoft.Json, xUnit v3.

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A SQL Server database you can connect to
- The EF Core CLI tool: `dotnet tool install --global dotnet-ef`

### 1. Configure the database

The connection string (`ConnectionStrings:McbaContext`) is intentionally blank in `Mcba.CustomerWebsite/appsettings.json` and `Mcba.AdminApi/appsettings.json`, so no credentials are stored in the repository. Both apps must point at the **same** SQL Server database.

Supply it with [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), run once per project:

```bash
dotnet user-secrets init --project Mcba.CustomerWebsite
dotnet user-secrets set "ConnectionStrings:McbaContext" "<your connection string>" --project Mcba.CustomerWebsite

dotnet user-secrets init --project Mcba.AdminApi
dotnet user-secrets set "ConnectionStrings:McbaContext" "<your connection string>" --project Mcba.AdminApi
```

`user-secrets init` adds a `UserSecretsId` to the project file, so commit that change if you want it shared.

Or use an environment variable instead: `ConnectionStrings__McbaContext`.

The `dotnet ef` command in the next step also reads the connection string from `Mcba.CustomerWebsite`, so set its secret first.

### 2. Create the schema

The apps do not apply migrations themselves, so create the schema once:

```bash
dotnet ef database update --project Mcba.Data --startup-project Mcba.CustomerWebsite
```

### 3. Run the applications

Run each in its own terminal from the repository root. Start the Admin API **before** the Admin Portal.

```bash
# Customer website: http://localhost:5003 (https://localhost:7143)
dotnet run --project Mcba.CustomerWebsite

# Admin API: http://localhost:5040 (https://localhost:7023)
dotnet run --project Mcba.AdminApi

# Admin portal: http://localhost:5136 (https://localhost:7009)
dotnet run --project Mcba.AdminPortal
```

The first time the customer website starts, it seeds the database from the web service (see [First-run seeding](#first-run-seeding)). This needs network access.

The admin portal finds the API through `AdminApi:BaseUrl` in `Mcba.AdminPortal/appsettings.json` (default `http://localhost:5040`).

### Logging in

- **Customer website:** use a Login ID and password from the seeded customer data.
- **Admin portal:** username `admin`, password `admin`.

## Running the tests

Run all test projects from the repository root:

```bash
dotnet test
```

Or run a single project:

```bash
dotnet test --project Mcba.CustomerWebsite.Tests
dotnet test --project Mcba.AdminApi.Tests
dotnet test --project Mcba.AdminPortal.Tests
```

| Test project | What it covers |
| --- | --- |
| `Mcba.CustomerWebsite.Tests` | Login, profile, customer transaction and bill pay controllers, and the `BillPayProcessor` service |
| `Mcba.AdminApi.Tests` | Payee and login controllers and repositories, and payee model validation |
| `Mcba.AdminPortal.Tests` | Admin controllers (using a fake Admin API), the `[AuthorizeAdmin]` filter, and view model validation |

## Admin API reference

Base URL: `http://localhost:5040`. When running in Development, the OpenAPI document is served at `/openapi/v1.json`.

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/payees` | List payees ordered by name. Optional `?state=VIC` filter (case-insensitive). |
| `GET` | `/api/payees/{id}` | Get one payee. Returns `404` if not found. |
| `POST` | `/api/payees` | Create a payee. Returns `201` with a `Location` header, or `400` if validation fails. |
| `GET` | `/api/logins` | List all logins ordered by login ID. Password hashes are not returned. |
| `PUT` | `/api/logins/{id}/lock` | Lock a login. Returns `204`, or `404` if not found. |
| `PUT` | `/api/logins/{id}/unlock` | Unlock a login. Returns `204`, or `404` if not found. |
