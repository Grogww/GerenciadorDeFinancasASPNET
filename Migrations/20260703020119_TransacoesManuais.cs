using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorDeFinancasASPNET.Migrations
{
    /// <inheritdoc />
    public partial class TransacoesManuais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origem",
                table: "Transactions",
                type: "TEXT",
                nullable: false,
                defaultValue: "Extrato");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origem",
                table: "Transactions");
        }
    }
}
