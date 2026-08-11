using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentCouncil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase5Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "due_soon_notified_at_utc",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "overdue_notified_at_utc",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reminder1h_sent_at_utc",
                table: "calendar_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reminder24h_sent_at_utc",
                table: "calendar_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "job_runs",
                columns: table => new
                {
                    job_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_run_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_runs", x => x.job_name);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_runs");

            migrationBuilder.DropColumn(
                name: "due_soon_notified_at_utc",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "overdue_notified_at_utc",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "reminder1h_sent_at_utc",
                table: "calendar_events");

            migrationBuilder.DropColumn(
                name: "reminder24h_sent_at_utc",
                table: "calendar_events");
        }
    }
}
