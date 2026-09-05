using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnemo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepetitionTasks_Users_UserId",
                table: "RepetitionTasks");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "RepetitionTasks",
                newName: "OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_RepetitionTasks_UserId",
                table: "RepetitionTasks",
                newName: "IX_RepetitionTasks_OwnerId");

            migrationBuilder.AlterColumn<int>(
                name: "VocabularyId",
                table: "VocabularyEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Vocabularies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_RepetitionTasks_Users_OwnerId",
                table: "RepetitionTasks",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepetitionTasks_Users_OwnerId",
                table: "RepetitionTasks");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Vocabularies");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "RepetitionTasks",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RepetitionTasks_OwnerId",
                table: "RepetitionTasks",
                newName: "IX_RepetitionTasks_UserId");

            migrationBuilder.AlterColumn<int>(
                name: "VocabularyId",
                table: "VocabularyEntries",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_RepetitionTasks_Users_UserId",
                table: "RepetitionTasks",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
