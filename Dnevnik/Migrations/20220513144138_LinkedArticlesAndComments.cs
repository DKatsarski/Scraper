using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dnevnik.Migrations
{
    public partial class LinkedArticlesAndComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommentId",
                table: "Articles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentId",
                table: "Articles");
        }
    }
}
