using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixChecklistSharingAndVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Checklists\" ADD COLUMN IF NOT EXISTS \"IsPublic\" boolean NOT NULL DEFAULT TRUE;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""ChecklistAccesses"" (
                    ""ChecklistId"" uuid NOT NULL,
                    ""UserId"" text NOT NULL,
                    ""IsOwner"" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""PK_ChecklistAccesses"" PRIMARY KEY (""ChecklistId"", ""UserId""),
                    CONSTRAINT ""FK_ChecklistAccesses_Checklists_ChecklistId"" FOREIGN KEY (""ChecklistId"") REFERENCES ""Checklists"" (""Id"") ON DELETE CASCADE
                );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ChecklistAccesses");
            migrationBuilder.DropColumn(name: "IsPublic", table: "Checklists");
        }
    }
}
