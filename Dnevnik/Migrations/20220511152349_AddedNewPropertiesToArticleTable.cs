using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dnevnik.Migrations
{
    public partial class AddedNewPropertiesToArticleTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateModified",
                table: "Articles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatePublished",
                table: "Articles",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateModified",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "DatePublished",
                table: "Articles");
        }
    }
}
