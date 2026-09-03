using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnemo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabularyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entries_Packs_SourcePackId",
                table: "Entries");

            migrationBuilder.DropForeignKey(
                name: "FK_Entries_Users_UserId",
                table: "Entries");

            migrationBuilder.DropForeignKey(
                name: "FK_RepetitionStates_Entries_VocabularyEntryId",
                table: "RepetitionStates");

            migrationBuilder.DropForeignKey(
                name: "FK_RepetitionStates_Users_UserId",
                table: "RepetitionStates");

            migrationBuilder.DropTable(
                name: "PackEntries");

            migrationBuilder.DropTable(
                name: "Packs");

            migrationBuilder.DropIndex(
                name: "IX_RepetitionStates_UserId",
                table: "RepetitionStates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Entries",
                table: "Entries");

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
                name: "UserId",
                table: "RepetitionStates");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Entries");

            migrationBuilder.RenameTable(
                name: "Entries",
                newName: "VocabularyEntries");

            migrationBuilder.RenameColumn(
                name: "SourcePackId",
                table: "VocabularyEntries",
                newName: "VocabularyId");

            migrationBuilder.AddColumn<int>(
                name: "MergedFromId",
                table: "VocabularyEntries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_VocabularyEntries",
                table: "VocabularyEntries",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Vocabularies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Guid = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vocabularies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vocabularies_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyEntries_MergedFromId",
                table: "VocabularyEntries",
                column: "MergedFromId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyEntries_VocabularyId_Foreign_PartOfSpeech",
                table: "VocabularyEntries",
                columns: new[] { "VocabularyId", "Foreign", "PartOfSpeech" });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyEntries_VocabularyId_MergedFromId",
                table: "VocabularyEntries",
                columns: new[] { "VocabularyId", "MergedFromId" });

            migrationBuilder.CreateIndex(
                name: "IX_Vocabularies_OwnerId",
                table: "Vocabularies",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_RepetitionStates_VocabularyEntries_VocabularyEntryId",
                table: "RepetitionStates",
                column: "VocabularyEntryId",
                principalTable: "VocabularyEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VocabularyEntries_Vocabularies_MergedFromId",
                table: "VocabularyEntries",
                column: "MergedFromId",
                principalTable: "Vocabularies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VocabularyEntries_Vocabularies_VocabularyId",
                table: "VocabularyEntries",
                column: "VocabularyId",
                principalTable: "Vocabularies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepetitionStates_VocabularyEntries_VocabularyEntryId",
                table: "RepetitionStates");

            migrationBuilder.DropForeignKey(
                name: "FK_VocabularyEntries_Vocabularies_MergedFromId",
                table: "VocabularyEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_VocabularyEntries_Vocabularies_VocabularyId",
                table: "VocabularyEntries");

            migrationBuilder.DropTable(
                name: "Vocabularies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VocabularyEntries",
                table: "VocabularyEntries");

            migrationBuilder.DropIndex(
                name: "IX_VocabularyEntries_MergedFromId",
                table: "VocabularyEntries");

            migrationBuilder.DropIndex(
                name: "IX_VocabularyEntries_VocabularyId_Foreign_PartOfSpeech",
                table: "VocabularyEntries");

            migrationBuilder.DropIndex(
                name: "IX_VocabularyEntries_VocabularyId_MergedFromId",
                table: "VocabularyEntries");

            migrationBuilder.DropColumn(
                name: "MergedFromId",
                table: "VocabularyEntries");

            migrationBuilder.RenameTable(
                name: "VocabularyEntries",
                newName: "Entries");

            migrationBuilder.RenameColumn(
                name: "VocabularyId",
                table: "Entries",
                newName: "SourcePackId");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "RepetitionStates",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Entries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Entries",
                table: "Entries",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Packs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AuthorId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Guid = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false)
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
                    Antonyms = table.Column<string>(type: "TEXT", nullable: false),
                    AudioUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CEFR = table.Column<int>(type: "INTEGER", nullable: true),
                    Examples = table.Column<string>(type: "TEXT", nullable: false),
                    Foreign = table.Column<string>(type: "TEXT", nullable: false),
                    PartOfSpeech = table.Column<int>(type: "INTEGER", nullable: true),
                    Synonyms = table.Column<string>(type: "TEXT", nullable: false),
                    Transcription = table.Column<string>(type: "TEXT", nullable: true),
                    Translations = table.Column<string>(type: "TEXT", nullable: false)
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
                name: "IX_RepetitionStates_UserId",
                table: "RepetitionStates",
                column: "UserId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Entries_Users_UserId",
                table: "Entries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RepetitionStates_Entries_VocabularyEntryId",
                table: "RepetitionStates",
                column: "VocabularyEntryId",
                principalTable: "Entries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RepetitionStates_Users_UserId",
                table: "RepetitionStates",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
