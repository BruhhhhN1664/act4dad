using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> b)
    {
        b.ToTable("Students", t =>
        {
            t.HasCheckConstraint("CK_Students_NumberNotBlank", "btrim(\"StudentNumber\") <> ''");
            t.HasCheckConstraint("CK_Students_NameNotBlank", "btrim(\"FullName\") <> ''");
        });
        b.HasKey(s => s.Id);
        b.Property(s => s.StudentNumber).HasMaxLength(20).IsRequired();
        b.Property(s => s.StudentNumberNormalized).HasMaxLength(20).IsRequired();
        b.Property(s => s.FullName).HasMaxLength(100).IsRequired();
        b.Property(s => s.Contact).HasMaxLength(100);
        b.Property(s => s.IsAllowedToBorrow).IsRequired();
        b.Ignore(s => s.EligibilityText);
        b.HasIndex(s => s.StudentNumberNormalized).IsUnique().HasDatabaseName("UX_Students_StudentNumberNormalized");
    }
}

public sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> b)
    {
        b.ToTable("Equipment", t =>
        {
            t.HasCheckConstraint("CK_Equipment_IdentifierNotBlank", "btrim(\"EquipmentIdentifier\") <> ''");
            t.HasCheckConstraint("CK_Equipment_NameNotBlank", "btrim(\"Name\") <> ''");
        });
        b.HasKey(e => e.Id);
        b.Property(e => e.EquipmentIdentifier).HasMaxLength(30).IsRequired();
        b.Property(e => e.EquipmentIdentifierNormalized).HasMaxLength(30).IsRequired();
        b.Property(e => e.Name).HasMaxLength(100).IsRequired();
        b.Property(e => e.Description).HasMaxLength(200);
        b.Property(e => e.IsAvailable).IsRequired();
        b.Ignore(e => e.AvailabilityText);
        b.HasIndex(e => e.EquipmentIdentifierNormalized).IsUnique().HasDatabaseName("UX_Equipment_IdentifierNormalized");
    }
}

public sealed class BorrowingConfiguration : IEntityTypeConfiguration<Borrowing>
{
    public void Configure(EntityTypeBuilder<Borrowing> b)
    {
        b.ToTable("Borrowings", t =>
        {
            t.HasCheckConstraint("CK_Borrowings_DueAfterBorrowed", "\"ExpectedReturnAt\" > \"BorrowedAt\"");
            t.HasCheckConstraint("CK_Borrowings_StatusMatchesReturn",
                "(\"Status\" = 'Active' AND \"ReturnedAt\" IS NULL) OR " +
                "(\"Status\" = 'Returned' AND \"ReturnedAt\" IS NOT NULL AND \"ReturnedAt\" >= \"BorrowedAt\")");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.BorrowedAt).IsRequired();
        b.Property(x => x.ExpectedReturnAt).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired();
        b.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired();

        b.HasIndex(x => x.EquipmentId).IsUnique().HasFilter("\"ReturnedAt\" IS NULL")
            .HasDatabaseName("UX_Borrowings_ActiveByEquipment");
        b.HasIndex(x => x.StudentId).HasDatabaseName("IX_Borrowings_StudentId_Lookup");
    }
}
