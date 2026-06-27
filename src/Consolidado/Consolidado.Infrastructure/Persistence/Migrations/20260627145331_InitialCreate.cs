using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Consolidado.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_processados",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eventos_processados", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "saldos_diarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    comerciante_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    total_creditos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_debitos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quantidade_lancamentos = table.Column<int>(type: "integer", nullable: false),
                    atualizado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fechado = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saldos_diarios", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_saldos_diarios_comerciante_id_data",
                table: "saldos_diarios",
                columns: new[] { "comerciante_id", "data" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos_processados");

            migrationBuilder.DropTable(
                name: "saldos_diarios");
        }
    }
}
