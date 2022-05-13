using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dnevnik.Migrations
{
    public partial class AddedLinksToArticlesAndComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommentLink",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArticleLink",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentLink",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ArticleLink",
                table: "Articles");
        }
    }
}
