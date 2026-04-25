# Dominican Loan System - API & Entities Improvements

## Current State Assessment
✅ **Working:**
- Basic Customer, Loan, Employee entities
- Clean Architecture pattern
- Authentication/Authorization
- Fake data generation

❌ **Missing for Dominican Regulations:**
- Dominican-specific validations (Cédula/Passport formats)
- Delinquency tracking (critical for Dominican banks)
- Interest calculations (simple vs compound)
- Disbursement tracking
- Payment history
- Credit risk assessment

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
    
    // Passport format: 2-letter country code + 9-11 digits
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
