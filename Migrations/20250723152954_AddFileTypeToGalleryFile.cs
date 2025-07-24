using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoGalleryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddFileTypeToGalleryFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "GalleryFiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileType",
                table: "GalleryFiles");
        }
    }
}
