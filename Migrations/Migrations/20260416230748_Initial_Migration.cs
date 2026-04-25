using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Admin");

            migrationBuilder.EnsureSchema(
                name: "Reference");

            migrationBuilder.EnsureSchema(
                name: "Customers");

            migrationBuilder.EnsureSchema(
                name: "Loans");

            migrationBuilder.CreateTable(
                name: "Branch",
                schema: "Admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branch", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Country",
                schema: "Reference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CodeIso2 = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nationality = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditScore",
                schema: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    LastCalculatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentHistoryScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditUtilizationScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAgeScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DefaultHistoryScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditRating = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditScore", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currency",
                schema: "Reference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeIso2 = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    CodeIso3 = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currency", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                schema: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerType = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdentificationTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Identification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NationalityCountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Occupation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AnnualIncome = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IncomeSource = table.Column<int>(type: "int", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RiskLevel = table.Column<int>(type: "int", nullable: true),
                    BlacklistReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsBlacklisted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AlternativePhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customer_Country_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "Reference",
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customer_Country_NationalityCountryId",
                        column: x => x.NationalityCountryId,
                        principalSchema: "Reference",
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DelinquencyPolicy",
                schema: "Loans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DaysUntilEarlyDelinquency = table.Column<int>(type: "int", nullable: false),
                    EarlyDelinquencyPenalty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DaysUntilSevereDelinquency = table.Column<int>(type: "int", nullable: false),
                    SevereDelinquencyPenalty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DaysUntilWriteOff = table.Column<int>(type: "int", nullable: false),
                    AutomaticallyChargeLateFees = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DailyPenaltyRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelinquencyPolicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                schema: "Admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastLoginOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employee_Branch_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "Admin",
                        principalTable: "Branch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Employee_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Employee_DeletedById",
                        column: x => x.DeletedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Employee_UpdatedById",
                        column: x => x.UpdatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentificationType",
                schema: "Admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Mask = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentificationType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentificationType_Employee_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdentificationType_Employee_DeletedById",
                        column: x => x.DeletedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdentificationType_Employee_UpdatedById",
                        column: x => x.UpdatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Loan",
                schema: "Loans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TermMonths = table.Column<int>(type: "int", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LoanStatus = table.Column<int>(type: "int", nullable: false),
                    DisbursementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MaturityDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NextPaymentDueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DaysOverdue = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DelinquencyPercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    LastPaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalInterestAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidInterest = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingInterest = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmortizationBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrincipalPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrincipalRemaining = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LoanPurpose = table.Column<int>(type: "int", nullable: true),
                    MaxCredit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RequiresGuarantor = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    GuarantorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Loan_Branch_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "Admin",
                        principalTable: "Branch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loan_Currency_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "Reference",
                        principalTable: "Currency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loan_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Customers",
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loan_Employee_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loan_Employee_DeletedById",
                        column: x => x.DeletedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loan_Employee_UpdatedById",
                        column: x => x.UpdatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "Admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Role_Employee_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Role_Employee_DeletedById",
                        column: x => x.DeletedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Role_Employee_UpdatedById",
                        column: x => x.UpdatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanDocument",
                schema: "Loans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanDocument_Employee_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanDocument_Employee_DeletedById",
                        column: x => x.DeletedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanDocument_Employee_UpdatedById",
                        column: x => x.UpdatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanDocument_Loan_LoanId",
                        column: x => x.LoanId,
                        principalSchema: "Loans",
                        principalTable: "Loan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanPayment",
                schema: "Loans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InterestAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPaymentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanPayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanPayment_Currency_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "Reference",
                        principalTable: "Currency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPayment_Employee_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPayment_Employee_DeletedById",
                        column: x => x.DeletedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPayment_Employee_UpdatedById",
                        column: x => x.UpdatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPayment_Loan_LoanId",
                        column: x => x.LoanId,
                        principalSchema: "Loans",
                        principalTable: "Loan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branch_Code",
                schema: "Admin",
                table: "Branch",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_CreatedById",
                schema: "Admin",
                table: "Branch",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_CreatedOn",
                schema: "Admin",
                table: "Branch",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_DeletedById",
                schema: "Admin",
                table: "Branch",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_IsDeleted",
                schema: "Admin",
                table: "Branch",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_UpdatedById",
                schema: "Admin",
                table: "Branch",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Country_CreatedById",
                schema: "Reference",
                table: "Country",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Country_CreatedOn",
                schema: "Reference",
                table: "Country",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Country_DeletedById",
                schema: "Reference",
                table: "Country",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Country_IsDeleted",
                schema: "Reference",
                table: "Country",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Country_UpdatedById",
                schema: "Reference",
                table: "Country",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CreditScore_CreatedById",
                schema: "Customers",
                table: "CreditScore",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CreditScore_CreatedOn",
                schema: "Customers",
                table: "CreditScore",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_CreditScore_CustomerId",
                schema: "Customers",
                table: "CreditScore",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditScore_DeletedById",
                schema: "Customers",
                table: "CreditScore",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CreditScore_IsDeleted",
                schema: "Customers",
                table: "CreditScore",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CreditScore_UpdatedById",
                schema: "Customers",
                table: "CreditScore",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Currency_CreatedById",
                schema: "Reference",
                table: "Currency",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Currency_CreatedOn",
                schema: "Reference",
                table: "Currency",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Currency_DeletedById",
                schema: "Reference",
                table: "Currency",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Currency_IsDeleted",
                schema: "Reference",
                table: "Currency",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Currency_UpdatedById",
                schema: "Reference",
                table: "Currency",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CountryId",
                schema: "Customers",
                table: "Customer",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CreatedById",
                schema: "Customers",
                table: "Customer",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CreatedOn",
                schema: "Customers",
                table: "Customer",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_DeletedById",
                schema: "Customers",
                table: "Customer",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Email",
                schema: "Customers",
                table: "Customer",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Identification",
                schema: "Customers",
                table: "Customer",
                column: "Identification",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_IdentificationTypeId",
                schema: "Customers",
                table: "Customer",
                column: "IdentificationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_IsDeleted",
                schema: "Customers",
                table: "Customer",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_NationalityCountryId",
                schema: "Customers",
                table: "Customer",
                column: "NationalityCountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Phone",
                schema: "Customers",
                table: "Customer",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_UpdatedById",
                schema: "Customers",
                table: "Customer",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DelinquencyPolicy_CreatedById",
                schema: "Loans",
                table: "DelinquencyPolicy",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DelinquencyPolicy_CreatedOn",
                schema: "Loans",
                table: "DelinquencyPolicy",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_DelinquencyPolicy_DeletedById",
                schema: "Loans",
                table: "DelinquencyPolicy",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DelinquencyPolicy_IsDeleted",
                schema: "Loans",
                table: "DelinquencyPolicy",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DelinquencyPolicy_UpdatedById",
                schema: "Loans",
                table: "DelinquencyPolicy",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_BranchId",
                schema: "Admin",
                table: "Employee",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_CreatedById",
                schema: "Admin",
                table: "Employee",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_CreatedOn",
                schema: "Admin",
                table: "Employee",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_DeletedById",
                schema: "Admin",
                table: "Employee",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Email",
                schema: "Admin",
                table: "Employee",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_IsDeleted",
                schema: "Admin",
                table: "Employee",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_RoleId",
                schema: "Admin",
                table: "Employee",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_UpdatedById",
                schema: "Admin",
                table: "Employee",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_UserName",
                schema: "Admin",
                table: "Employee",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentificationType_Code",
                schema: "Admin",
                table: "IdentificationType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentificationType_CreatedById",
                schema: "Admin",
                table: "IdentificationType",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_IdentificationType_CreatedOn",
                schema: "Admin",
                table: "IdentificationType",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_IdentificationType_DeletedById",
                schema: "Admin",
                table: "IdentificationType",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_IdentificationType_IsDeleted",
                schema: "Admin",
                table: "IdentificationType",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_IdentificationType_UpdatedById",
                schema: "Admin",
                table: "IdentificationType",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_BranchId",
                schema: "Loans",
                table: "Loan",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_CreatedById",
                schema: "Loans",
                table: "Loan",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_CreatedOn",
                schema: "Loans",
                table: "Loan",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_CurrencyId",
                schema: "Loans",
                table: "Loan",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_CustomerId",
                schema: "Loans",
                table: "Loan",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_DeletedById",
                schema: "Loans",
                table: "Loan",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_IsDeleted",
                schema: "Loans",
                table: "Loan",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_UpdatedById",
                schema: "Loans",
                table: "Loan",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDocument_CreatedById",
                schema: "Loans",
                table: "LoanDocument",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDocument_CreatedOn",
                schema: "Loans",
                table: "LoanDocument",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDocument_DeletedById",
                schema: "Loans",
                table: "LoanDocument",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDocument_IsDeleted",
                schema: "Loans",
                table: "LoanDocument",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDocument_LoanId",
                schema: "Loans",
                table: "LoanDocument",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDocument_UpdatedById",
                schema: "Loans",
                table: "LoanDocument",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayment_CreatedById",
                schema: "Loans",
                table: "LoanPayment",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayment_CreatedOn",
                schema: "Loans",
                table: "LoanPayment",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayment_CurrencyId",
                schema: "Loans",
                table: "LoanPayment",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayment_DeletedById",
                schema: "Loans",
                table: "LoanPayment",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayment_IsDeleted",
                schema: "Loans",
                table: "LoanPayment",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayment_LoanId",
                schema: "Loans",
                table: "LoanPayment",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayment_UpdatedById",
                schema: "Loans",
                table: "LoanPayment",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Code",
                schema: "Admin",
                table: "Role",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Role_CreatedById",
                schema: "Admin",
                table: "Role",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Role_CreatedOn",
                schema: "Admin",
                table: "Role",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Role_DeletedById",
                schema: "Admin",
                table: "Role",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Role_IsDeleted",
                schema: "Admin",
                table: "Role",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Role_UpdatedById",
                schema: "Admin",
                table: "Role",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Branch_Employee_CreatedById",
                schema: "Admin",
                table: "Branch",
                column: "CreatedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Branch_Employee_DeletedById",
                schema: "Admin",
                table: "Branch",
                column: "DeletedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Branch_Employee_UpdatedById",
                schema: "Admin",
                table: "Branch",
                column: "UpdatedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Country_Employee_CreatedById",
                schema: "Reference",
                table: "Country",
                column: "CreatedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Country_Employee_DeletedById",
                schema: "Reference",
                table: "Country",
                column: "DeletedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Country_Employee_UpdatedById",
                schema: "Reference",
                table: "Country",
                column: "UpdatedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditScore_Customer_CustomerId",
                schema: "Customers",
                table: "CreditScore",
                column: "CustomerId",
                principalSchema: "Customers",
                principalTable: "Customer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditScore_Employee_CreatedById",
                schema: "Customers",
                table: "CreditScore",
                column: "CreatedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditScore_Employee_DeletedById",
                schema: "Customers",
                table: "CreditScore",
                column: "DeletedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditScore_Employee_UpdatedById",
                schema: "Customers",
                table: "CreditScore",
                column: "UpdatedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Currency_Employee_CreatedById",
                schema: "Reference",
                table: "Currency",
                column: "CreatedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Currency_Employee_DeletedById",
                schema: "Reference",
                table: "Currency",
                column: "DeletedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Currency_Employee_UpdatedById",
                schema: "Reference",
                table: "Currency",
                column: "UpdatedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_Employee_CreatedById",
                schema: "Customers",
                table: "Customer",
                column: "CreatedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_Employee_DeletedById",
                schema: "Customers",
                table: "Customer",
                column: "DeletedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_Employee_UpdatedById",
                schema: "Customers",
                table: "Customer",
                column: "UpdatedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_IdentificationType_IdentificationTypeId",
                schema: "Customers",
                table: "Customer",
                column: "IdentificationTypeId",
                principalSchema: "Admin",
                principalTable: "IdentificationType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DelinquencyPolicy_Employee_CreatedById",
                schema: "Loans",
                table: "DelinquencyPolicy",
                column: "CreatedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DelinquencyPolicy_Employee_DeletedById",
                schema: "Loans",
                table: "DelinquencyPolicy",
                column: "DeletedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DelinquencyPolicy_Employee_UpdatedById",
                schema: "Loans",
                table: "DelinquencyPolicy",
                column: "UpdatedById",
                principalSchema: "Admin",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employee_Role_RoleId",
                schema: "Admin",
                table: "Employee",
                column: "RoleId",
                principalSchema: "Admin",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branch_Employee_CreatedById",
                schema: "Admin",
                table: "Branch");

            migrationBuilder.DropForeignKey(
                name: "FK_Branch_Employee_DeletedById",
                schema: "Admin",
                table: "Branch");

            migrationBuilder.DropForeignKey(
                name: "FK_Branch_Employee_UpdatedById",
                schema: "Admin",
                table: "Branch");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Employee_CreatedById",
                schema: "Admin",
                table: "Role");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Employee_DeletedById",
                schema: "Admin",
                table: "Role");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Employee_UpdatedById",
                schema: "Admin",
                table: "Role");

            migrationBuilder.DropTable(
                name: "CreditScore",
                schema: "Customers");

            migrationBuilder.DropTable(
                name: "DelinquencyPolicy",
                schema: "Loans");

            migrationBuilder.DropTable(
                name: "LoanDocument",
                schema: "Loans");

            migrationBuilder.DropTable(
                name: "LoanPayment",
                schema: "Loans");

            migrationBuilder.DropTable(
                name: "Loan",
                schema: "Loans");

            migrationBuilder.DropTable(
                name: "Currency",
                schema: "Reference");

            migrationBuilder.DropTable(
                name: "Customer",
                schema: "Customers");

            migrationBuilder.DropTable(
                name: "Country",
                schema: "Reference");

            migrationBuilder.DropTable(
                name: "IdentificationType",
                schema: "Admin");

            migrationBuilder.DropTable(
                name: "Employee",
                schema: "Admin");

            migrationBuilder.DropTable(
                name: "Branch",
                schema: "Admin");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "Admin");
        }
    }
}
