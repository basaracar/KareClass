using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KareClass.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTeacherModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Teachers_TeacherId1",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_TeacherId1",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "TeacherId1",
                table: "Schedules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeacherId1",
                table: "Schedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    TeacherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Specialty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TeacherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.TeacherId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TeacherId1",
                table: "Schedules",
                column: "TeacherId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Teachers_TeacherId1",
                table: "Schedules",
                column: "TeacherId1",
                principalTable: "Teachers",
                principalColumn: "TeacherId");
        }
    }
}
