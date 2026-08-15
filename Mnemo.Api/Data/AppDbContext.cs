using Microsoft.EntityFrameworkCore;
using Mnemo.Data.Entities;

namespace Mnemo.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<VocabularyEntry> Entries { get; set; }

        public DbSet<VocabularyPack> Packs { get; set; }
        public DbSet<VocabularyPackEntry> PackEntries { get; set; }

        public DbSet<RepetitionState> RepetitionStates { get; set; }
        public DbSet<RepetitionTask> RepetitionTasks { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // VocabularyEntry
            modelBuilder.Entity<VocabularyEntry>()
                .HasIndex(e => new { e.UserId, e.Foreign, e.PartOfSpeech });

            modelBuilder.Entity<VocabularyEntry>()
                .HasIndex(e => new { e.UserId, e.SourcePackId });

            modelBuilder.Entity<VocabularyEntry>()
                .HasOne(e => e.RepetitionState)
                .WithOne(s => s.VocabularyEntry)
                .HasForeignKey<RepetitionState>(s => s.VocabularyEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VocabularyEntry>()
                .HasOne(e => e.User)
                .WithMany(u => u.VocabularyEntries)
                .HasForeignKey(e => e.UserId);


            // RepetitionState
            modelBuilder.Entity<RepetitionState>()
                .HasIndex(s => s.VocabularyEntryId)
                .IsUnique();


            // VocabularyPack
            modelBuilder.Entity<VocabularyPack>()
                .HasIndex(p => p.AuthorId);

            modelBuilder.Entity<VocabularyPack>()
                .HasMany(p => p.PackEntries)
                .WithOne(e => e.VocabularyPack)
                .HasForeignKey(e => e.VocabularyPackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VocabularyPack>()
                .HasOne(p => p.Author)
                .WithMany(u => u.VocabularyPacks)
                .HasForeignKey(p => p.AuthorId);


            // RepetitionTask
            modelBuilder.Entity<RepetitionTask>()
                .HasDiscriminator<string>("task_type")
                .HasValue<TextRepetitionTask>("text")
                .HasValue<OptionRepetitionTask>("option")
                .HasValue<SentenceReorderRepetitionTask>("sentence")
                .HasValue<SyllableReorderRepetitionTask>("syllable")
                .HasValue<YesOrNoRepetitionTask>("yesorno");
        }
    }
}
