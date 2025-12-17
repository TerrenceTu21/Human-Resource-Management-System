using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fyphrms.Migrations
{
    /// <inheritdoc />
    public partial class AddContributionBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Month",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Payrolls");

            migrationBuilder.RenameColumn(
                name: "SOCSO",
                table: "Payrolls",
                newName: "SOCSOEmployer");

            migrationBuilder.RenameColumn(
                name: "EPF",
                table: "Payrolls",
                newName: "SOCSOEmployee");

            migrationBuilder.RenameColumn(
                name: "EIS",
                table: "Payrolls",
                newName: "OvertimePay");

            migrationBuilder.AddColumn<decimal>(
                name: "EISEmployee",
                table: "Payrolls",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EISEmployer",
                table: "Payrolls",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EPFEmployee",
                table: "Payrolls",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EPFEmployer",
                table: "Payrolls",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Payrolls",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EISEmployee",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "EISEmployer",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "EPFEmployee",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "EPFEmployer",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Payrolls");

            migrationBuilder.RenameColumn(
                name: "SOCSOEmployer",
                table: "Payrolls",
                newName: "SOCSO");

            migrationBuilder.RenameColumn(
                name: "SOCSOEmployee",
                table: "Payrolls",
                newName: "EPF");

            migrationBuilder.RenameColumn(
                name: "OvertimePay",
                table: "Payrolls",
                newName: "EIS");

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
    }
}
