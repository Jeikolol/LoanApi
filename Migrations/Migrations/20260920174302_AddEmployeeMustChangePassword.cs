using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeMustChangePassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Admin",
                table: "Role",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Loans",
                table: "LoanPayment",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Loans",
                table: "LoanDocument",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Loans",
                table: "Loan",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Admin",
                table: "IdentificationType",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Admin",
                table: "Employee",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                schema: "Admin",
                table: "Employee",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Loans",
                table: "DelinquencyPolicy",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Customers",
                table: "Customer",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Reference",
                table: "Currency",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Customers",
                table: "CreditScore",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Reference",
                table: "Country",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Admin",
                table: "Branch",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.CreateIndex(
                name: "IX_Role_IsActive",
                schema: "Admin",
                table: "Role",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayment_IsActive",
                schema: "Loans",
                table: "LoanPayment",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDocument_IsActive",
                schema: "Loans",
                table: "LoanDocument",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Loan_IsActive",
                schema: "Loans",
                table: "Loan",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_IdentificationType_IsActive",
                schema: "Admin",
                table: "IdentificationType",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_IsActive",
                schema: "Admin",
                table: "Employee",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DelinquencyPolicy_IsActive",
                schema: "Loans",
                table: "DelinquencyPolicy",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_IsActive",
                schema: "Customers",
                table: "Customer",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Currency_IsActive",
                schema: "Reference",
                table: "Currency",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CreditScore_IsActive",
                schema: "Customers",
                table: "CreditScore",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Country_IsActive",
                schema: "Reference",
                table: "Country",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_IsActive",
                schema: "Admin",
                table: "Branch",
                column: "IsActive");

            migrationBuilder.Sql("UPDATE [Admin].[Employee] SET MustChangePassword = 1 WHERE UserName = 'admin'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Role_IsActive",
                schema: "Admin",
                table: "Role");

            migrationBuilder.DropIndex(
                name: "IX_LoanPayment_IsActive",
                schema: "Loans",
                table: "LoanPayment");

            migrationBuilder.DropIndex(
                name: "IX_LoanDocument_IsActive",
                schema: "Loans",
                table: "LoanDocument");

            migrationBuilder.DropIndex(
                name: "IX_Loan_IsActive",
                schema: "Loans",
                table: "Loan");

            migrationBuilder.DropIndex(
                name: "IX_IdentificationType_IsActive",
                schema: "Admin",
                table: "IdentificationType");

            migrationBuilder.DropIndex(
                name: "IX_Employee_IsActive",
                schema: "Admin",
                table: "Employee");

            migrationBuilder.DropIndex(
                name: "IX_DelinquencyPolicy_IsActive",
                schema: "Loans",
                table: "DelinquencyPolicy");

            migrationBuilder.DropIndex(
                name: "IX_Customer_IsActive",
                schema: "Customers",
                table: "Customer");

            migrationBuilder.DropIndex(
                name: "IX_Currency_IsActive",
                schema: "Reference",
                table: "Currency");

            migrationBuilder.DropIndex(
                name: "IX_CreditScore_IsActive",
                schema: "Customers",
                table: "CreditScore");

            migrationBuilder.DropIndex(
                name: "IX_Country_IsActive",
                schema: "Reference",
                table: "Country");

            migrationBuilder.DropIndex(
                name: "IX_Branch_IsActive",
                schema: "Admin",
                table: "Branch");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                schema: "Admin",
                table: "Employee");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Admin",
                table: "Role",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Loans",
                table: "LoanPayment",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Loans",
                table: "LoanDocument",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Loans",
                table: "Loan",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Admin",
                table: "IdentificationType",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Admin",
                table: "Employee",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Loans",
                table: "DelinquencyPolicy",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Customers",
                table: "Customer",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Reference",
                table: "Currency",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Customers",
                table: "CreditScore",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Reference",
                table: "Country",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "Admin",
                table: "Branch",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);
        }
    }
}
