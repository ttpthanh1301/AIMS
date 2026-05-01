using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIMS.BackendServer.Migrations
{
    /// <inheritdoc />
    public partial class AddAIScreeningProcessingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "AIScreeningResults",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Completed");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "AIScreeningResults",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "AIScreeningResults");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "AIScreeningResults");
        }
    }
}
