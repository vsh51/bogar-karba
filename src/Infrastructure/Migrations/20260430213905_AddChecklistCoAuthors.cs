using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistCoAuthors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChecklistCoAuthors",
                columns: table => new
                {
                    ChecklistId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    WritingPermission = table.Column<bool>(type: "boolean", nullable: false),
                    DeletePermission = table.Column<bool>(type: "boolean", nullable: false),
                    ActivateTogglePermission = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistCoAuthors", x => new { x.ChecklistId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ChecklistCoAuthors_Checklists_ChecklistId",
                        column: x => x.ChecklistId,
                        principalTable: "Checklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChecklistCoAuthors");
        }
    }
}
