using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorDeFinancasASPNET.Migrations
{
    /// <inheritdoc />
    public partial class CategorizacaoAutomatica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Categories_CategoryId",
                table: "Transactions");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                table: "Transactions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportedAt",
                table: "Transactions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "MatchKey",
                table: "Transactions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cor",
                table: "Categories",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Categorias criadas antes desta migration recebem a cor padrão do app.
            migrationBuilder.Sql("UPDATE Categories SET Cor = '#64748b' WHERE Cor = '';");

            migrationBuilder.CreateTable(
                name: "CategoryRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchKey = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryRules_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_MatchKey",
                table: "Transactions",
                column: "MatchKey");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Nome",
                table: "Categories",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryRules_CategoryId",
                table: "CategoryRules",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryRules_MatchKey",
                table: "CategoryRules",
                column: "MatchKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Categories_CategoryId",
                table: "Transactions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Categories_CategoryId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "CategoryRules");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_MatchKey",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Nome",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ImportedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "MatchKey",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Cor",
                table: "Categories");

            migrationBuilder.AlterColumn<int>(
                name: "NumeroDocumento",
                table: "Transactions",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Categories_CategoryId",
                table: "Transactions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}
