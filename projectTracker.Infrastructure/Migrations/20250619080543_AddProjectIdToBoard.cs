using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectIdToBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectId",
                table: "Boards",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Boards_ProjectId",
                table: "Boards",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Boards_Projects_ProjectId",
                table: "Boards",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Boards_Projects_ProjectId",
                table: "Boards");

            migrationBuilder.DropIndex(
                name: "IX_Boards_ProjectId",
                table: "Boards");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Boards");
        }
    }
}
