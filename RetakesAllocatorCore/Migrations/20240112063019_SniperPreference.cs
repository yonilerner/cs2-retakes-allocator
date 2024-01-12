using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetakesAllocator.Migrations
{
    /// <inheritdoc />
    public partial class SniperPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SniperPreference",
                table: "UserSettings",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SniperPreference",
                table: "UserSettings");
        }
    }
}
