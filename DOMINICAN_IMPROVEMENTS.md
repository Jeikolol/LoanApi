# Dominican Loan System - API & Entities Improvements

## Current State Assessment (updated 2026-09-21)
✅ **Working:**
- Customer, Loan, Employee, LoanPayment, LoanDocument, DelinquencyPolicy, CreditScore entities
- Clean Architecture pattern, MediatR pipeline, Dapper handlers
- Authentication/Authorization (JWT via HttpOnly cookie through the Next.js proxy, roles, forced password change, rate limiting)
- Audit trail for every command (`AuditBehavior`) with a screen to browse it
- Identification rules (`IdentificationRules`): cédula `999-9999999-9`, RNC `999-99999-9`, passport `AA` + 6–12 digits (AA = nationality ISO code); legal person → RNC only, individual → cédula or passport, cédula ⇒ Dominican nationality; canonical normalization prevents duplicates
- Delinquency tracking (`DelinquencyRules`, recalculation endpoint, "Mora" screen)
- Amortization (annuity schedule, `AmortizationSchedule`)
- Payment history: register and list payments per loan (screen + dialog)
- Loan documents: real uploads to local disk or Azure Blob (switchable, `LoanApi.Storage`), authenticated downloads, list and (admin) delete per loan; legacy external links still supported
- Credit score (0–1000) with recalculation and an overdue penalty
- Realistic seed data (150 customers, ~125 loans with consistent payments, Spanish descriptions) verified by tests
- Loan life cycle (create as Pending, approve, disburse, cancel, close) with segregation of duties; payments advance the schedule
- Roles ADMIN, MANAGER and USER; employee management with revocable sessions
- 344 backend tests (NUnit + SQLite in-memory) plus end-to-end runs against SQL Server for storage, life cycle and employees

❌ **Still missing** — see "Remaining Improvements" below.

---

## Priority 1: CRITICAL IMPROVEMENTS (Dominican Regulations)

### 1.1 Enhanced Loan Entity
**File:** `Domain/Entities/Loan.cs`

Add these fields:
```csharp
// Delinquency tracking (crucial for Dominican market)
public int DaysOverdue { get; set; } = 0;
public decimal DelinquencyPercentage { get; set; } = 0m;
public DateTime? LastPaymentDate { get; set; }

// Interest calculations
public decimal TotalInterestAmount { get; set; }
public decimal PaidInterest { get; set; }
public decimal RemainingInterest { get; set; }

// Balance tracking
public decimal AmortizationBalance { get; set; }
public decimal PrincipalPaid { get; set; }
public decimal PrincipalRemaining { get; set; }

// Dominican bank requirements
public string? LoanPurpose { get; set; } // "Personal", "Business", "Real Estate"
public decimal MaxCredit { get; set; } // Credit limit for customer
public bool RequiresGuarantor { get; set; }
public string? GuarantorId { get; set; } // Foreign key to another customer

// Dates as DateOnly for better precision
public DateOnly DisbursementDate { get; set; } // Already exists, make DateOnly
public DateOnly MaturityDate { get; set; } // Already exists, make DateOnly
public DateOnly NextPaymentDueDate { get; set; }

// Collections
public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();
public ICollection<LoanDocument> Documents { get; set; } = new List<LoanDocument>();
```

### 1.2 New Entity: LoanPayment
**File:** `Domain/Entities/LoanPayment.cs`

```csharp
public class LoanPayment : AuditableEntity<Guid>
{
    public Guid LoanId { get; set; }
    public Loan Loan { get; set; } = default!;
    
    public DateOnly PaymentDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal PenaltyAmount { get; set; } // Late fees (Dominican banks charge penalties)
    public decimal TotalPaymentAmount { get; set; }
    
    public string PaymentMethod { get; set; } = "Bank Transfer"; // "Cash", "Check", "Bank Transfer"
    public string? ReferenceNumber { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Reversed = 3
}
```

### 1.3 New Entity: LoanDocument
**File:** `Domain/Entities/LoanDocument.cs`

```csharp
public class LoanDocument : AuditableEntity<Guid>
{
    public Guid LoanId { get; set; }
    public Loan Loan { get; set; } = default!;
    
    public string DocumentType { get; set; } = string.Empty; // "Contract", "Promissory Note", "ID Copy", etc.
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; }
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = "application/pdf";
}
```

### 1.4 Enhanced Customer Entity
**File:** `Domain/Entities/Customer.cs`

