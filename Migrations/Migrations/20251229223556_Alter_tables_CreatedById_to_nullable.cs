using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Alter_tables_CreatedById_to_nullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IdentificationType_Employee_CreatedById",
                table: "IdentificationType");

            migrationBuilder.DropForeignKey(
                name: "FK_Loan_Customer_CustomerId1",
                schema: "dbo",
                table: "Loan");

            migrationBuilder.DropIndex(
                name: "IX_Loan_CustomerId1",
                schema: "dbo",
                table: "Loan");

            migrationBuilder.DropColumn(
                name: "CustomerId1",
                schema: "dbo",
                table: "Loan");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                schema: "dbo",
                table: "Role",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                schema: "dbo",
                table: "Loan",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                table: "IdentificationType",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                schema: "dbo",
                table: "Employee",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                schema: "dbo",
                table: "Customer",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                schema: "dbo",
                table: "Branch",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_IdentificationType_Employee_CreatedById",
                table: "IdentificationType",
                column: "CreatedById",
                principalSchema: "dbo",
                principalTable: "Employee",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IdentificationType_Employee_CreatedById",
                table: "IdentificationType");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                schema: "dbo",
                table: "Role",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                schema: "dbo",
                table: "Loan",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId1",
                schema: "dbo",
                table: "Loan",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                table: "IdentificationType",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                schema: "dbo",
                table: "Employee",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                schema: "dbo",
                table: "Customer",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                schema: "dbo",
                table: "Branch",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Loan_CustomerId1",
                schema: "dbo",
                table: "Loan",
                column: "CustomerId1");

            migrationBuilder.AddForeignKey(
                name: "FK_IdentificationType_Employee_CreatedById",
                table: "IdentificationType",
                column: "CreatedById",
                principalSchema: "dbo",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Loan_Customer_CustomerId1",
                schema: "dbo",
                table: "Loan",
                column: "CustomerId1",
                principalSchema: "dbo",
                principalTable: "Customer",
                principalColumn: "Id");
        }
    }
}
