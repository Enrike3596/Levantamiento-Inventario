using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventarioTI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agentes_registrados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NombreEquipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApiKeyHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agentes_registrados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "equipos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NombreEquipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UsuarioConectado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Dominio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Mac = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                    SistemaOperativo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VersionSo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RamGb = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Procesador = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DiscoGb = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    EspacioLibreGb = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    NumeroSerie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Fabricante = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Modelo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Antivirus = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UltimoReinicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UltimoReporte = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Programas = table.Column<string>(type: "jsonb", nullable: true),
                    CreadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "switches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Marca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Modelo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Serial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Firmware = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TotalPuertos = table.Column<int>(type: "integer", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UltimoPoll = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_switches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Rol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "switch_puertos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SwitchId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroPuerto = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Vlan = table.Column<int>(type: "integer", nullable: true),
                    TraficoInMb = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    TraficoOutMb = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_switch_puertos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_switch_puertos_switches_SwitchId",
                        column: x => x.SwitchId,
                        principalTable: "switches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agentes_registrados_NombreEquipo",
                table: "agentes_registrados",
                column: "NombreEquipo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipos_NombreEquipo",
                table: "equipos",
                column: "NombreEquipo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_switch_puertos_SwitchId_NumeroPuerto",
                table: "switch_puertos",
                columns: new[] { "SwitchId", "NumeroPuerto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_switches_Ip",
                table: "switches",
                column: "Ip",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Email",
                table: "usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agentes_registrados");

            migrationBuilder.DropTable(
                name: "equipos");

            migrationBuilder.DropTable(
                name: "switch_puertos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "switches");
        }
    }
}