Add Dominican-specific fields:
```csharp
// Additional required fields for Dominican compliance
public string Occupation { get; set; } = string.Empty;
public string Nationality { get; set; } = "DO";

// Financial profile
public decimal AnnualIncome { get; set; } // In DOP (Dominican Pesos)
public string IncomeSource { get; set; } = string.Empty; // "Employment", "Business", "Investment"
public decimal CreditLimit { get; set; } // Maximum credit exposure

// Risk assessment
public string RiskLevel { get; set; } = "Medium"; // "Low", "Medium", "High", "Blacklist"
public string? BlacklistReason { get; set; }
public bool IsBlacklisted { get; set; } = false;

// Contact details for compliance
public string? AlternativePhone { get; set; }
public string? EmergencyContactName { get; set; }
public string? EmergencyContactPhone { get; set; }

// Tax ID (RNC - Registro Nacional del Contribuyente)
public string? TaxId { get; set; }

// Payment history
public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();
```

---

## Priority 2: DELINQUENCY & RISK MANAGEMENT

### 2.1 New Entity: DelinquencyPolicy
**File:** `Domain/Entities/DelinquencyPolicy.cs`

```csharp
public class DelinquencyPolicy : AuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty; // "Standard", "Aggressive", "Conservative"
    
    public int DaysUntilEarlyDelinquency { get; set; } = 1; // 1 day late = delinquent
    public decimal EarlyDelinquencyPenalty { get; set; } = 0.05m; // 5% penalty
    
    public int DaysUntilSevereDelinquency { get; set; } = 30;
    public decimal SevereDelinquencyPenalty { get; set; } = 0.15m;
    
    public int DaysUntilWriteOff { get; set; } = 90; // After 90 days, write off the loan
    
    public bool AutomaticallyChargeLateFees { get; set; } = true;
    public decimal DailyPenaltyRate { get; set; } = 0.001m; // 0.1% per day
}
```

### 2.2 New Entity: CreditScore
**File:** `Domain/Entities/CreditScore.cs`

```csharp
public class CreditScore : AuditableEntity<Guid>
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    
    public int Score { get; set; } // 0-1000
    public DateTime LastCalculatedDate { get; set; }
    
    // Calculation factors
    public decimal PaymentHistoryScore { get; set; } // 35%
    public decimal CreditUtilizationScore { get; set; } // 30%
    public decimal CreditAgeScore { get; set; } // 15%
    public decimal DefaultHistoryScore { get; set; } // 20%
    
    public string CreditRating { get; set; } = "Pending"; // "Excellent", "Good", "Fair", "Poor"
}
```

---

## Priority 3: NEW API ENDPOINTS

### 3.1 Loan Management Endpoints
```
POST   /api/v1/loans                    - Create new loan
GET    /api/v1/loans/{id}               - Get loan details
GET    /api/v1/loans/customer/{customerId} - Get customer's loans
PUT    /api/v1/loans/{id}               - Update loan
GET    /api/v1/loans/{id}/amortization  - Get amortization schedule

GET    /api/v1/loans/{id}/payments      - Get payment history
POST   /api/v1/loans/{id}/payments      - Record payment
GET    /api/v1/loans/{id}/documents     - Get loan documents
POST   /api/v1/loans/{id}/documents     - Upload document

GET    /api/v1/loans/delinquent         - Get all delinquent loans
GET    /api/v1/loans/dashboard/summary  - Dashboard summary
```

### 3.2 Customer Risk Assessment Endpoints
```
GET    /api/v1/customers/{id}/credit-score    - Get customer credit score
POST   /api/v1/customers/{id}/credit-score    - Recalculate credit score
GET    /api/v1/customers/{id}/loan-history    - Full loan history
POST   /api/v1/customers/{id}/blacklist       - Add to blacklist
DELETE /api/v1/customers/{id}/blacklist       - Remove from blacklist
```

---

## Priority 4: VALIDATIONS & BUSINESS RULES

### 4.1 Dominican Cédula/Passport Validation
**File:** `Domain/ValueObjects/Identification.cs`

