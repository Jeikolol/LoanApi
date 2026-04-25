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

        public async Task SeedDatabaseAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            try
            {
                // Seed static data first
                await SeedCountriesAsync(dbContext, cancellationToken);
                await SeedCurrenciesAsync(dbContext, cancellationToken);
                await SeedRolesAsync(dbContext, cancellationToken);
                await SeedBranchesAsync(dbContext, cancellationToken);
                await SeedIdentificationTypesAsync(dbContext, cancellationToken);
                await SeedDelinquencyPoliciesAsync(dbContext, cancellationToken);
                await SeedAdminUserAsync(dbContext, cancellationToken);

                // Then seed dependent entities using actual IDs
                await SeedCustomersAsync(cancellationToken);
                await SeedLoansAsync(cancellationToken);
                await SeedCreditScoresAsync(cancellationToken);
                await SeedLoanPaymentsAsync(cancellationToken);
                await SeedLoanDocumentsAsync(cancellationToken);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task SeedRolesAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            if (await dbContext.Roles.AnyAsync(cancellationToken))
                return;

            try
            {
                var roles = new List<Role>
                {
                    new Role
                    {
                        Id = SystemConstants.DefaultRoleId,
                        Code = "ADMIN",
                        Name = "Administrator",
                        Description = "System Administrator with full access",
                    },
                    new Role
                    {
                        Id = Guid.NewGuid(),
                        Code = "USER",
                        Name = "Standard User",
                        Description = "Standard user with limited access",
                    }
                };

                await dbContext.Roles.AddRangeAsync(roles);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {

                throw ex;
            }
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
                    Name = "Main Branch",
                    Address = "123 Main St, Cityville",
                },
                new Branch
                {
                    Id = Guid.NewGuid(),
                    Code = "02",
                    Name = "Secondary Branch",
                    Address = "456 Side St, Townsville",
                }
            };

            await dbContext.Branches.AddRangeAsync(branches);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedIdentificationTypesAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
            if (await dbContext.IdentificationTypes.AnyAsync(cancellationToken))
                return;

            var identificationTypes = new List<IdentificationType>
            {
                new IdentificationType
                {
                    Id = SystemConstants.CedulaIdentificationTypeId,
                    Code = "CEDULA",
                    Name = "Cédula de Identidad",
                    Description = "Dominican National ID"
                },
                new IdentificationType
                {
                    Id = SystemConstants.PassportIdentificationTypeId,
                    Code = "PASSPORT",
                    Name = "Pasaporte",
                    Description = "International Passport"
                }
            };

            await dbContext.IdentificationTypes.AddRangeAsync(identificationTypes);
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
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@admin.com",
                UserName = "admin",
                RoleId = SystemConstants.DefaultRoleId,
                BranchId = SystemConstants.DefaultBranchId,
                CreatedOn = DateTime.UtcNow,
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

                var customers = FakeDbQueryCustomers.Customers;

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
                        DateTime.UtcNow,
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

            // Query actual customer and branch IDs from database
            var customerIds = (await sqlConn.QueryAsync<Guid>(
                "SELECT Id FROM [Customers].[Customer]"
            )).ToList();

            var branchIds = (await sqlConn.QueryAsync<Guid>(
                "SELECT Id FROM [Admin].[Branch]"
            )).ToList();

            if (customerIds.Count == 0 || branchIds.Count == 0)
                throw new InvalidOperationException("Seed customers and branches first.");

            var loans = FakeDbQueryLoans.GenerateLoans(
                customerIds.Select(id => new Customer { Id = id }).AsEnumerable(),
                branchIds.Select(id => new Branch { Id = id }).AsEnumerable()
            );

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
                    DateTime.UtcNow,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    true,
                    false
                );
            }

            await BulkInsertAsync(sqlConn, "[Loans].[Loan]", dt, cancellationToken);
        }

        private async Task SeedCreditScoresAsync(CancellationToken cancellationToken)
        {
            var sqlConn = _connection as SqlConnection
                ?? throw new InvalidOperationException("IDbConnection must be SqlConnection for SqlBulkCopy.");

            if (await sqlConn.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM [Customers].[CreditScore]") > 0)
                return;

            var customers = (await sqlConn.QueryAsync<Guid>("SELECT Id FROM [Customers].[Customer]")).ToList();
            var customerEntities = customers.Select(id => new Customer { Id = id }).ToList();

            var creditScores = FakeDbQueryCreditScores.GenerateCreditScores(customerEntities);

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

        private async Task SeedLoanPaymentsAsync(CancellationToken cancellationToken)
        {
            var sqlConn = _connection as SqlConnection
                ?? throw new InvalidOperationException("IDbConnection must be SqlConnection for SqlBulkCopy.");

            if (await sqlConn.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM [Loans].[LoanPayment]") > 0)
                return;

            var loans = (await sqlConn.QueryAsync<(Guid Id, Guid CurrencyId)>("SELECT Id, CurrencyId FROM [Loans].[Loan]")).ToList();
            var loanEntities = loans.Select(l => new Loan { Id = l.Id, CurrencyId = l.CurrencyId }).ToList();

            var payments = FakeDbQueryLoanPayments.GenerateLoanPayments(loanEntities);

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
                    DateTime.UtcNow,
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
