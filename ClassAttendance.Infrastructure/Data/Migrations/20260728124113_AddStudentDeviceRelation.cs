using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassAttendance.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentDeviceRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_AttendanceSessionId",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_AttendanceSessionId_StudentId",
                table: "Attendances",
                columns: new[] { "AttendanceSessionId", "StudentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_AttendanceSessionId_StudentId",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_AttendanceSessionId",
                table: "Attendances",
                column: "AttendanceSessionId");
        }
    }
}
