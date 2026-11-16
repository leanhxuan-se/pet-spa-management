// Migration cập nhật cấu trúc cơ sở dữ liệu.
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminStaffRole : Migration
    {
        // Áp dụng thay đổi cơ sở dữ liệu.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint("ck_staff_role", "staff", "operation");
            migrationBuilder.AddCheckConstraint(
                name: "ck_staff_role",
                schema: "operation",
                table: "staff",
                sql: "role IN ('MANAGER', 'RECEPTIONIST', 'STAFF', 'ADMIN')");
        }

        // Hoàn tác thay đổi cơ sở dữ liệu.
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint("ck_staff_role", "staff", "operation");
            migrationBuilder.AddCheckConstraint(
                name: "ck_staff_role",
                schema: "operation",
                table: "staff",
                sql: "role IN ('MANAGER', 'RECEPTIONIST', 'STAFF')");
        }
    }
}
