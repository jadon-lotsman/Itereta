using Microsoft.EntityFrameworkCore;
using Mnemo.Data.Entities;

namespace Mnemo.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Vocabulary> Vocabularies { get; set; }
        public DbSet<VocabularyEntry> VocabularyEntries { get; set; }
        public DbSet<RepetitionTask> RepetitionTasks { get; set; }
        public DbSet<RepetitionState> RepetitionStates { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Indexes
            modelBuilder.Entity<Vocabulary>()
                .HasIndex(v => v.OwnerId);

            modelBuilder.Entity<VocabularyEntry>()
                .HasIndex(e => new { e.VocabularyId, e.Foreign, e.PartOfSpeech });
            modelBuilder.Entity<VocabularyEntry>()
                .HasIndex(e => new { e.VocabularyId, e.MergedFromId });

            modelBuilder.Entity<RepetitionState>()
                .HasIndex(s => s.VocabularyEntryId)
                .IsUnique();

            // Relations
            modelBuilder.Entity<User>()
                .HasMany(p => p.Vocabularies)
                .WithOne(u => u.Owner)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(p => p.RepetitionTasks)
                .WithOne(u => u.Owner)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Vocabulary>()
                .HasMany(v => v.Entries)
                .WithOne(e => e.Vocabulary)
                .HasForeignKey(e => e.VocabularyId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<VocabularyEntry>()
                .HasOne(e => e.RepetitionState)
                .WithOne(s => s.VocabularyEntry)
                .HasForeignKey<RepetitionState>(s => s.VocabularyEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Discriminators
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
