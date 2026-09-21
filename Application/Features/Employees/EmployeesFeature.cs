using Application.Common.Filters;
using Application.Exceptions;
using Application.Features.Customers;
using Application.Interfaces;
using Application.Models.Responses;
using Dapper;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace Application.Features.Employees
{
    // ------------------------------------------------------------------ responses

    public class EmployeeResponse
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime? LastLoginOn { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    /// <summary>The only time a temporary password is ever visible: it is stored hashed and cannot be read again.</summary>
    public class EmployeeCredentialsResponse
    {
        public EmployeeResponse Employee { get; set; } = new();
        public string TemporaryPassword { get; set; } = string.Empty;
    }

    public class OptionItem
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class EmployeeOptionsResponse
    {
        public List<OptionItem> Roles { get; set; } = [];
        public List<OptionItem> Branches { get; set; } = [];
    }

    // ------------------------------------------------------------------ requests

    public class GetEmployeesQuery : PaginationFilter, IRequest<PaginatedResponse<EmployeeResponse>>
    {
        public GetEmployeesQuery() { }

        public GetEmployeesQuery(int pageNumber, int pageSize) : base(pageNumber, pageSize) { }
    }

    public record GetEmployeeOptionsQuery : IRequest<EmployeeOptionsResponse>;

    public class CreateEmployeeCommand : IRequest<EmployeeCredentialsResponse>
    {
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
        public Guid BranchId { get; set; }
    }

    public class UpdateEmployeeCommand : IRequest<EmployeeResponse>
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
        public Guid BranchId { get; set; }
    }

    public class SetEmployeeActiveCommand : IRequest<EmployeeResponse>
    {
        public Guid Id { get; set; }
        public bool Active { get; set; }
    }

    public class ResetEmployeePasswordCommand : IRequest<EmployeeCredentialsResponse>
    {
        public Guid Id { get; set; }
    }

    // ------------------------------------------------------------------ shared

    internal static class EmployeeData
    {
        public const string SelectSql = @"
            SELECT e.Id, e.UserName, e.FirstName, e.LastName, e.Email, e.RoleId, r.Code AS RoleCode, r.Name AS RoleName,
                   e.BranchId, b.Name AS BranchName, e.IsActive, e.MustChangePassword, e.LastLoginOn, e.CreatedOn
            FROM [Admin].[Employee] e
            INNER JOIN [Admin].[Role] r ON r.Id = e.RoleId
            INNER JOIN [Admin].[Branch] b ON b.Id = e.BranchId";

        public static async Task<EmployeeResponse> GetAsync(IDbConnection connection, Guid id) =>
            await connection.QueryFirstOrDefaultAsync<EmployeeResponse>($"{SelectSql} WHERE e.Id = @Id AND e.IsDeleted = 0", new { Id = id })
            ?? throw new NotFoundException($"Employee {id} not found");

        /// <summary>The admin role must always have at least one active member, or nobody could manage the rest.</summary>
        public const string AnotherActiveAdminSql = @"
            EXISTS (SELECT 1 FROM [Admin].[Employee] other
                    WHERE other.Id <> @Id AND other.IsActive = 1 AND other.IsDeleted = 0
                      AND other.RoleId IN (SELECT Id FROM [Admin].[Role] WHERE Code = @AdminCode))";

        public static void ValidateProfile(List<string> errors, string firstName, string lastName, string email, Guid roleId, Guid branchId)
        {
            CustomerFieldRules.Required(errors, "FirstName", firstName, 100);
            CustomerFieldRules.Required(errors, "LastName", lastName, 100);
            CustomerFieldRules.Required(errors, "Email", email, 150);
            if (email.Length > 0) CustomerFieldRules.CheckEmail(errors, email);
            if (roleId == Guid.Empty) errors.Add("RoleId is required.");
            if (branchId == Guid.Empty) errors.Add("BranchId is required.");
        }

        public static async Task<string?> RoleCodeAsync(IDbConnection connection, Guid roleId) =>
            await connection.QueryFirstOrDefaultAsync<string>("SELECT Code FROM [Admin].[Role] WHERE Id = @Id AND IsDeleted = 0", new { Id = roleId });

        public static async Task EnsureRoleAndBranchExistAsync(IDbConnection connection, Guid roleId, Guid branchId)
        {
            List<string> errors = [];
            if (await RoleCodeAsync(connection, roleId) is null) errors.Add("The role does not exist.");
            if (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [Admin].[Branch] WHERE Id = @Id AND IsDeleted = 0", new { Id = branchId }) == 0)
            {
                errors.Add("The branch does not exist.");
            }

            if (errors.Count > 0) throw new ValidationException("The employee is not valid.", errors);
        }
    }

    // ------------------------------------------------------------------ queries

    public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, PaginatedResponse<EmployeeResponse>>
    {
        private readonly IDbConnection _connection;

        public GetEmployeesQueryHandler(IDbConnection connection) => _connection = connection;

        public async Task<PaginatedResponse<EmployeeResponse>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
        {
            using SqlMapper.GridReader grid = await _connection.QueryMultipleAsync(
                $@"{EmployeeData.SelectSql}
                   WHERE e.IsDeleted = 0
                   ORDER BY e.IsActive DESC, e.UserName
                   OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;

                   SELECT COUNT(*) FROM [Admin].[Employee] WHERE IsDeleted = 0;",
                new { Skip = request.GetSkip(), Take = request.PageSize });

            List<EmployeeResponse> employees = (await grid.ReadAsync<EmployeeResponse>()).ToList();
            int total = await grid.ReadFirstAsync<int>();

            return new PaginatedResponse<EmployeeResponse>(employees, request.PageNumber, request.PageSize, total);
        }
    }

    public class GetEmployeeOptionsQueryHandler : IRequestHandler<GetEmployeeOptionsQuery, EmployeeOptionsResponse>
    {
        private readonly IDbConnection _connection;

        public GetEmployeeOptionsQueryHandler(IDbConnection connection) => _connection = connection;

        public async Task<EmployeeOptionsResponse> Handle(GetEmployeeOptionsQuery request, CancellationToken cancellationToken)
        {
            List<OptionItem> roles = (await _connection.QueryAsync<OptionItem>(
                "SELECT Id, Code, Name FROM [Admin].[Role] WHERE IsDeleted = 0 ORDER BY Name")).ToList();
            List<OptionItem> branches = (await _connection.QueryAsync<OptionItem>(
                "SELECT Id, Code, Name FROM [Admin].[Branch] WHERE IsDeleted = 0 ORDER BY Name")).ToList();

            return new EmployeeOptionsResponse { Roles = roles, Branches = branches };
        }
    }

    // ------------------------------------------------------------------ commands

    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, EmployeeCredentialsResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<CreateEmployeeCommandHandler> _logger;

        public CreateEmployeeCommandHandler(IDbConnection connection, ICurrentUser currentUser, ILogger<CreateEmployeeCommandHandler> logger)
        {
            _connection = connection;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<EmployeeCredentialsResponse> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            string userName = EmployeeRules.NormalizeUserName(request.UserName);
            string firstName = request.FirstName.Trim();
            string lastName = request.LastName.Trim();
            string email = request.Email.Trim().ToLowerInvariant();

            List<string> errors = [];
            if (!EmployeeRules.IsValidUserName(userName))
            {
                errors.Add("The user name must be 3 to 50 characters: lower-case letters, digits, dots, dashes or underscores.");
            }

            EmployeeData.ValidateProfile(errors, firstName, lastName, email, request.RoleId, request.BranchId);
            if (errors.Count > 0) throw new ValidationException("The employee is not valid.", errors);

            await EmployeeData.EnsureRoleAndBranchExistAsync(_connection, request.RoleId, request.BranchId);
            await EnsureUnique(userName, email);

            string temporaryPassword = EmployeeRules.GenerateTemporaryPassword();
            Employee employee = new() { UserName = userName };
            string hash = new PasswordHasher<Employee>().HashPassword(employee, temporaryPassword);
            Guid id = Guid.NewGuid();

            try
            {
                await _connection.ExecuteAsync(
                    @"INSERT INTO [Admin].[Employee]
                        (Id, UserName, PasswordHash, PasswordSalt, FirstName, LastName, Email, RoleId, BranchId,
                         MustChangePassword, CreatedOn, CreatedById, IsActive, IsDeleted)
                      VALUES
                        (@Id, @UserName, @Hash, '', @FirstName, @LastName, @Email, @RoleId, @BranchId,
                         1, @Now, @CreatedById, 1, 0)",
                    new { Id = id, UserName = userName, Hash = hash, FirstName = firstName, LastName = lastName, Email = email, request.RoleId, request.BranchId, Now = DateTime.UtcNow, CreatedById = _currentUser.UserId });
            }
            catch (DbException ex) when (ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                // Two requests passed the check at the same time; the unique indexes decide.
                throw new ConflictException("An employee with this user name or email already exists.");
            }

            // The password itself is never logged or audited: only the response carries it, once.
            _logger.LogInformation("Employee {EmployeeId} ({UserName}) created", id, userName);

            return new EmployeeCredentialsResponse { Employee = await EmployeeData.GetAsync(_connection, id), TemporaryPassword = temporaryPassword };
        }

        private async Task EnsureUnique(string userName, string email)
        {
            // Soft-deleted rows count too: the unique indexes include them.
            List<(string UserName, string Email)> matches = (await _connection.QueryAsync<(string, string)>(
                "SELECT UserName, Email FROM [Admin].[Employee] WHERE UserName = @UserName OR Email = @Email",
                new { UserName = userName, Email = email })).ToList();

            List<string> conflicts = [];
            if (matches.Any(m => string.Equals(m.UserName, userName, StringComparison.OrdinalIgnoreCase))) conflicts.Add("An employee with this user name already exists.");
            if (matches.Any(m => string.Equals(m.Email, email, StringComparison.OrdinalIgnoreCase))) conflicts.Add("An employee with this email already exists.");
            if (conflicts.Count > 0) throw new ConflictException(string.Join(" ", conflicts));
        }
    }

    public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, EmployeeResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ICurrentUser _currentUser;

        public UpdateEmployeeCommandHandler(IDbConnection connection, ICurrentUser currentUser)
        {
            _connection = connection;
            _currentUser = currentUser;
        }

        public async Task<EmployeeResponse> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            string firstName = request.FirstName.Trim();
            string lastName = request.LastName.Trim();
            string email = request.Email.Trim().ToLowerInvariant();

            List<string> errors = [];
            EmployeeData.ValidateProfile(errors, firstName, lastName, email, request.RoleId, request.BranchId);
            if (errors.Count > 0) throw new ValidationException("The employee is not valid.", errors);

            Guid? currentRoleId = await _connection.QueryFirstOrDefaultAsync<Guid?>(
                "SELECT RoleId FROM [Admin].[Employee] WHERE Id = @Id AND IsDeleted = 0", new { request.Id });
            if (currentRoleId is null) throw new NotFoundException($"Employee {request.Id} not found");

            await EmployeeData.EnsureRoleAndBranchExistAsync(_connection, request.RoleId, request.BranchId);

            bool roleChanged = currentRoleId != request.RoleId;
            if (roleChanged && request.Id == _currentUser.UserId)
            {
                throw new ConflictException("You cannot change your own role.");
            }

            int sameEmail = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM [Admin].[Employee] WHERE Email = @Email AND Id <> @Id", new { Email = email, request.Id });
            if (sameEmail > 0) throw new ConflictException("An employee with this email already exists.");

            bool newRoleIsAdmin = await EmployeeData.RoleCodeAsync(_connection, request.RoleId) == SystemRoles.Admin;

            int updated;
            try
            {
                updated = await _connection.ExecuteAsync(
                    $@"UPDATE [Admin].[Employee]
                       SET FirstName = @FirstName, LastName = @LastName, Email = @Email, RoleId = @RoleId, BranchId = @BranchId,
                           TokensValidFrom = CASE WHEN @RoleChanged = 1 THEN @ValidFrom ELSE TokensValidFrom END,
                           UpdatedOn = @Now
                       WHERE Id = @Id AND IsDeleted = 0
                         AND (@RoleChanged = 0 OR @NewRoleIsAdmin = 1
                              OR RoleId NOT IN (SELECT Id FROM [Admin].[Role] WHERE Code = @AdminCode)
                              OR {EmployeeData.AnotherActiveAdminSql})",
                    new
                    {
                        request.Id, FirstName = firstName, LastName = lastName, Email = email, request.RoleId, request.BranchId,
                        RoleChanged = roleChanged ? 1 : 0, NewRoleIsAdmin = newRoleIsAdmin ? 1 : 0, AdminCode = SystemRoles.Admin,
                        ValidFrom = EmployeeRules.TokensValidFrom(DateTime.UtcNow), Now = DateTime.UtcNow,
                    });
            }
            catch (DbException ex) when (ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException("An employee with this email already exists.");
            }

            if (updated != 1) throw new ConflictException("The last active administrator cannot lose the administrator role.");

            return await EmployeeData.GetAsync(_connection, request.Id);
        }
    }

    public class SetEmployeeActiveCommandHandler : IRequestHandler<SetEmployeeActiveCommand, EmployeeResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ICurrentUser _currentUser;

        public SetEmployeeActiveCommandHandler(IDbConnection connection, ICurrentUser currentUser)
        {
            _connection = connection;
            _currentUser = currentUser;
        }

        public async Task<EmployeeResponse> Handle(SetEmployeeActiveCommand request, CancellationToken cancellationToken)
        {
            int exists = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM [Admin].[Employee] WHERE Id = @Id AND IsDeleted = 0", new { request.Id });
            if (exists == 0) throw new NotFoundException($"Employee {request.Id} not found");

            if (request.Active)
            {
                await _connection.ExecuteAsync(
                    "UPDATE [Admin].[Employee] SET IsActive = 1, UpdatedOn = @Now WHERE Id = @Id AND IsDeleted = 0",
                    new { request.Id, Now = DateTime.UtcNow });
            }
            else
            {
                if (request.Id == _currentUser.UserId) throw new ConflictException("You cannot deactivate your own account.");

                int updated = await _connection.ExecuteAsync(
                    $@"UPDATE [Admin].[Employee]
                       SET IsActive = 0, TokensValidFrom = @ValidFrom, UpdatedOn = @Now
                       WHERE Id = @Id AND IsDeleted = 0
                         AND (RoleId NOT IN (SELECT Id FROM [Admin].[Role] WHERE Code = @AdminCode)
                              OR {EmployeeData.AnotherActiveAdminSql})",
                    new { request.Id, AdminCode = SystemRoles.Admin, ValidFrom = EmployeeRules.TokensValidFrom(DateTime.UtcNow), Now = DateTime.UtcNow });

                if (updated != 1) throw new ConflictException("The last active administrator cannot be deactivated.");
            }

            return await EmployeeData.GetAsync(_connection, request.Id);
        }
    }

    public class ResetEmployeePasswordCommandHandler : IRequestHandler<ResetEmployeePasswordCommand, EmployeeCredentialsResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ICurrentUser _currentUser;

        public ResetEmployeePasswordCommandHandler(IDbConnection connection, ICurrentUser currentUser)
        {
            _connection = connection;
            _currentUser = currentUser;
        }

        public async Task<EmployeeCredentialsResponse> Handle(ResetEmployeePasswordCommand request, CancellationToken cancellationToken)
        {
            string? userName = await _connection.QueryFirstOrDefaultAsync<string>(
                "SELECT UserName FROM [Admin].[Employee] WHERE Id = @Id AND IsDeleted = 0", new { request.Id });
            if (userName is null) throw new NotFoundException($"Employee {request.Id} not found");

            if (request.Id == _currentUser.UserId)
            {
                throw new ConflictException("Use the change password option for your own account.");
            }

            string temporaryPassword = EmployeeRules.GenerateTemporaryPassword();
            string hash = new PasswordHasher<Employee>().HashPassword(new Employee { UserName = userName }, temporaryPassword);

            // Any session the old password opened stops working, and the new one must be replaced at first sign-in.
            await _connection.ExecuteAsync(
                @"UPDATE [Admin].[Employee]
                  SET PasswordHash = @Hash, MustChangePassword = 1, TokensValidFrom = @ValidFrom, UpdatedOn = @Now
                  WHERE Id = @Id AND IsDeleted = 0",
                new { request.Id, Hash = hash, ValidFrom = EmployeeRules.TokensValidFrom(DateTime.UtcNow), Now = DateTime.UtcNow });

            return new EmployeeCredentialsResponse { Employee = await EmployeeData.GetAsync(_connection, request.Id), TemporaryPassword = temporaryPassword };
        }
    }
}
