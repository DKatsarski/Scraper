using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dnevnik.Migrations
{
    public partial class NewPropertiesAddedToComment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArticleTitle",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorsInfo",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorsRating",
                table: "Comments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CommentNumber",
                table: "Comments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatePosted",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "NegativeReactions",
                table: "Comments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PositiveReactions",
                table: "Comments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tone",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArticleTitle",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "AuthorsInfo",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "AuthorsRating",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "CommentNumber",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "DatePosted",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "NegativeReactions",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "PositiveReactions",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Tone",
                table: "Comments");
        }
    }
}
