using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class createdUpdateMethodforEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Goal",
                table: "Sprints",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Goal",
                table: "Sprints");
        }
    }
}
