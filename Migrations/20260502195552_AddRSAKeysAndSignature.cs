using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KriptoProje.Migrations
{
    /// <inheritdoc />
    public partial class AddRSAKeysAndSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DigitalSignature",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RSAPrivateKey",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RSAPublicKey",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DigitalSignature",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RSAPrivateKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RSAPublicKey",
                table: "Users");
        }
    }
}
