using System;
using Kvitoria.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kvitoria.Migrations;

[DbContext(typeof(KvitoriaDbContext))]
[Migration("20260511204000_UserAndPlantCatalogFeatures")]
public partial class UserAndPlantCatalogFeatures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "BirthDate",
            table: "AspNetUsers",
            type: "date",
            nullable: false,
            defaultValue: new DateOnly(1990, 1, 1));

        migrationBuilder.AddColumn<DateTime>(
            name: "LastLoginChangedAtUtc",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastPasswordChangedAtUtc",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "plant_species",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_plant_species", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "plant_varieties",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                plant_species_id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                type = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_plant_varieties", x => x.id);
                table.ForeignKey(
                    name: "FK_plant_varieties_plant_species_plant_species_id",
                    column: x => x.plant_species_id,
                    principalTable: "plant_species",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.AddColumn<int>(
            name: "plant_species_id",
            table: "plants",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "plant_variety_id",
            table: "plants",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_plant_species_name",
            table: "plant_species",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_plant_varieties_name",
            table: "plant_varieties",
            column: "name");

        migrationBuilder.CreateIndex(
            name: "IX_plant_varieties_plant_species_id_name_type",
            table: "plant_varieties",
            columns: new[] { "plant_species_id", "name", "type" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_plants_plant_species_id",
            table: "plants",
            column: "plant_species_id");

        migrationBuilder.CreateIndex(
            name: "IX_plants_plant_variety_id",
            table: "plants",
            column: "plant_variety_id");

        migrationBuilder.Sql("""
            INSERT INTO plant_species (name, created_at_utc)
            SELECT DISTINCT trim(species), now()
            FROM plants
            WHERE species IS NOT NULL
              AND trim(species) <> ''
              AND NOT EXISTS (
                  SELECT 1 FROM plant_species existing
                  WHERE lower(existing.name) = lower(trim(plants.species))
              );
            """);

        migrationBuilder.Sql("""
            INSERT INTO plant_varieties (plant_species_id, name, type, created_at_utc)
            SELECT DISTINCT species.id, trim(plants.variety), 2, now()
            FROM plants
            INNER JOIN plant_species species ON lower(species.name) = lower(trim(plants.species))
            WHERE plants.variety IS NOT NULL
              AND trim(plants.variety) <> ''
              AND NOT EXISTS (
                  SELECT 1
                  FROM plant_varieties existing
                  WHERE existing.plant_species_id = species.id
                    AND lower(existing.name) = lower(trim(plants.variety))
                    AND existing.type = 2
              );
            """);

        migrationBuilder.Sql("""
            UPDATE plants
            SET plant_species_id = species.id
            FROM plant_species species
            WHERE lower(species.name) = lower(trim(plants.species));
            """);

        migrationBuilder.Sql("""
            UPDATE plants
            SET plant_variety_id = variety.id
            FROM plant_varieties variety
            WHERE variety.plant_species_id = plants.plant_species_id
              AND plants.variety IS NOT NULL
              AND lower(variety.name) = lower(trim(plants.variety));
            """);

        migrationBuilder.Sql("""
            UPDATE plants
            SET next_watering_date = last_watered_date + watering_frequency
            WHERE last_watered_date IS NOT NULL;
            """);

        migrationBuilder.Sql("""
            UPDATE "AspNetUsers"
            SET "UserName" = 'admin',
                "NormalizedUserName" = 'ADMIN',
                "BirthDate" = DATE '1990-01-01'
            WHERE "Email" = 'admin@kvitoria.local';

            UPDATE "AspNetUsers"
            SET "UserName" = 'user.demo',
                "NormalizedUserName" = 'USER.DEMO',
                "BirthDate" = DATE '1995-05-20'
            WHERE "Email" = 'user@kvitoria.local';
            """);

        migrationBuilder.AddForeignKey(
            name: "FK_plants_plant_species_plant_species_id",
            table: "plants",
            column: "plant_species_id",
            principalTable: "plant_species",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_plants_plant_varieties_plant_variety_id",
            table: "plants",
            column: "plant_variety_id",
            principalTable: "plant_varieties",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_plants_plant_species_plant_species_id",
            table: "plants");

        migrationBuilder.DropForeignKey(
            name: "FK_plants_plant_varieties_plant_variety_id",
            table: "plants");

        migrationBuilder.DropIndex(
            name: "IX_plants_plant_species_id",
            table: "plants");

        migrationBuilder.DropIndex(
            name: "IX_plants_plant_variety_id",
            table: "plants");

        migrationBuilder.DropColumn(
            name: "plant_species_id",
            table: "plants");

        migrationBuilder.DropColumn(
            name: "plant_variety_id",
            table: "plants");

        migrationBuilder.DropTable(
            name: "plant_varieties");

        migrationBuilder.DropTable(
            name: "plant_species");

        migrationBuilder.DropColumn(
            name: "BirthDate",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "LastLoginChangedAtUtc",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "LastPasswordChangedAtUtc",
            table: "AspNetUsers");
    }
}
