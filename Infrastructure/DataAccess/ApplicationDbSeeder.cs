using Application.FakeData;
using Dapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System.Data;

namespace Infrastructure.DataAccess
{
    public class ApplicationDbSeeder
    {
        private readonly IDbConnection _connection;

        public ApplicationDbSeeder(IDbConnection connection)
        {
            _connection = connection;
        }

        private LoanPortfolio? _portfolio;

        public async Task SeedDatabaseAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            try
            {
                // Seed static data first
                await SeedCountriesAsync(dbContext, cancellationToken);
                await SeedCurrenciesAsync(dbContext, cancellationToken);
                await SeedRolesAsync(dbContext, cancellationToken);
                await SeedStorageSettingsAsync(dbContext, cancellationToken);
                await SeedBranchesAsync(dbContext, cancellationToken);
                await SeedIdentificationTypesAsync(dbContext, cancellationToken);
                await SeedDelinquencyPoliciesAsync(dbContext, cancellationToken);
                await SeedAdminUserAsync(dbContext, cancellationToken);

                // Then seed dependent entities using actual IDs
                await SeedCustomersAsync(cancellationToken);
                await SeedLoansAsync(cancellationToken);
                await SeedCreditScoresAsync(cancellationToken);
                await SeedLoanDocumentsAsync(cancellationToken);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task SeedRolesAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            try
            {
                var roles = new List<Role>
                {
                    new Role
                    {
                        Id = SystemConstants.DefaultRoleId,
                        Code = "ADMIN",
                        Name = "Administrador",
                        Description = "Administrador del sistema con acceso completo",
                    },
                    new Role
                    {
                        Id = Guid.NewGuid(),
                        Code = "USER",
                        Name = "Usuario estándar",
                        Description = "Usuario con acceso limitado",
                    },
                    new Role
                    {
                        Id = Guid.NewGuid(),
                        Code = "MANAGER",
                        Name = "Gerente",
                        Description = "Gerente: administra la configuración del sistema, como el almacenamiento de documentos",
                    }
                };

                // Roles are added by code, so a role introduced later reaches databases that were seeded before it.
                List<string> existing = await dbContext.Roles.Select(r => r.Code).ToListAsync(cancellationToken);
                var missing = roles.Where(r => !existing.Contains(r.Code)).ToList();
                if (missing.Count == 0)
                    return;

                await dbContext.Roles.AddRangeAsync(missing);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task SeedStorageSettingsAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            if (await dbContext.StorageSettings.AnyAsync(cancellationToken))
                return;

            // Azure starts switched off, so new documents go to local storage until an admin or manager enables it.
            await dbContext.StorageSettings.AddAsync(new StorageSetting { Id = Guid.NewGuid(), AzureEnabled = false }, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedBranchesAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            if (await dbContext.Branches.AnyAsync(cancellationToken))
                return;

            var branches = new List<Branch>
            {
                new Branch
                {
                    Id = SystemConstants.DefaultBranchId,
                    Code = "01",
                    Name = "Sucursal Principal",
                    Address = "Av. 27 de Febrero #100, Santo Domingo",
                },
                new Branch
                {
                    Id = Guid.NewGuid(),
                    Code = "02",
                    Name = "Sucursal Santiago",
                    Address = "Av. Juan Pablo Duarte #45, Santiago",
                }
            };

            await dbContext.Branches.AddRangeAsync(branches);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedIdentificationTypesAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {

            var identificationTypes = new List<IdentificationType>
            {
                new IdentificationType
                {
                    Id = SystemConstants.CedulaIdentificationTypeId,
                    Code = "CEDULA",
                    Name = "Cédula de Identidad",
                    Description = "Documento de identidad nacional dominicano",
                    Mask = "999-9999999-9"
                },
                new IdentificationType
                {
                    Id = SystemConstants.PassportIdentificationTypeId,
                    Code = "PASSPORT",
                    Name = "Pasaporte",
                    Description = "Pasaporte internacional (extranjeros)",
                    Mask = "AA999999999999"
                },
                new IdentificationType
                {
                    Id = SystemConstants.RncIdentificationTypeId,
                    Code = "RNC",
                    Name = "RNC",
                    Description = "Registro Nacional del Contribuyente (personas jurídicas)",
                    Mask = "999-99999-9"
                }
            };

            // Add only the types that are missing, so databases created before a type existed pick it up on the next start.
            var existingIds = await dbContext.IdentificationTypes.Select(t => t.Id).ToListAsync(cancellationToken);
            var missing = identificationTypes.Where(t => !existingIds.Contains(t.Id)).ToList();
            if (missing.Count == 0)
                return;

            await dbContext.IdentificationTypes.AddRangeAsync(missing);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedDelinquencyPoliciesAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            if (await dbContext.DelinquencyPolicies.AnyAsync(cancellationToken))
                return;

            var policies = FakeDbQueryDelinquencyPolicies.DelinquencyPolicies;
            await dbContext.DelinquencyPolicies.AddRangeAsync(policies);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedAdminUserAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            if (await dbContext.Employees.AnyAsync(cancellationToken))
                return;

            var hasher = new PasswordHasher<Employee>();
            var employee = new Employee
            {
                Id = SystemConstants.DefaultAdminId,
                FirstName = "Administrador",
                LastName = "del Sistema",
                Email = "admin@admin.com",
                UserName = "admin",
                RoleId = SystemConstants.DefaultRoleId,
                BranchId = SystemConstants.DefaultBranchId,
                CreatedOn = DateTime.UtcNow,
                MustChangePassword = true,
            };

            employee.PasswordHash = hasher.HashPassword(employee, SystemConstants.DefaultAdminPassword);
            await dbContext.Employees.AddAsync(employee);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedCountriesAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            if (await dbContext.Countries.AnyAsync(cancellationToken))
                return;

            var countries = FakeDbQueryCountries.Countries;
            await dbContext.Countries.AddRangeAsync(countries);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedCurrenciesAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            if (await dbContext.Currencies.AnyAsync(cancellationToken))
                return;

            var currencies = FakeDbQueryCurrencies.Currencies;
            await dbContext.Currencies.AddRangeAsync(currencies);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedCustomersAsync(CancellationToken cancellationToken)
        {
            try
            {
                var sqlConn = _connection as SqlConnection
                ?? throw new InvalidOperationException("IDbConnection must be SqlConnection for SqlBulkCopy.");

                if (await sqlConn.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM [Customers].[Customer]") > 0)
                    return;

                var dt = new DataTable();
                dt.Columns.Add("Id", typeof(Guid));
                dt.Columns.Add("CustomerType", typeof(int));
                dt.Columns.Add("FirstName", typeof(string));
                dt.Columns.Add("LastName", typeof(string));
                dt.Columns.Add("CompanyName", typeof(string));
                dt.Columns.Add("IdentificationTypeId", typeof(Guid));
                dt.Columns.Add("Identification", typeof(string));
                dt.Columns.Add("Phone", typeof(string));
                dt.Columns.Add("Email", typeof(string));
                dt.Columns.Add("Address", typeof(string));
                dt.Columns.Add("Province", typeof(string));
                dt.Columns.Add("CountryId", typeof(Guid));
                dt.Columns.Add("NationalityCountryId", typeof(Guid));
                dt.Columns.Add("Status", typeof(int));
                dt.Columns.Add("Occupation", typeof(string));
                dt.Columns.Add("AnnualIncome", typeof(decimal));
                dt.Columns.Add("IncomeSource", typeof(int));
                dt.Columns.Add("CreditLimit", typeof(decimal));
                dt.Columns.Add("RiskLevel", typeof(int));
                dt.Columns.Add("BlacklistReason", typeof(string));
                dt.Columns.Add("IsBlacklisted", typeof(bool));
                dt.Columns.Add("AlternativePhone", typeof(string));
                dt.Columns.Add("EmergencyContactName", typeof(string));
                dt.Columns.Add("EmergencyContactPhone", typeof(string));
                dt.Columns.Add("TaxId", typeof(string));
                dt.Columns.Add("CreatedOn", typeof(DateTime));
                dt.Columns.Add("CreatedById", typeof(Guid));
                dt.Columns.Add("UpdatedOn", typeof(DateTime));
                dt.Columns.Add("UpdatedById", typeof(Guid));
                dt.Columns.Add("IsActive", typeof(bool));
                dt.Columns.Add("IsDeleted", typeof(bool));

                IReadOnlyList<Customer> customers = FakeDbQueryCustomers.Generate();

                foreach (var customer in customers)
                {
                    dt.Rows.Add(
                        customer.Id,
                        (int)customer.CustomerType,
                        customer.FirstName,
                        customer.LastName,
                        customer.CompanyName ?? (object)DBNull.Value,
                        customer.IdentificationTypeId,
                        customer.Identification,
                        customer.Phone,
                        customer.Email ?? (object)DBNull.Value,
                        customer.Address,
                        customer.Province,
                        customer.CountryId,
                        customer.NationalityCountryId,
                        (int)customer.Status,
                        customer.Occupation,
                        customer.AnnualIncome,
                        (int?)customer.IncomeSource,
                        customer.CreditLimit,
                        (int?)customer.RiskLevel ?? (object)DBNull.Value,
                        customer.BlacklistReason ?? (object)DBNull.Value,
                        customer.IsBlacklisted,
                        customer.AlternativePhone ?? (object)DBNull.Value,
                        customer.EmergencyContactName ?? (object)DBNull.Value,
                        customer.EmergencyContactPhone ?? (object)DBNull.Value,
                        customer.TaxId ?? (object)DBNull.Value,
                        customer.CreatedOn,
                        (object)DBNull.Value,
                        (object)DBNull.Value,
                        (object)DBNull.Value,
                        true,
                        false
                    );
                }

                await BulkInsertAsync(sqlConn, "[Customers].[Customer]", dt, cancellationToken);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task SeedLoansAsync(CancellationToken cancellationToken)
        {
            var sqlConn = _connection as SqlConnection
                ?? throw new InvalidOperationException("IDbConnection must be SqlConnection for SqlBulkCopy.");

            if (await sqlConn.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM [Loans].[Loan]") > 0)
                return;

            // Only customers who are allowed to borrow get loans, sized from their own income and credit limit.
            var profiles = (await sqlConn.QueryAsync<(Guid Id, decimal AnnualIncome, decimal CreditLimit)>(
                "SELECT Id, ISNULL(AnnualIncome, 0), CreditLimit FROM [Customers].[Customer] WHERE Status = 1 AND IsBlacklisted = 0 AND IsDeleted = 0"
            )).Select(c => new CustomerProfile(c.Id, c.AnnualIncome, c.CreditLimit)).ToList();

            var branchIds = (await sqlConn.QueryAsync<Guid>(
                "SELECT Id FROM [Admin].[Branch]"
            )).ToList();

            if (profiles.Count == 0 || branchIds.Count == 0)
                throw new InvalidOperationException("Seed customers and branches first.");

            var portfolio = FakeDbQueryLoanPortfolio.Generate(profiles, branchIds, DateOnly.FromDateTime(DateTime.UtcNow));
            var loans = portfolio.Loans;

            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(Guid));
            dt.Columns.Add("CustomerId", typeof(Guid));
            dt.Columns.Add("BranchId", typeof(Guid));
            dt.Columns.Add("CurrencyId", typeof(Guid));
            dt.Columns.Add("LoanNumber", typeof(string));
            dt.Columns.Add("PrincipalAmount", typeof(decimal));
            dt.Columns.Add("InterestRate", typeof(decimal));
            dt.Columns.Add("TermMonths", typeof(int));
            dt.Columns.Add("InstallmentAmount", typeof(decimal));
            dt.Columns.Add("LoanStatus", typeof(int));
            dt.Columns.Add("DisbursementDate", typeof(DateTime));
            dt.Columns.Add("MaturityDate", typeof(DateTime));
            dt.Columns.Add("NextPaymentDueDate", typeof(DateTime));
            dt.Columns.Add("DaysOverdue", typeof(int));
            dt.Columns.Add("DelinquencyPercentage", typeof(decimal));
            dt.Columns.Add("LastPaymentDate", typeof(DateTime));
            dt.Columns.Add("TotalInterestAmount", typeof(decimal));
            dt.Columns.Add("PaidInterest", typeof(decimal));
            dt.Columns.Add("RemainingInterest", typeof(decimal));
            dt.Columns.Add("AmortizationBalance", typeof(decimal));
            dt.Columns.Add("PrincipalPaid", typeof(decimal));
            dt.Columns.Add("PrincipalRemaining", typeof(decimal));
            dt.Columns.Add("LoanPurpose", typeof(int));
            dt.Columns.Add("MaxCredit", typeof(decimal));
            dt.Columns.Add("RequiresGuarantor", typeof(bool));
            dt.Columns.Add("GuarantorId", typeof(Guid));
            dt.Columns.Add("CreatedOn", typeof(DateTime));
            dt.Columns.Add("CreatedById", typeof(Guid));
            dt.Columns.Add("UpdatedOn", typeof(DateTime));
            dt.Columns.Add("UpdatedById", typeof(Guid));
            dt.Columns.Add("IsActive", typeof(bool));
            dt.Columns.Add("IsDeleted", typeof(bool));

            foreach (var loan in loans)
            {
                dt.Rows.Add(
                    loan.Id,
                    loan.CustomerId,
                    loan.BranchId,
                    loan.CurrencyId,
                    loan.LoanNumber,
                    loan.PrincipalAmount,
                    loan.InterestRate,
                    loan.TermMonths,
                    loan.InstallmentAmount,
                    (int)loan.LoanStatus,
                    loan.DisbursementDate.ToDateTime(TimeOnly.MinValue),
                    loan.MaturityDate.ToDateTime(TimeOnly.MinValue),
                    loan.NextPaymentDueDate.ToDateTime(TimeOnly.MinValue),
                    loan.DaysOverdue,
                    loan.DelinquencyPercentage,
                    loan.LastPaymentDate,
                    loan.TotalInterestAmount,
                    loan.PaidInterest,
                    loan.RemainingInterest,
                    loan.AmortizationBalance,
                    loan.PrincipalPaid,
                    loan.PrincipalRemaining,
                    (int?)loan.LoanPurpose,
                    loan.MaxCredit,
                    loan.RequiresGuarantor,
                    loan.GuarantorId ?? (object)DBNull.Value,
                    loan.CreatedOn,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    true,
                    false
                );
            }

            await BulkInsertAsync(sqlConn, "[Loans].[Loan]", dt, cancellationToken);
            await BulkInsertPaymentsAsync(sqlConn, portfolio.Payments, cancellationToken);
            await DeriveCustomerFieldsFromLoansAsync(sqlConn);

            _portfolio = portfolio;
        }

        /// <summary>
        /// A customer's risk level and how long they have been a customer follow from their loan history,
        /// so they are computed from the seeded loans instead of being random.
        /// </summary>
        private static async Task DeriveCustomerFieldsFromLoansAsync(SqlConnection sqlConn)
        {
            await sqlConn.ExecuteAsync(@"
                UPDATE c
                SET RiskLevel = CASE
                    WHEN c.IsBlacklisted = 1 THEN 3
                    WHEN EXISTS (SELECT 1 FROM [Loans].[Loan] l WHERE l.CustomerId = c.Id AND (l.LoanStatus = 7 OR l.DaysOverdue > 30)) THEN 3
                    WHEN EXISTS (SELECT 1 FROM [Loans].[Loan] l WHERE l.CustomerId = c.Id AND l.DaysOverdue > 0) THEN 2
                    WHEN (SELECT COUNT(*) FROM [Loans].[Loan] l INNER JOIN [Loans].[LoanPayment] p ON p.LoanId = l.Id WHERE l.CustomerId = c.Id AND p.Status = 1 AND p.PenaltyAmount > 0) >= 3 THEN 2
                    WHEN EXISTS (SELECT 1 FROM [Loans].[Loan] l WHERE l.CustomerId = c.Id) THEN 1
                    ELSE NULL END
                FROM [Customers].[Customer] c;

                UPDATE c
                SET CreatedOn = DATEADD(DAY, -15, CAST(first.FirstDisbursement AS datetime2))
                FROM [Customers].[Customer] c
                INNER JOIN (
                    SELECT CustomerId, MIN(DisbursementDate) AS FirstDisbursement
                    FROM [Loans].[Loan]
                    GROUP BY CustomerId
                ) first ON first.CustomerId = c.Id
                WHERE first.FirstDisbursement < CAST(c.CreatedOn AS date);");
        }

        private async Task SeedCreditScoresAsync(CancellationToken cancellationToken)
        {
            var sqlConn = _connection as SqlConnection
                ?? throw new InvalidOperationException("IDbConnection must be SqlConnection for SqlBulkCopy.");

            if (await sqlConn.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM [Customers].[CreditScore]") > 0)
                return;

            // A score is derived from loan history, so it can only be seeded together with the loans.
            if (_portfolio is null)
                return;

            var customers = (await sqlConn.QueryAsync<(Guid Id, decimal CreditLimit, bool IsBlacklisted)>(
                "SELECT Id, CreditLimit, IsBlacklisted FROM [Customers].[Customer]"
            )).Select(c => new Customer { Id = c.Id, CreditLimit = c.CreditLimit, IsBlacklisted = c.IsBlacklisted }).ToList();

            var paymentsByLoan = _portfolio.Payments.GroupBy(p => p.LoanId).ToDictionary(g => g.Key, g => g.ToList());
            var loansByCustomer = _portfolio.Loans.GroupBy(l => l.CustomerId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var customer in customers)
            {
                if (!loansByCustomer.TryGetValue(customer.Id, out var customerLoans))
                    continue;

                foreach (var loan in customerLoans)
                {
                    loan.Payments = paymentsByLoan.TryGetValue(loan.Id, out var loanPayments) ? loanPayments : new List<LoanPayment>();
                }

                customer.Loans = customerLoans;
            }

            var creditScores = FakeDbQueryCreditScores.Generate(customers);

            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(Guid));
            dt.Columns.Add("CustomerId", typeof(Guid));
            dt.Columns.Add("Score", typeof(int));
            dt.Columns.Add("LastCalculatedDate", typeof(DateTime));
            dt.Columns.Add("PaymentHistoryScore", typeof(decimal));
            dt.Columns.Add("CreditUtilizationScore", typeof(decimal));
            dt.Columns.Add("CreditAgeScore", typeof(decimal));
            dt.Columns.Add("DefaultHistoryScore", typeof(decimal));
            dt.Columns.Add("CreditRating", typeof(int));
            dt.Columns.Add("CreatedOn", typeof(DateTime));
            dt.Columns.Add("CreatedById", typeof(Guid));
            dt.Columns.Add("UpdatedOn", typeof(DateTime));
            dt.Columns.Add("UpdatedById", typeof(Guid));
            dt.Columns.Add("IsActive", typeof(bool));
            dt.Columns.Add("IsDeleted", typeof(bool));

            foreach (var score in creditScores)
            {
                dt.Rows.Add(
                    score.Id,
                    score.CustomerId,
                    score.Score,
                    score.LastCalculatedDate,
                    score.PaymentHistoryScore,
                    score.CreditUtilizationScore,
                    score.CreditAgeScore,
                    score.DefaultHistoryScore,
                    (int)score.CreditRating,
                    DateTime.UtcNow,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    true,
                    false
                );
            }

            await BulkInsertAsync(sqlConn, "[Customers].[CreditScore]", dt, cancellationToken);
        }

        private async Task BulkInsertPaymentsAsync(SqlConnection sqlConn, IEnumerable<LoanPayment> payments, CancellationToken cancellationToken)
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(Guid));
            dt.Columns.Add("LoanId", typeof(Guid));
            dt.Columns.Add("CurrencyId", typeof(Guid));
            dt.Columns.Add("PaymentDate", typeof(DateTime));
            dt.Columns.Add("PrincipalAmount", typeof(decimal));
            dt.Columns.Add("InterestAmount", typeof(decimal));
            dt.Columns.Add("PenaltyAmount", typeof(decimal));
            dt.Columns.Add("TotalPaymentAmount", typeof(decimal));
            dt.Columns.Add("PaymentMethod", typeof(int));
            dt.Columns.Add("ReferenceNumber", typeof(string));
            dt.Columns.Add("Status", typeof(int));
            dt.Columns.Add("CreatedOn", typeof(DateTime));
            dt.Columns.Add("CreatedById", typeof(Guid));
            dt.Columns.Add("UpdatedOn", typeof(DateTime));
            dt.Columns.Add("UpdatedById", typeof(Guid));
            dt.Columns.Add("IsActive", typeof(bool));
            dt.Columns.Add("IsDeleted", typeof(bool));

            foreach (var payment in payments)
            {
                dt.Rows.Add(
                    payment.Id,
                    payment.LoanId,
                    payment.CurrencyId,
                    payment.PaymentDate.ToDateTime(TimeOnly.MinValue),
                    payment.PrincipalAmount,
                    payment.InterestAmount,
                    payment.PenaltyAmount,
                    payment.TotalPaymentAmount,
                    (int)payment.PaymentMethod,
                    payment.ReferenceNumber ?? (object)DBNull.Value,
                    (int)payment.Status,
                    payment.CreatedOn,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    true,
                    false
                );
            }

            await BulkInsertAsync(sqlConn, "[Loans].[LoanPayment]", dt, cancellationToken);
        }

        private async Task SeedLoanDocumentsAsync(CancellationToken cancellationToken)
        {
            var sqlConn = _connection as SqlConnection
                ?? throw new InvalidOperationException("IDbConnection must be SqlConnection for SqlBulkCopy.");

            if (await sqlConn.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM [Loans].[LoanDocument]") > 0)
                return;

            var loans = (await sqlConn.QueryAsync<Guid>("SELECT Id FROM [Loans].[Loan]")).ToList();
            var loanEntities = loans.Select(id => new Loan { Id = id }).ToList();

            var documents = FakeDbQueryLoanDocuments.GenerateLoanDocuments(loanEntities);

            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(Guid));
            dt.Columns.Add("LoanId", typeof(Guid));
            dt.Columns.Add("DocumentType", typeof(int));
            dt.Columns.Add("FileName", typeof(string));
            dt.Columns.Add("FileUrl", typeof(string));
            dt.Columns.Add("UploadedDate", typeof(DateTime));
            dt.Columns.Add("FileSizeBytes", typeof(long));
            dt.Columns.Add("MimeType", typeof(int));
            dt.Columns.Add("CreatedOn", typeof(DateTime));
            dt.Columns.Add("CreatedById", typeof(Guid));
            dt.Columns.Add("UpdatedOn", typeof(DateTime));
            dt.Columns.Add("UpdatedById", typeof(Guid));
            dt.Columns.Add("IsActive", typeof(bool));
            dt.Columns.Add("IsDeleted", typeof(bool));

            foreach (var doc in documents)
            {
                dt.Rows.Add(
                    doc.Id,
                    doc.LoanId,
                    (int)doc.DocumentType,
                    doc.FileName,
                    doc.FileUrl,
                    doc.UploadedDate,
                    doc.FileSizeBytes,
                    (int)doc.MimeType,
                    DateTime.UtcNow,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    true,
                    false
                );
            }

            await BulkInsertAsync(sqlConn, "[Loans].[LoanDocument]", dt, cancellationToken);
        }

        private async Task BulkInsertAsync(SqlConnection sqlConn, string tableName, DataTable dt, CancellationToken cancellationToken)
        {
            var destinationColumns = await GetDestinationColumnsAsync(sqlConn, tableName, cancellationToken);

            var wasClosed = sqlConn.State != ConnectionState.Open;
            if (wasClosed) await sqlConn.OpenAsync(cancellationToken);

            using var tx = await sqlConn.BeginTransactionAsync(cancellationToken);
            try
            {
                using var bulk = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.CheckConstraints, (SqlTransaction)tx)
                {
                    DestinationTableName = tableName,
                    BulkCopyTimeout = 0,
                    BatchSize = 5_000
                };

                AddColumnMappings(bulk, dt, destinationColumns);
                await bulk.WriteToServerAsync(dt, cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (wasClosed) await sqlConn.CloseAsync();
            }
        }

        private async Task<HashSet<string>> GetDestinationColumnsAsync(SqlConnection sqlConn, string tableName, CancellationToken cancellationToken)
        {
            var wasClosed = sqlConn.State != ConnectionState.Open;
            try
            {
                if (wasClosed)
                    await sqlConn.OpenAsync(cancellationToken);

                using var cmd = sqlConn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(@table)";
                cmd.Parameters.AddWithValue("@table", tableName);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                var destination = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (await reader.ReadAsync(cancellationToken))
                {
                    destination.Add(reader.GetString(0));
                }

                return destination;
            }
            finally
            {
                if (wasClosed)
                    await sqlConn.CloseAsync();
            }
        }

        private static void AddColumnMappings(SqlBulkCopy bulk, DataTable sourceTable, HashSet<string> destinationColumns)
        {
            foreach (DataColumn col in sourceTable.Columns)
            {
                if (destinationColumns.Contains(col.ColumnName))
                {
                    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                }
            }
        }
    }
}
