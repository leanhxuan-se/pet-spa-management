// Cập nhật ràng buộc số điện thoại nhân viên.
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Migrations
{
    public partial class InitialOperation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "operation",
                table: "staff",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "operation",
                table: "staff",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);
        }
    }
}
