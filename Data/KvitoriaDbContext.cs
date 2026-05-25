using Kvitoria.Models;
using Kvitoria.Models.Auth;
using Kvitoria.Models.Feedback;
using Kvitoria.Models.PlantCatalog;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kvitoria.Data;

public class KvitoriaDbContext(DbContextOptions<KvitoriaDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Plant> Plants => Set<Plant>();

    public DbSet<CareLog> CareLogs => Set<CareLog>();

    public DbSet<PlantSpecies> PlantSpecies => Set<PlantSpecies>();

    public DbSet<PlantVariety> PlantVarieties => Set<PlantVariety>();

    public DbSet<FeedbackMessage> FeedbackMessages => Set<FeedbackMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName).HasMaxLength(80).IsRequired();
            entity.Property(user => user.BirthDate).HasColumnType("date");
            entity.Property(user => user.LastLoginChangedAtUtc);
            entity.Property(user => user.LastPasswordChangedAtUtc);
        });

        modelBuilder.Entity<Plant>(entity =>
        {
            entity.ToTable("plants");
            entity.HasKey(plant => plant.Id);

            entity.Property(plant => plant.Id).HasColumnName("id");
            entity.Property(plant => plant.UserId).HasColumnName("user_id");
            entity.Property(plant => plant.PlantSpeciesId).HasColumnName("plant_species_id");
            entity.Property(plant => plant.PlantVarietyId).HasColumnName("plant_variety_id");
            entity.Property(plant => plant.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
            entity.Property(plant => plant.Species).HasColumnName("species").HasMaxLength(120).IsRequired();
            entity.Property(plant => plant.Variety).HasColumnName("variety").HasMaxLength(80);
            entity.Property(plant => plant.Status).HasColumnName("status");
            entity.Property(plant => plant.LightRequirement).HasColumnName("light_requirement");
            entity.Property(plant => plant.WateringFrequency).HasColumnName("watering_frequency");
            entity.Property(plant => plant.Location).HasColumnName("location").HasMaxLength(80);
            entity.Property(plant => plant.PotDiameterCm).HasColumnName("pot_diameter_cm").HasPrecision(5, 1);
            entity.Property(plant => plant.AcquisitionDate).HasColumnName("acquisition_date");
            entity.Property(plant => plant.AcquisitionSource).HasColumnName("acquisition_source").HasMaxLength(120);
            entity.Property(plant => plant.LastWateredDate).HasColumnName("last_watered_date");
            entity.Property(plant => plant.NextWateringDate).HasColumnName("next_watering_date");
            entity.Property(plant => plant.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
            entity.Property(plant => plant.Notes).HasColumnName("notes").HasMaxLength(1000);
            entity.Property(plant => plant.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(plant => plant.UpdatedAtUtc).HasColumnName("updated_at_utc");

            entity.HasIndex(plant => plant.Status);
            entity.HasIndex(plant => plant.NextWateringDate);
            entity.HasIndex(plant => plant.UserId);
            entity.HasIndex(plant => plant.PlantSpeciesId);
            entity.HasIndex(plant => plant.PlantVarietyId);

            entity.HasOne(plant => plant.Owner)
                .WithMany(user => user.Plants)
                .HasForeignKey(plant => plant.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(plant => plant.CatalogSpecies)
                .WithMany(species => species.Plants)
                .HasForeignKey(plant => plant.PlantSpeciesId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(plant => plant.CatalogVariety)
                .WithMany(variety => variety.Plants)
                .HasForeignKey(plant => plant.PlantVarietyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PlantSpecies>(entity =>
        {
            entity.ToTable("plant_species");
            entity.HasKey(species => species.Id);

            entity.Property(species => species.Id).HasColumnName("id");
            entity.Property(species => species.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(species => species.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(species => species.UpdatedAtUtc).HasColumnName("updated_at_utc");

            entity.HasIndex(species => species.Name).IsUnique();
        });

        modelBuilder.Entity<PlantVariety>(entity =>
        {
            entity.ToTable("plant_varieties");
            entity.HasKey(variety => variety.Id);

            entity.Property(variety => variety.Id).HasColumnName("id");
            entity.Property(variety => variety.PlantSpeciesId).HasColumnName("plant_species_id");
            entity.Property(variety => variety.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
            entity.Property(variety => variety.Type).HasColumnName("type");
            entity.Property(variety => variety.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(variety => variety.UpdatedAtUtc).HasColumnName("updated_at_utc");

            entity.HasOne(variety => variety.Species)
                .WithMany(species => species.Varieties)
                .HasForeignKey(variety => variety.PlantSpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(variety => variety.Name);
            entity.HasIndex(variety => new { variety.PlantSpeciesId, variety.Name, variety.Type }).IsUnique();
        });

        modelBuilder.Entity<CareLog>(entity =>
        {
            entity.ToTable("care_logs");
            entity.HasKey(log => log.Id);

            entity.Property(log => log.Id).HasColumnName("id");
            entity.Property(log => log.PlantId).HasColumnName("plant_id");
            entity.Property(log => log.ActivityType).HasColumnName("activity_type");
            entity.Property(log => log.PerformedOn).HasColumnName("performed_on");
            entity.Property(log => log.Notes).HasColumnName("notes").HasMaxLength(700);
            entity.Property(log => log.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(log => log.UpdatedAtUtc).HasColumnName("updated_at_utc");

            entity.HasOne(log => log.Plant)
                .WithMany(plant => plant.CareLogs)
                .HasForeignKey(log => log.PlantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(log => log.PerformedOn);
            entity.HasIndex(log => new { log.PlantId, log.ActivityType, log.PerformedOn }).IsUnique();
        });

        modelBuilder.Entity<FeedbackMessage>(entity =>
        {
            entity.ToTable("feedback_messages");
            entity.HasKey(message => message.Id);

            entity.Property(message => message.Id).HasColumnName("id");
            entity.Property(message => message.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(message => message.Subject).HasColumnName("subject").HasMaxLength(140).IsRequired();
            entity.Property(message => message.Body).HasColumnName("body").HasMaxLength(2500).IsRequired();
            entity.Property(message => message.IsRead).HasColumnName("is_read");
            entity.Property(message => message.ReadAtUtc).HasColumnName("read_at_utc");
            entity.Property(message => message.AdminReply).HasColumnName("admin_reply").HasMaxLength(2500);
            entity.Property(message => message.RepliedAtUtc).HasColumnName("replied_at_utc");
            entity.Property(message => message.RepliedByAdminId).HasColumnName("replied_by_admin_id");
            entity.Property(message => message.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(message => message.UpdatedAtUtc).HasColumnName("updated_at_utc");

            entity.HasOne(message => message.User)
                .WithMany()
                .HasForeignKey(message => message.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(message => message.RepliedByAdmin)
                .WithMany()
                .HasForeignKey(message => message.RepliedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(message => message.IsRead);
            entity.HasIndex(message => message.CreatedAtUtc);
        });

    }
}
