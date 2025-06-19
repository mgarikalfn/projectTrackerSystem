using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedJiraSprintIDtoTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JiraSprintId",
                table: "Tasks",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JiraSprintId",
                table: "Tasks");
        }
    }
}
