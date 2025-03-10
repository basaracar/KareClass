using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KareClass.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCourseRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Classes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_DepartmentId",
                table: "Classes",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Departments_DepartmentId",
                table: "Classes",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Departments_DepartmentId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_DepartmentId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Classes");
        }
    }
}
