using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentReversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                schema: "Loans",
                table: "LoanPayment",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedOn",
                schema: "Loans",
                table: "LoanPayment",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReversalReason",
                schema: "Loans",
                table: "LoanPayment");

            migrationBuilder.DropColumn(
                name: "ReversedOn",
                schema: "Loans",
                table: "LoanPayment");
        }
    }
}