```csharp
public static class IdentificationValidator
{
    // Cédula format: ###-#######-# (e.g., 001-2345678-9)
    public static bool IsValidDominicanCedula(string cedula)
    {
        var pattern = @"^\d{3}-\d{7}-\d{1}$";
        return Regex.IsMatch(cedula, pattern);
    }
    
    // Passport format: ISO-2 code of the passport's country + number, no spaces or hyphens (e.g. US123456789).
    // Implemented in Application/Features/Customers/IdentificationRules.cs (6–12 digits; prefix must match the
    // customer's nationality). A cédula is only valid for Dominican nationals; a legal person uses RNC (###-#####-#).
    public static bool IsValidPassport(string passport)
    {
        var pattern = @"^[A-Z]{2}\d{9,11}$";
        return Regex.IsMatch(passport, pattern);
    }
}
```

### 4.2 Loan Validation Service
**File:** `Application/Services/LoanValidationService.cs`

```csharp
public class LoanValidationService
{
    // Check if customer can receive loan
    public async Task<LoanEligibilityResult> CheckEligibilityAsync(Customer customer, decimal loanAmount)
    {
        // Rules:
        // 1. Not blacklisted
        // 2. Credit score > 300
        // 3. Loan amount < Credit limit
        // 4. No active delinquent loans
        // 5. Income verification (annual income > loan amount / 12)
        
        return new LoanEligibilityResult { IsEligible = true, Reasons = [] };
    }
    
    // Calculate amortization schedule
    public List<AmortizationScheduleItem> GenerateAmortizationSchedule(
        decimal principal,
        decimal annualInterestRate,
        int months)
    {
        // Monthly payment = P * [r(1+r)^n] / [(1+r)^n - 1]
        // Where r = monthly rate, n = number of months
        
        var schedule = new List<AmortizationScheduleItem>();
        // Implementation...
        return schedule;
    }
}
```

### 4.3 Delinquency Calculation
**File:** `Application/Services/DelinquencyService.cs`

```csharp
public class DelinquencyService
{
    public async Task UpdateDelinquencyAsync(Loan loan)
    {
        var daysOverdue = (DateTime.UtcNow.Date - loan.NextPaymentDueDate.ToDateTime(TimeOnly.MinValue)).Days;
        
        if (daysOverdue > 0)
        {
            loan.DaysOverdue = daysOverdue;
            
            // Calculate delinquency percentage
            var totalPaymentDue = loan.InstallmentAmount;
            loan.DelinquencyPercentage = (daysOverdue * 0.01m); // 1% per day
            
            // Apply late fees
            if (daysOverdue > 1)
            {
                var penaltyAmount = loan.InstallmentAmount * loan.DelinquencyPercentage;
                // Add to outstanding balance
            }
        }
    }
}
```

---

## Priority 5: ENUMS EXPANSION

### 5.1 Enhanced LoanStatus
```csharp
public enum LoanStatus
{
    Pending = 0,
    Approved = 1,
    Disbursed = 2,
    Active = 3,           // NEW - actively being paid
    PartiallyPaid = 4,    // NEW
    FullyPaid = 5,        // NEW
    Defaulted = 6,        // Changed from Closed
    WrittenOff = 7,       // NEW - after 90 days delinquent
    Restructured = 8,     // NEW - modified terms
    Closed = 9            // Successfully completed
}
```

### 5.2 New Enums
```csharp
public enum LoanPurpose { Personal, Business, RealEstate, Agriculture, Education }
public enum IncomeSource { Employment, Business, Investment, Pension, Other }
public enum DocumentType { Contract, PromissoryNote, IDCopy, IncomeCertificate, BankStatement }
```

---

## Remaining Improvements (added 2026-09-20)

Ordered roughly from easiest to hardest. Localization is deliberately last.

