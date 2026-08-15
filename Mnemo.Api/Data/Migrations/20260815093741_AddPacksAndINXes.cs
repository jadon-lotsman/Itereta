using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnemo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPacksAndINXes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Entries_UserId",
                table: "Entries");

            migrationBuilder.RenameColumn(
                name: "TranscriptionAudioUrl",
                table: "Entries",
                newName: "AudioUrl");

            migrationBuilder.AddColumn<int>(
                name: "CEFR",
                table: "Entries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourcePackId",
                table: "Entries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Entries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Packs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Packs_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VocabularyPackId = table.Column<int>(type: "INTEGER", nullable: false),
                    PartOfSpeech = table.Column<int>(type: "INTEGER", nullable: true),
                    CEFR = table.Column<int>(type: "INTEGER", nullable: true),
                    Foreign = table.Column<string>(type: "TEXT", nullable: false),
                    Transcription = table.Column<string>(type: "TEXT", nullable: true),
                    AudioUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Examples = table.Column<string>(type: "TEXT", nullable: false),
                    Translations = table.Column<string>(type: "TEXT", nullable: false),
                    Synonyms = table.Column<string>(type: "TEXT", nullable: false),
                    Antonyms = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackEntries_Packs_VocabularyPackId",
                        column: x => x.VocabularyPackId,
                        principalTable: "Packs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_SourcePackId",
                table: "Entries",
                column: "SourcePackId");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_UserId_Foreign_PartOfSpeech",
                table: "Entries",
                columns: new[] { "UserId", "Foreign", "PartOfSpeech" });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_UserId_SourcePackId",
                table: "Entries",
                columns: new[] { "UserId", "SourcePackId" });

            migrationBuilder.CreateIndex(
                name: "IX_PackEntries_VocabularyPackId",
                table: "PackEntries",
                column: "VocabularyPackId");

            migrationBuilder.CreateIndex(
                name: "IX_Packs_AuthorId",
                table: "Packs",
                column: "AuthorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Entries_Packs_SourcePackId",
                table: "Entries",
                column: "SourcePackId",
                principalTable: "Packs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entries_Packs_SourcePackId",
                table: "Entries");

            migrationBuilder.DropTable(
                name: "PackEntries");

            migrationBuilder.DropTable(
                name: "Packs");

            migrationBuilder.DropIndex(
                name: "IX_Entries_SourcePackId",
                table: "Entries");

            migrationBuilder.DropIndex(
                name: "IX_Entries_UserId_Foreign_PartOfSpeech",
                table: "Entries");

            migrationBuilder.DropIndex(
                name: "IX_Entries_UserId_SourcePackId",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "CEFR",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "SourcePackId",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Entries");

            migrationBuilder.RenameColumn(
                name: "AudioUrl",
                table: "Entries",
                newName: "TranscriptionAudioUrl");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_UserId",
                table: "Entries",
                column: "UserId");
        }
    }
}
