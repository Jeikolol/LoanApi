using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                schema: "Loans",
                table: "LoanDocument",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StorageProvider",
                schema: "Loans",
                table: "LoanDocument",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StorageSetting",
                schema: "Admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AzureEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AzureAccountName = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    AzureContainer = table.Column<string>(type: "nvarchar(63)", maxLength: 63, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageSetting_Employee_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StorageSetting_Employee_DeletedById",
                        column: x => x.DeletedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StorageSetting_Employee_UpdatedById",
                        column: x => x.UpdatedById,
                        principalSchema: "Admin",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorageSetting_CreatedById",
                schema: "Admin",
                table: "StorageSetting",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StorageSetting_CreatedOn",
                schema: "Admin",
                table: "StorageSetting",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_StorageSetting_DeletedById",
                schema: "Admin",
                table: "StorageSetting",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StorageSetting_IsActive",
                schema: "Admin",
                table: "StorageSetting",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_StorageSetting_IsDeleted",
                schema: "Admin",
                table: "StorageSetting",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StorageSetting_UpdatedById",
                schema: "Admin",
                table: "StorageSetting",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorageSetting",
                schema: "Admin");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                schema: "Loans",
                table: "LoanDocument");

            migrationBuilder.DropColumn(
                name: "StorageProvider",
                schema: "Loans",
                table: "LoanDocument");
        }
    }
}
