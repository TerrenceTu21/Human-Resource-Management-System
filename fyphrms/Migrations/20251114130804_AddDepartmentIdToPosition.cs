using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fyphrms.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentIdToPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateIndex(
                name: "IX_Positions_DepartmentID",
                table: "Positions",
                column: "DepartmentID");

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Departments_DepartmentID",
                table: "Positions",
                column: "DepartmentID",
                principalTable: "Departments",
                principalColumn: "DepartmentID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Departments_DepartmentID",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Positions_DepartmentID",
                table: "Positions");

        }
    }
}
