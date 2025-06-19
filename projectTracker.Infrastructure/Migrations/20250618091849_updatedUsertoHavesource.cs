using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatedUsertoHavesource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "Tasks",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AppUserId",
                table: "Tasks",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_AppUserId",
                table: "Tasks",
                column: "AppUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_AppUserId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_AppUserId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Tasks");
        }
    }
}
