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
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Branch",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                name: "Customer",
                schema: "dbo",
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
                    Country = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                schema: "dbo",
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
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                        principalSchema: "dbo",
                        principalTable: "Branch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Employee_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "dbo",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Employee_DeletedById",
                        column: x => x.DeletedById,
                        principalSchema: "dbo",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Employee_UpdatedById",
                        column: x => x.UpdatedById,
                        principalSchema: "dbo",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentificationType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mask = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentificationType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentificationType_Employee_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "dbo",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IdentificationType_Employee_DeletedById",
                        column: x => x.DeletedById,
                        principalSchema: "dbo",
                        principalTable: "Employee",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IdentificationType_Employee_UpdatedById",
                        column: x => x.UpdatedById,
                        principalSchema: "dbo",
                        principalTable: "Employee",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Loan",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TermMonths = table.Column<int>(type: "int", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LoanStatus = table.Column<int>(type: "int", nullable: false),
                    DisbursementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaturityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                        principalSchema: "dbo",
                        principalTable: "Branch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loan_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "dbo",
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loan_Customer_CustomerId1",
                        column: x => x.CustomerId1,
                        principalSchema: "dbo",
                        principalTable: "Customer",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Loan_Employee_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "dbo",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loan_Employee_DeletedById",
                        column: x => x.DeletedById,
                        principalSchema: "dbo",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loan_Employee_UpdatedById",
                        column: x => x.UpdatedById,
                        principalSchema: "dbo",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                        principalSchema: "dbo",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Role_Employee_DeletedById",
                        column: x => x.DeletedById,
                        principalSchema: "dbo",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Role_Employee_UpdatedById",
                        column: x => x.UpdatedById,
                        principalSchema: "dbo",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branch_Code",
                schema: "dbo",
                table: "Branch",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_CreatedById",
                schema: "dbo",
                table: "Branch",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_CreatedOn",
                schema: "dbo",
                table: "Branch",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_DeletedById",
                schema: "dbo",
                table: "Branch",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_IsDeleted",
                schema: "dbo",
                table: "Branch",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_UpdatedById",
                schema: "dbo",
                table: "Branch",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CreatedById",
                schema: "dbo",
                table: "Customer",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CreatedOn",
                schema: "dbo",
                table: "Customer",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_DeletedById",
                schema: "dbo",
                table: "Customer",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Email",
                schema: "dbo",
                table: "Customer",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Identification",
                schema: "dbo",
                table: "Customer",
                column: "Identification",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_IdentificationTypeId",
                schema: "dbo",
                table: "Customer",
                column: "IdentificationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_IsDeleted",
                schema: "dbo",
                table: "Customer",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Phone",
                schema: "dbo",
                table: "Customer",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_UpdatedById",
                schema: "dbo",
                table: "Customer",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_BranchId",
                schema: "dbo",
                table: "Employee",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_CreatedById",
                schema: "dbo",
                table: "Employee",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_CreatedOn",
                schema: "dbo",
                table: "Employee",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_DeletedById",
                schema: "dbo",
                table: "Employee",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Email",
                schema: "dbo",
                table: "Employee",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_IsDeleted",
                schema: "dbo",
                table: "Employee",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_RoleId",
                schema: "dbo",
                table: "Employee",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_UpdatedById",
                schema: "dbo",
                table: "Employee",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_UserName",
                schema: "dbo",
                table: "Employee",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentificationType_CreatedById",
                table: "IdentificationType",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_IdentificationType_DeletedById",
                table: "IdentificationType",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_IdentificationType_UpdatedById",
                table: "IdentificationType",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_BranchId",
                schema: "dbo",
                table: "Loan",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_CreatedById",
                schema: "dbo",
                table: "Loan",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_CreatedOn",
                schema: "dbo",
                table: "Loan",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_CustomerId",
                schema: "dbo",
                table: "Loan",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_CustomerId1",
                schema: "dbo",
                table: "Loan",
                column: "CustomerId1");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_DeletedById",
                schema: "dbo",
                table: "Loan",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_IsDeleted",
                schema: "dbo",
                table: "Loan",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_UpdatedById",
                schema: "dbo",
                table: "Loan",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Code",
                schema: "dbo",
                table: "Role",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Role_CreatedById",
                schema: "dbo",
                table: "Role",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Role_CreatedOn",
                schema: "dbo",
                table: "Role",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Role_DeletedById",
                schema: "dbo",
                table: "Role",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Role_IsDeleted",
                schema: "dbo",
                table: "Role",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Role_UpdatedById",
                schema: "dbo",
                table: "Role",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Branch_Employee_CreatedById",
                schema: "dbo",
                table: "Branch",
                column: "CreatedById",
                principalSchema: "dbo",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Branch_Employee_DeletedById",
                schema: "dbo",
                table: "Branch",
                column: "DeletedById",
                principalSchema: "dbo",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Branch_Employee_UpdatedById",
                schema: "dbo",
                table: "Branch",
                column: "UpdatedById",
                principalSchema: "dbo",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_Employee_CreatedById",
                schema: "dbo",
                table: "Customer",
                column: "CreatedById",
                principalSchema: "dbo",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_Employee_DeletedById",
                schema: "dbo",
                table: "Customer",
                column: "DeletedById",
                principalSchema: "dbo",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_Employee_UpdatedById",
                schema: "dbo",
                table: "Customer",
                column: "UpdatedById",
                principalSchema: "dbo",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_IdentificationType_IdentificationTypeId",
                schema: "dbo",
                table: "Customer",
                column: "IdentificationTypeId",
                principalTable: "IdentificationType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employee_Role_RoleId",
                schema: "dbo",
                table: "Employee",
                column: "RoleId",
                principalSchema: "dbo",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branch_Employee_CreatedById",
                schema: "dbo",
                table: "Branch");

            migrationBuilder.DropForeignKey(
                name: "FK_Branch_Employee_DeletedById",
                schema: "dbo",
                table: "Branch");

            migrationBuilder.DropForeignKey(
                name: "FK_Branch_Employee_UpdatedById",
                schema: "dbo",
                table: "Branch");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Employee_CreatedById",
                schema: "dbo",
                table: "Role");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Employee_DeletedById",
                schema: "dbo",
                table: "Role");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Employee_UpdatedById",
                schema: "dbo",
                table: "Role");

            migrationBuilder.DropTable(
                name: "Loan",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Customer",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "IdentificationType");

            migrationBuilder.DropTable(
                name: "Employee",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Branch",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "dbo");
        }
    }
}
