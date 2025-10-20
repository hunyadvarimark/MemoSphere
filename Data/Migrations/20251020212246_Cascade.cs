using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoSphere.Data.Migrations
{
    /// <inheritdoc />
    public partial class Cascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Notes_SourceNoteId",
                table: "Questions");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Notes_SourceNoteId",
                table: "Questions",
                column: "SourceNoteId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Notes_SourceNoteId",
                table: "Questions");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Notes_SourceNoteId",
                table: "Questions",
                column: "SourceNoteId",
                principalTable: "Notes",
                principalColumn: "Id");
        }
    }
}
