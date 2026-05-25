using Kvitoria.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kvitoria.Migrations;

[DbContext(typeof(KvitoriaDbContext))]
[Migration("20260511183600_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "plants",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                species = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                variety = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                status = table.Column<int>(type: "integer", nullable: false),
                light_requirement = table.Column<int>(type: "integer", nullable: false),
                watering_frequency = table.Column<int>(type: "integer", nullable: false),
                location = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                pot_diameter_cm = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                acquisition_date = table.Column<DateOnly>(type: "date", nullable: true),
                acquisition_source = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                last_watered_date = table.Column<DateOnly>(type: "date", nullable: true),
                next_watering_date = table.Column<DateOnly>(type: "date", nullable: true),
                image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_plants", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "care_logs",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                plant_id = table.Column<int>(type: "integer", nullable: false),
                activity_type = table.Column<int>(type: "integer", nullable: false),
                performed_on = table.Column<DateOnly>(type: "date", nullable: false),
                notes = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_care_logs", x => x.id);
                table.ForeignKey(
                    name: "FK_care_logs_plants_plant_id",
                    column: x => x.plant_id,
                    principalTable: "plants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_care_logs_performed_on",
            table: "care_logs",
            column: "performed_on");

        migrationBuilder.CreateIndex(
            name: "IX_care_logs_plant_id",
            table: "care_logs",
            column: "plant_id");

        migrationBuilder.CreateIndex(
            name: "IX_plants_next_watering_date",
            table: "plants",
            column: "next_watering_date");

        migrationBuilder.CreateIndex(
            name: "IX_plants_status",
            table: "plants",
            column: "status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "care_logs");
        migrationBuilder.DropTable(name: "plants");
    }
}
