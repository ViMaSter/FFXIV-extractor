using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace sqpackExtractor.Model
{
    public partial class hashlistContext : DbContext
    {
        public hashlistContext()
        {
        }

        public hashlistContext(DbContextOptions<hashlistContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Dbinfo> Dbinfo { get; set; }
        public virtual DbSet<Filenames> Filenames { get; set; }
        public virtual DbSet<Folders> Folders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlite("Data Source=" + Environment.CurrentDirectory + "\\Database\\hashlist.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Dbinfo>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("dbinfo");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasColumnName("type")
                    .HasColumnType("string");

                entity.Property(e => e.Value)
                    .IsRequired()
                    .HasColumnName("value")
                    .HasColumnType("string");
            });

            modelBuilder.Entity<Filenames>(entity =>
            {
                entity.HasKey(e => e.Hash);

                entity.ToTable("filenames");

                entity.Property(e => e.Hash)
                    .HasColumnName("hash")
                    .ValueGeneratedNever();

                entity.Property(e => e.Archive)
                    .HasColumnName("archive")
                    .HasColumnType("STRING");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("STRING");

                entity.Property(e => e.Used)
                    .HasColumnName("used")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Version).HasColumnName("version");
            });

            modelBuilder.Entity<Folders>(entity =>
            {
                entity.HasKey(e => e.Hash);

                entity.ToTable("folders");

                entity.Property(e => e.Hash)
                    .HasColumnName("hash")
                    .ValueGeneratedNever();

                entity.Property(e => e.Archive)
                    .HasColumnName("archive")
                    .HasColumnType("STRING");

                entity.Property(e => e.Path)
                    .HasColumnName("path")
                    .HasColumnType("STRING");

                entity.Property(e => e.Used)
                    .HasColumnName("used")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Version).HasColumnName("version");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
