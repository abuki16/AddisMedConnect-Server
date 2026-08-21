using AddisMedConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AddisMedConnect.Infrastructure.Persistence;

public class AddisDbContext : DbContext
{
    public AddisDbContext(DbContextOptions<AddisDbContext> options) : base(options) { }

    public DbSet<Hospital> Hospitals => Set<Hospital>();
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Ambulance> Ambulances => Set<Ambulance>();
    public DbSet<EmergencyCase> EmergencyCases => Set<EmergencyCase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Hospital relationships
        modelBuilder.Entity<Hospital>()
            .HasMany(h => h.Beds)
            .WithOne(b => b.Hospital)
            .HasForeignKey(b => b.HospitalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Hospital>()
            .HasMany(h => h.Staff)
            .WithOne(u => u.AssignedHospital)
            .HasForeignKey(u => u.AssignedHospitalId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Hospital>()
            .HasMany(h => h.DestinationCases)
            .WithOne(c => c.TargetHospital)
            .HasForeignKey(c => c.TargetHospitalId)
            .OnDelete(DeleteBehavior.Restrict);

        // EmergencyCase -> Bed relationship
        modelBuilder.Entity<EmergencyCase>()
            .HasOne(c => c.AssignedBed)
            .WithOne(b => b.CurrentCase)
            .HasForeignKey<Bed>(b => b.CurrentCaseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}