### Loan lifecycle and money flow
- [x] Registering a payment now advances `NextPaymentDueDate` (installment k is due `DisbursementDate + k months`, covered installments = floor((principal + interest paid) / installment); penalties do not count), recomputes `DaysOverdue`, clears the delinquency percentage when up to date, moves a Defaulted/Disbursed loan back to Active, and puts a settled loan at maturity (`PaymentRules`, 20 tests). Verified against SQLite only, not yet against SQL Server
- [x] Late fees (`POST delinquency/{loanId}/charge-fees`, admin): each overdue day is charged exactly once (`LateFeeRules`), so repeating the request the same day charges nothing (200, 0 days) and running it daily never charges a day twice; before, every call charged installment x rate x ALL overdue days again, compounding, and it added the fee to the amortization balance and remaining interest. Fees now have their own ledger on the loan (`LateFeesCharged`, `LateFeesChargedThrough`), rounded to cents, never touching principal or interest; the loan page shows fees charged and outstanding (payments' penalty part counts as paid) and admins get a "Cargar penalidades" button. Verified on SQL Server (no existing loan carried the old bug) and in a browser
- [x] Automatic late fees: `POST delinquency/charge-fees` (admin) charges every overdue loan being collected, and a daily background job (`LateFeeBackgroundService`) does it on its own. All paths share one `LateFeeCharger`, so each overdue day is still charged exactly once and running twice, restarting or having several API instances is harmless. **The job is off by default**: enable it with `LateFees:AutoCharge:Enabled` = true (and `LateFees:AutoCharge:RunAtUtc`, default 06:00 = 02:00 in the Dominican Republic); even then the active delinquency policy must have `AutomaticallyChargeLateFees` on (the manual "charge all" ignores that flag). One failing loan does not stop the others; the run is logged (`Late fee sweep (automatic): N loan(s) checked, ...`). Button "Cargar penalidades de todos" on the delinquency screen. Verified on SQL Server: the sweep charged exactly the expected 6 loans / 302 days / RD$1,797.37, repeats charged nothing, balances untouched, and the job fired by itself at its scheduled time
- [x] Payment penalty is capped: `PenaltyAmount` cannot exceed the late fees charged minus penalties already paid (reversed payments give theirs back)
- [x] Late fees are charged per overdue installment (`LateFeeRules.Assess`): each late installment costs its own amount x the policy's daily rate for every day it stays unpaid, payments cover the oldest installments first, and every installment-day is still charged exactly once (a loan 60 days late on installment 1 and 30 on installment 2 pays for both). Verified on SQL Server. No cap and no grace period yet: both are one-line changes in `Assess` once the business decides the numbers
- [x] Delinquency is recalculated daily: `DelinquencyRefresher` updates the days overdue, percentage and status of every loan being repaid (an Active loan that stopped paying becomes Defaulted, a caught-up one goes back to Active; never a write-off, no money moves). It runs first in the daily job (`Delinquency:DailyRefresh:Enabled`, on by default, at `LateFees:AutoCharge:RunAtUtc`; late fee charging stays off unless `LateFees:AutoCharge:Enabled`), and by hand with `POST delinquency/recalculate-all` (admin or manager) and the "Actualizar mora de todos" button on the Mora screen. Tested on SQL Server; not run on your data yet, so the first run will move every loan that stopped paying to Defaulted
- [ ] Late fee follow-ups: the daily job runs inside the API process (a dedicated scheduler or an external cron calling the endpoint is sturdier for production) and the job's own runs are logged but not written to the audit trail
- [x] Loan life cycle: `POST loans/Loans/Approve|Disburse|Cancel|Close/{id}` (admin) with `LoanLifecycleRules` (Pending → Approved → Disbursed; Pending/Approved → Closed by cancelling; FullyPaid → Closed). Approving checks the customer (not blacklisted, Active, no loan over 30 days overdue, outstanding credit + this loan within the credit limit); disbursing starts the schedule today; every transition is atomic (status-guarded update). Buttons on the loan page for admins. `PUT loans/{id}` can no longer set the status, and the rate only changes while Pending (installment and interest recomputed)
- [x] Loan creation fixed: it was broken through the API (no currency), always stored the requested status, ignored interest in the installment and crashed the schedule on a 0% rate. Now: always Pending, default currency DOP, one annuity (`AmortizationSchedule`) for installment, total interest and the schedule endpoint
- [x] "New loan" screen (`/app/loans/new`, button on the loans list): customer search by name, company, ID or phone (server-side, debounced), branch, amount, annual rate and term, with a live installment / total interest preview that matches the stored values exactly. Warns above the credit limit, blocks non-active customers. The API now also refuses unknown or soft-deleted customers, blacklisted or non-active customers and unknown branches, and enforces bounds (amount up to 100,000,000, rate up to 100%, term up to 360 months, two decimals). Verified in a real browser against SQL Server
- [x] Reversing a payment (`POST payments/Reverse/{id}`, admin or manager, reason required): the payment stays in the history as Reversed with its reason and date; the loan goes back to exactly what it was without it (principal, interest, balance, last payment, next due date, days overdue, and status: a settled loan reopens, a loan that falls behind becomes delinquent). Atomic, refused for closed/written-off loans and for a payment already reversed. Reversed payments are excluded from the dashboard collections and no longer count against the credit score. Button and reason dialog on the loan page. Verified on SQL Server and in a real browser
- [ ] Reversal follow-ups: no separate "who may reverse" rule beyond admin/manager (for example a second approver for large amounts), and the reversal of a penalty is not tied to late-fee charging (which is still non-idempotent)
- [ ] The new-loan form has no purpose, guarantor or currency choice yet (DOP by default)
- [ ] Not yet run against SQL Server: the transition handler, create/update loan and the payment schedule advance are covered by SQLite tests only
- [x] Write-off is a deliberate admin action: `POST delinquency/{loanId}/write-off` (reason 5-250 chars, only after the active policy's `DaysUntilWriteOff`, audited, atomic) with a "Castigar préstamo" button on the loan page. A recalculation no longer writes loans off on its own (it used to, silently, past 90 days). Still no scheduled job that recalculates delinquency daily
- [x] Dashboard "Evolución de Mora" chart fixed: the query returned newest-first cumulative averages of `UpdatedOn` (meaningless). It now rebuilds, for each of the last 30 days, the average days overdue from each loan's schedule and the payments made (and not yet reversed) up to that day, oldest first, against the 30 days before (`DelinquencyHistory`). Nothing stores daily snapshots, so this is a reconstruction; a real snapshot table would be exact for future days
- [x] Dashboard numbers now match their labels: "Cartera Total" is the principal still owed on loans being repaid (was the sum of every loan ever created), "Cobros Mensuales" is the calendar month (was only today; RD$ 0 most days), "Préstamos Activos" counts loans being repaid (was status Active only), and the cash-flow chart runs oldest to newest with date ranges instead of "Week 1..4" and counts only disbursed loans. Follow-up: a cancelled loan is stored as Closed with its creation date, so it can still show as a disbursement in that chart
- [x] One delinquency percentage everywhere: days overdue x the active policy's daily rate x 100, capped at 100 (`LateFeeRules.DelinquencyPercentage`), used by the recalculation and refreshed after every payment and reversal
- [x] Only one delinquency policy can be active: unique filtered index `UX_DelinquencyPolicy_OneActive` (migration `SingleActiveDelinquencyPolicy`), tested against SQL Server
- [ ] Interest-rate cap (Banco Central) is not enforced in loan validation (loans are hard-limited to 100% today; the legal/business cap value is a decision to make, then it can be one config setting)
- [x] Payments record the employee who registered them (`LoanPayment.CreatedById`, set from the token) and the payments list shows a "Registrado por" column. Payments made before this change show a dash
- [x] The loan detail page shows the branch name, purpose, maximum credit and whether a guarantor is required (the guarantor themself is still not linked/shown: there is no guarantor screen)

### Customers and risk
- [x] Customer edit: `PUT /customers/{id}` now replaces the whole editable profile (validated like creation, phone uniqueness, 404 for unknown), and admins get an edit screen. Before, it blanked address/income/credit limit and returned 500 for unknown ids. Identification, type and nationality stay immutable by design; risk level is not editable here (it is derived)
- [x] Blacklist: `POST customers/Customers/Blacklist/{id}` (reason 5-250 characters) and `POST .../Unblacklist/{id}` (admin or manager, audited, guarded against double changes). Button on the customer detail page with a reason dialog. A blacklisted customer cannot get or approve a loan. The customers list now shows a "Lista negra" chip. Follow-up: a filter/report of blacklisted customers and a per-customer blacklist history
- [x] Phones by country: each country has a dial code and a national mask (`Country.PhoneCode` / `PhoneMask`; DO/US/PR `+1 999-999-9999`, MX `+52 99 9999 9999`, CO `+57 999 999 9999`, ES `+34 999 999 999`). Phones are stored canonical, `+18095550100` (`PhoneRules`): a national number typed without a code is read as Dominican, every spelling of the same number is one number for the unique index, and the customer search matches by digits. The migration converted the 150 stored phones. Forms use a phone input with a country selector (defaults to the country of residence) that masks as you type; detail page and list show `+1 809-555-0100`. Identification fields are masked too (cédula, RNC, passport). Verified in a real browser and against SQL Server
- [ ] Phone follow-ups: only six countries have dial codes/masks (add data as countries are added; a country without a mask accepts 7-15 digits), no extensions, and no check that a number is really in service
- [x] Customer detail page lists the customer's loans (`CustomerLoansCard`, first 10, row opens the loan) with a "New loan" button that opens the new-loan form with the customer already chosen (`?customerId=`)
- [ ] Credit-score model only crudely reflects delinquency; utilisation, credit age and default history factors are not weighted as described in 2.2
- [ ] Risk level and credit score can disagree; derive both from the same inputs

### Documents
- [x] Real document uploads through a storage library (`LoanApi.Storage`: `IFileStorage` with local-disk and Azure Blob providers). New files go to Azure only when it is switched on and fully configured in `Admin.StorageSetting`; otherwise to local disk. Each document records its own `StorageProvider` + `StorageKey`, so switching never breaks old files; the 290 seeded documents stay `External` (URL references). Uploads: 10 MB limit measured on the real bytes, extension allow-list, file signature must match the extension, generated storage keys (the user's name is never a path), an orphaned file is removed if the row cannot be saved. Downloads need a session, are always attachments, with `nosniff`. Configuration is server-side (`Storage:Local:RootPath` defaults to `%ProgramData%\LoanApi\files`; `Storage:Azure:ConnectionString` is optional, otherwise the managed identity is used); the table holds only account, container and the on/off switch, never a secret. Enabling Azure first writes and deletes a test file, so a broken setup cannot be activated. Settings screen at `/app/settings/storage` for ADMIN and MANAGER. Tested locally against SQL Server with local storage; **the Azure provider itself has not been run against a real Azure account** (only its selection, validation and failure paths)
- [ ] Storage follow-ups: virus scanning of uploads, a retention policy for stored files (7 years), a job that lists stored files without a row, and moving the seeded external documents to real storage
- [x] Roles: `MANAGER` ("Gerente") is seeded. ADMIN and MANAGER can approve/disburse/cancel/close loans and change a pending loan's rate, edit customers (including credit limit), read the audit trail and manage storage settings. Admin-only: deleting documents (retention), charging late fees, and (future) employees and role assignment. Everyone signed in can create customers, loans and payments, upload documents and recalculate delinquency. Segregation of duties: nobody can approve a loan they created (`LoanLifecycleRules.ApprovalProblem`; imported loans with no recorded creator can be approved by anyone allowed). Verified on SQL Server as admin; the MANAGER and USER paths are covered by the authorization attributes and unit tests only, because no such employee exists to log in with
- [x] The single-administrator blocker is gone: admins can now create managers and users (see employee management below)
- [ ] The dev `.env`/proxy limits uploads at 15 MB and the API at 11 MB; behind a reverse proxy (IIS/nginx) its own body-size limit must allow 11 MB
- [ ] 7-year retention: deleted documents are soft-deleted, but there is no retention policy or purge guard

### Application and quality
- [x] Employee management (admin only, `/app/employees`): create with a server-generated temporary password (14 chars, cryptographic random, shown once, stored hashed, `no-store` response, never in the audit trail), edit profile and role, deactivate/reactivate, reset password. Safeguards: nobody can change their own role, deactivate themselves or reset their own password here, and the last active administrator can never be demoted or deactivated (atomic guarded update). Login now refuses deactivated or removed employees (same generic message) and records the last sign-in. Sessions are revocable: tokens carry `iat`, `Admin.Employee.TokensValidFrom` is set on deactivation, role change and password reset, and every request re-checks the account behind its token, so a deactivated, demoted or reset employee loses access immediately. Verified end to end on SQL Server with real manager and user logins (permissions, revocation, protections, no password in the audit trail)
- [ ] Employee follow-ups: password reset by email or a self-service "forgot password" flow (today an admin hands over the temporary password), account lockout after repeated failed sign-ins (only the per-IP rate limit exists), multi-factor authentication, and a per-request DB check that could be cached briefly if load grows
- [ ] Currency: stay single-currency (DOP) for now. Groundwork to do with the loan lifecycle work: loans and payments already carry a `CurrencyId`, so only stop hard-coding "RD$" in `formatCurrency` and the forms (take it from the record). Rule for later: a payment must use the loan's currency and totals never mix currencies. Full multi-currency (exchange-rate source, dated rates, reporting currency) only if a USD product is required
- [x] TanStack Query installed with a `QueryClientProvider` in `providers.tsx`; `useCountries` now uses it (cached forever, shared by all screens)
- [x] TanStack Query now backs every list (`usePaginatedList` takes a `queryKey`: customers, loans, delinquency, employees, audit with its filters, a loan's payments and documents; pages are cached and the previous page stays while the next loads) and the loan detail page (loan + schedule queries, invalidated after each loan action). Checked in the browser
- [ ] Still hand-rolled with `useEffect`: dashboard, customer detail/edit/new, new loan (reference data, prefill), storage settings, employee form options. Move them to `useQuery`/`useMutation` when they are next touched
- [x] SQL Server integration tests (`SqlServerIntegrationTests`, category `SqlServer`): build a throwaway database from the real migrations + seed data, run payment→reversal and late fee→penalty cap, then drop it. Opt in with env var `LOANWEB_TEST_SQL` (e.g. `Server=MSI;Integrated Security=true;TrustServerCertificate=true`); skipped otherwise so CI without SQL Server still passes
- [ ] Grow the SQL Server integration tests (loan creation/transitions, sweep, customers) 
- [x] GitHub Actions CI (`.github/workflows/ci.yml` in each repo: LoanApi = API job, loan-web = web job): API build + unit tests + SQL Server integration tests (SQL Server 2022 service container) + vulnerable-package check; web `tsc`, `pnpm build`, `pnpm audit`. Not yet run on GitHub
- [x] Dependabot (`.github/dependabot.yml` in each repo: weekly NuGet / npm, monthly GitHub Actions)
- [ ] CI follow-ups: deploy step, branch protection requiring both jobs, running the Playwright smoke test in CI (needs the API + a seeded database there)
- [x] Bug: when a table is empty the paginator showed a stray "0" (`totalItems && totalItems > 0` in `DataTable.tsx` evaluated to the number 0 and React rendered it); fixed with `(totalItems ?? 0) > 0`
- [x] The side menu logo image was missing (404): replaced by a "LoanWeb" wordmark until the real logo file is provided (`/images/mts-logo.png` is not referenced anywhere)
- [x] Playwright fixtures/helpers fixed (sign in by pressing Enter, dev-admin default password, `BASE_URL` override) and a read-only `tests/smoke.spec.ts` added (lists and paging, loan detail, customer loans card, dashboard). Run: `BASE_URL=http://localhost:3000 pnpm playwright test smoke --project=chromium --workers=1` (one worker: the API limits logins to 5 per minute)
- [x] Localization: the API answers errors in the language the frontend sends. `UseRequestLocalization` (es default, en) reads `Accept-Language`; the frontend adds it from `localStorage['language']` (`i18n/acceptLanguage.ts`) and the Next proxy/login routes forward it. `ExceptionMiddleware` translates every error message through `Localization/{es,en}/LoanApi.po` using `MessageTemplate` (numbers, GUIDs and status words become `{0}`, `{1}`... placeholders, and status values are translated too: `Closed` -> `Cerrado`). Unknown messages fall back to English. `.po` files are copied to the output/publish folder. Tests: `LocalizationTests` (placeholder consistency of every Spanish entry). Verified through the real API and the Next proxy
- [ ] Localization follow-ups: add a missing message to `es/LoanApi.po` whenever a new error text is thrown (a source audit found ~90 that were missing and now added; none of the framework 401/403/model-binding messages are translated); dates/numbers formatting per culture in API-produced text; dev mode still shows the raw English `exception` field

---

## Implementation Timeline

**Phase 1 (Week 1):** Add enhanced Loan, Customer entities + enums
**Phase 2 (Week 2):** Add LoanPayment, LoanDocument, DelinquencyPolicy entities
**Phase 3 (Week 3):** Implement validation services + business logic
**Phase 4 (Week 4):** Create API endpoints + update Faker data
**Phase 5 (Week 5):** Add delinquency calculations + credit scoring

---

## Dominican Banking Compliance Notes

✅ **Delinquency Tracking** - CRITICAL
- Dominican banks must track daily delinquency
- Loans > 90 days delinquent must be written off per banking regulations

✅ **Currency** - Always in DOP (Dominican Pesos)
- Never mix with USD directly in calculations
- Exchange rates handled separately

✅ **Interest Rate Caps**
- Maximum interest rate regulated by Banco Central
- Enforce in validations

✅ **Identification Requirements**
- Cédula de Identidad (Primary)
- Passport (Alternative)
- RNC (Tax ID) for businesses

✅ **Record Retention**
- Keep loan documents for minimum 7 years
- Audit trail for all modifications

---

## Next Steps

1. Approve entity additions
2. Update migrations
3. Implement validation services
4. Create new API endpoints
5. Update Faker to generate realistic Dominican loan data
