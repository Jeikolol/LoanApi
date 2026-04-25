# LoanWeb API

A comprehensive loan management API built with .NET Core 8.0 following Clean Architecture principles and CQRS pattern with MediatR.

## Overview

LoanWeb API is a robust backend service for managing loans, customers, delinquency tracking, credit scoring, and financial portfolio analysis. It provides RESTful endpoints for a complete Dominican loan management system with advanced features like amortization calculations, delinquency management, and real-time portfolio analytics.

## Tech Stack

- **Framework**: .NET Core 8.0+
- **Language**: C# 12
- **Architecture**: Clean Architecture + CQRS with MediatR
- **Database**: SQL Server with Entity Framework Core + Dapper
- **Logging**: Microsoft.Extensions.Logging
- **Serialization**: System.Text.Json (camelCase naming)
- **API Documentation**: Swagger/OpenAPI
- **Versioning**: API versioning via URL routing

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server 2019 or later (LocalDB works for development)
- Visual Studio 2022 or VS Code
- Git

## Installation

### Clone Repository
```bash
cd LoanWeb/LoanApi
dotnet restore
```

### Database Setup

Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LoanWebDb;Trusted_Connection=true;"
  }
}
```

Run migrations:
```bash
cd LoanApi
dotnet ef database update --project ../Persistence
```

The seeder automatically populates initial data (customers, loans, policies, etc.).

## Running the Application

### Development Server
```bash
dotnet run --project LoanApi
```

API runs at: `https://localhost:7097/api/v1`  
Swagger UI: `https://localhost:7097/swagger`

### Watch Mode
```bash
dotnet watch --project LoanApi
```

## Project Structure

```
LoanApi/
├── Domain/
│   ├── Entities/              # Core business entities
│   │   ├── Loan.cs
│   │   ├── Customer.cs
│   │   ├── LoanPayment.cs
│   │   ├── LoanDocument.cs
│   │   └── ...
│   └── Enums/                 # Enum types
│       ├── LoanStatus.cs
│       ├── PaymentStatus.cs
│       └── ...
│
├── Application/
│   ├── Features/              # CQRS features by domain
│   │   ├── Loans/
│   │   │   ├── Queries/
│   │   │   ├── Commands/
│   │   │   └── Handlers/
│   │   ├── Payments/
│   │   ├── Documents/
│   │   ├── Customers/
│   │   ├── Delinquency/
│   │   └── CreditScores/
│   ├── Services/              # Business logic services
│   │   ├── LoanValidationService.cs
│   │   ├── DelinquencyService.cs
│   │   ├── CreditScoreService.cs
│   │   └── IdentificationValidator.cs
│   └── Models/
│       ├── Requests/
│       └── Responses/
│
├── Infrastructure/
│   ├── DataAccess/
│   ├── Authentication/
│   └── Services/
│
├── Persistence/
│   ├── Migrations/
│   └── Configuration/
│
└── LoanApi/
    ├── Controllers/
    │   ├── Modules/
    │   │   ├── Loans/
    │   │   ├── Customers/
    │   │   ├── Payments/
    │   │   └── Documents/
    │   └── AuthController.cs
    ├── Middleware/
    ├── Program.cs
    └── appsettings.json
```

## API Endpoints

### Loans
```
GET    /api/v1/loans                      # List all loans
GET    /api/v1/loans/{id}                 # Get loan details
POST   /api/v1/loans                      # Create loan
PUT    /api/v1/loans/{id}                 # Update loan
GET    /api/v1/loans/GetAmortization/{id} # Get amortization schedule
GET    /api/v1/loans/GetPayments/{id}     # Get payment history
POST   /api/v1/loans/CreatePayment/{id}   # Record payment
GET    /api/v1/loans/GetDocuments/{id}    # Get loan documents
POST   /api/v1/loans/CreateDocument/{id}  # Upload document
GET    /api/v1/loans/GetDelinquent        # Get delinquent loans
GET    /api/v1/loans/dashboard/summary    # Dashboard summary
```

### Customers
```
GET    /api/v1/customers/{id}             # Get customer details
PUT    /api/v1/customers/{id}             # Update customer
```

### Payments
```
GET    /api/v1/payments/loan/{loanId}     # Get payments for loan
POST   /api/v1/payments/loan/{loanId}     # Create payment
```

### Documents
```
GET    /api/v1/documents/loan/{loanId}    # Get documents for loan
POST   /api/v1/documents/CreateForLoan/{loanId} # Upload document
DELETE /api/v1/documents/{id}              # Delete document
```

