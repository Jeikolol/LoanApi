using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations.Migrations
{
    /// <inheritdoc />
    public partial class SingleActiveDelinquencyPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DelinquencyPolicy_IsActive",
                schema: "Loans",
                table: "DelinquencyPolicy");

            migrationBuilder.CreateIndex(
                name: "UX_DelinquencyPolicy_OneActive",
                schema: "Loans",
                table: "DelinquencyPolicy",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_DelinquencyPolicy_OneActive",
                schema: "Loans",
                table: "DelinquencyPolicy");

            migrationBuilder.CreateIndex(
                name: "IX_DelinquencyPolicy_IsActive",
                schema: "Loans",
                table: "DelinquencyPolicy",
                column: "IsActive");
        }
    }
}
