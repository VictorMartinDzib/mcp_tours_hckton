using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tours.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Destination = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    Reviews = table.Column<string>(type: "jsonb", nullable: false),
                    Photos = table.Column<string>(type: "jsonb", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    minimum_age = table.Column<int>(type: "integer", nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    IncludesTransport = table.Column<bool>(type: "boolean", nullable: false),
                    IncludesGuide = table.Column<bool>(type: "boolean", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsIndoorAlternative = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "itineraries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Destination = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NumberOfPeople = table.Column<int>(type: "integer", nullable: false),
                    Ages = table.Column<string>(type: "jsonb", nullable: false),
                    Preferences = table.Column<string>(type: "jsonb", nullable: false),
                    Budget = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Items = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itineraries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activities");

            migrationBuilder.DropTable(
                name: "itineraries");
        }
    }
}
