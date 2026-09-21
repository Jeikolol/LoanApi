using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryPhoneAndCanonicalPhones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneCode",
                schema: "Reference",
                table: "Country",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneMask",
                schema: "Reference",
                table: "Country",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            // Dial codes and national masks of the countries the system already knows.
            migrationBuilder.Sql(@"
                UPDATE [Reference].[Country] SET PhoneCode = '1',  PhoneMask = '999-999-9999' WHERE CodeIso2 IN ('DO', 'US', 'PR');
                UPDATE [Reference].[Country] SET PhoneCode = '52', PhoneMask = '99 9999 9999' WHERE CodeIso2 = 'MX';
                UPDATE [Reference].[Country] SET PhoneCode = '57', PhoneMask = '999 999 9999' WHERE CodeIso2 = 'CO';
                UPDATE [Reference].[Country] SET PhoneCode = '34', PhoneMask = '999 999 999'  WHERE CodeIso2 = 'ES';");

            // Every stored phone was a Dominican national number (809-555-0100). From now on they are kept in the
            // canonical international form (+18095550100), so duplicates are detected whatever the spelling.
            migrationBuilder.Sql(@"
                UPDATE [Customers].[Customer]
                SET Phone = '+1' + REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Phone, '-', ''), ' ', ''), '(', ''), ')', ''), '.', '')
                WHERE Phone IS NOT NULL AND Phone NOT LIKE '+%' AND LEN(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Phone, '-', ''), ' ', ''), '(', ''), ')', ''), '.', '')) = 10;");
            migrationBuilder.Sql(@"
                UPDATE [Customers].[Customer]
                SET AlternativePhone = '+1' + REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(AlternativePhone, '-', ''), ' ', ''), '(', ''), ')', ''), '.', '')
                WHERE AlternativePhone IS NOT NULL AND AlternativePhone NOT LIKE '+%' AND LEN(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(AlternativePhone, '-', ''), ' ', ''), '(', ''), ')', ''), '.', '')) = 10;");
            migrationBuilder.Sql(@"
                UPDATE [Customers].[Customer]
                SET EmergencyContactPhone = '+1' + REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(EmergencyContactPhone, '-', ''), ' ', ''), '(', ''), ')', ''), '.', '')
                WHERE EmergencyContactPhone IS NOT NULL AND EmergencyContactPhone NOT LIKE '+%' AND LEN(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(EmergencyContactPhone, '-', ''), ' ', ''), '(', ''), ')', ''), '.', '')) = 10;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE [Customers].[Customer]
                SET Phone = SUBSTRING(Phone, 3, 3) + '-' + SUBSTRING(Phone, 6, 3) + '-' + SUBSTRING(Phone, 9, 4)
                WHERE Phone LIKE '+1__________';");

            migrationBuilder.Sql(@"
                UPDATE [Customers].[Customer]
                SET AlternativePhone = SUBSTRING(AlternativePhone, 3, 3) + '-' + SUBSTRING(AlternativePhone, 6, 3) + '-' + SUBSTRING(AlternativePhone, 9, 4)
                WHERE AlternativePhone LIKE '+1__________';");

            migrationBuilder.Sql(@"
                UPDATE [Customers].[Customer]
                SET EmergencyContactPhone = SUBSTRING(EmergencyContactPhone, 3, 3) + '-' + SUBSTRING(EmergencyContactPhone, 6, 3) + '-' + SUBSTRING(EmergencyContactPhone, 9, 4)
                WHERE EmergencyContactPhone LIKE '+1__________';");

            migrationBuilder.DropColumn(
                name: "PhoneCode",
                schema: "Reference",
                table: "Country");

            migrationBuilder.DropColumn(
                name: "PhoneMask",
                schema: "Reference",
                table: "Country");
        }
    }
}
