using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialTool.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GoogleWorkspaceDomainsList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Renomeia em vez de apagar e recriar: preserva o domínio que já estiver configurado.
            migrationBuilder.RenameColumn(
                name: "GoogleWorkspaceDomain",
                table: "organization",
                newName: "GoogleWorkspaceDomains");

            migrationBuilder.AlterColumn<string>(
                name: "GoogleWorkspaceDomains",
                table: "organization",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GoogleWorkspaceDomains",
                table: "organization",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.RenameColumn(
                name: "GoogleWorkspaceDomains",
                table: "organization",
                newName: "GoogleWorkspaceDomain");
        }
    }
}
