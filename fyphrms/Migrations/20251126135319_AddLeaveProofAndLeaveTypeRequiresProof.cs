using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fyphrms.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveProofAndLeaveTypeRequiresProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresProof",
                table: "LeaveTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProofPath",
                table: "Leaves",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresProof",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "ProofPath",
                table: "Leaves");
        }
    }
}
