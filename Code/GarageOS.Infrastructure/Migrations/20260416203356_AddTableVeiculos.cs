using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTableVeiculos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Veiculos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarcaVeiculo = table.Column<string>(type: "text", nullable: false),
                    ModeloVeiculo = table.Column<string>(type: "text", nullable: false),
                    PlacaVeiculo = table.Column<string>(type: "text", nullable: false),
                    AnoVeiculo = table.Column<int>(type: "integer", nullable: false),
                    PrecoVeiculo = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veiculos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Veiculos");
        }
    }
}
