using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fyphrms.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthYearToPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "Payrolls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Payrolls",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Month",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Payrolls");
        }
    }
}
