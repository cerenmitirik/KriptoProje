using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KriptoProje.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrityHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "Users");
        }
    }
}