### Delinquency
```
GET    /api/v1/delinquency/{loanId}       # Get delinquency status
POST   /api/v1/delinquency/{loanId}/calculate # Calculate delinquency
POST   /api/v1/delinquency/{loanId}/charge-fees # Charge late fees
```

### Credit Scores
```
GET    /api/v1/credit-scores/{customerId} # Get credit score
POST   /api/v1/credit-scores/{customerId}/recalculate # Recalculate score
```

### Dashboard
```
GET    /api/v1/dashboard/portfolio-summary    # Portfolio metrics
GET    /api/v1/dashboard/portfolio-status     # Status breakdown
GET    /api/v1/dashboard/delinquency-trend    # Delinquency evolution
GET    /api/v1/dashboard/cash-flow            # Cash flow analysis
GET    /api/v1/dashboard/branch-portfolio     # Branch performance
```

### Authentication
```
POST   /api/v1/auth/login                 # Login
POST   /api/v1/auth/logout                # Logout
POST   /api/v1/auth/refresh               # Refresh token
```

## Core Services

### LoanValidationService
- Check loan eligibility based on credit score, income, delinquency
- Generate amortization schedules with proper calculations
- Calculate payment amounts

### DelinquencyService
- Update delinquency status based on payment history
- Calculate late fees based on policies
- Track days overdue

### CreditScoreService
- Calculate credit scores from payment history
- Evaluate credit utilization
- Assess default risk

### IdentificationValidator
- Validate Dominican ID numbers (Cédula)
- Verify identification format and check digits

## Database Schema

### Key Tables
- `[Customers].[Customer]` - Customer profiles
- `[Customers].[CreditScore]` - Credit score history
- `[Loans].[Loan]` - Loan records
- `[Loans].[LoanPayment]` - Payment history
- `[Loans].[LoanDocument]` - Document storage
- `[Admin].[DelinquencyPolicy]` - Delinquency rules
- `[Reference].[Currency]` - Currency definitions

### Important: Soft Deletes
All entities use `IsDeleted` flag. Always include `AND IsDeleted = 0` in queries.

## Enums

### LoanStatus
```
Pending = 1, Approved = 2, Disbursed = 3, Active = 4,
PartiallPaid = 5, FullyPaid = 6, Defaulted = 7,
WrittenOff = 8, Restructured = 9, Closed = 10
```

### PaymentStatus
```
Pending = 1, Completed = 2, Failed = 3, Cancelled = 4
```

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "connection_string"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "ExpirationMinutes": 60,
    "RefreshExpirationDays": 7
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

## JSON Serialization

All responses use camelCase naming:
```csharp
options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
```

## Error Handling

Exception types:
- `NotFoundException` - Resource not found
- `UnauthorizedException` - Authentication/authorization failed
- `ValidationException` - Input validation failed

Response format:
```json
{
  "success": false,
  "statusCode": 404,
  "message": "Loan not found",
  "exception": "NotFoundException"
}
```

## Migrations

### Create Migration
```bash
dotnet ef migrations add MigrationName --project Persistence
```

### Apply Migration
```bash
dotnet ef database update --project Persistence
```

### Revert Migration
```bash
dotnet ef database update PreviousMigrationName --project Persistence
```

## Code Style

- **Naming**: Classes/Methods/Properties PascalCase, _fields camelCase
- **C# Guidelines**: Nullable reference types, SOLID principles
- **Async**: All I/O operations are async with proper ConfigureAwait(false)

## Deployment

### Build Release
```bash
dotnet publish -c Release -o ./publish
```

### Environment Variables
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=prod_connection_string
JwtSettings__SecretKey=production_secret
```

## Troubleshooting

### Database Connection Issues
- Verify SQL Server is running
- Check connection string in appsettings.json
- Ensure database exists

### Migration Failures
- Install Entity Framework tools: `dotnet tool install -g dotnet-ef`
- Check migration history in `__EFMigrationsHistory` table

### JWT Token Issues
- Verify secret key matches frontend configuration
- Check token expiration time
- Use format: `Authorization: Bearer <token>`

### CORS Errors
- Verify frontend URL in CORS policy
- Check allowed headers and methods
- Enable credentials if using cookies

## Contributing

1. Create feature branch: `git checkout -b feature/your-feature`
2. Follow clean architecture principles
3. Write meaningful commit messages
4. Include tests for new features

## API Documentation

Full API documentation available at:
- **Swagger UI**: `https://localhost:7097/swagger`
- **Swagger JSON**: `https://localhost:7097/swagger/v1/swagger.json`

## License

Proprietary - LoanWeb System

## Support

For issues or questions, contact the development team.

---

**Last Updated**: April 2026  
**Version**: 1.0.0  
**.NET Version**: 8.0+  
**SQL Server**: 2019+