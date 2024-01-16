using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetakesAllocator.Migrations
{
    /// <inheritdoc />
    public partial class BigIntTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WeaponPreferences",
                table: "UserSettings",
                type: "text",
                maxLength: 10000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 10000,
                oldNullable: true);

            migrationBuilder.AlterColumn<ulong>(
                name: "UserId",
                table: "UserSettings",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WeaponPreferences",
                table: "UserSettings",
                type: "TEXT",
                maxLength: 10000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 10000,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "bigint");
        }
    }
}
