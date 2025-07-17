using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoGalleryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToGalleryFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "GalleryFiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "GalleryFiles");
        }
    }
}
