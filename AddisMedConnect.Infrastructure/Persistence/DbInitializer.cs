using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AddisMedConnect.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(AddisDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Hospitals.AnyAsync()) return;

        var tikurAnbessa = new Hospital
        {
            Name = "Tikur Anbessa Specialized Hospital",
            SubCity = "Lideta",
            Address = "Zewditu Street, Addis Ababa",
            Latitude = 9.0158,
            Longitude = 38.7508,
            ContactPhone = "+251115511211"
        };

        var stPaul = new Hospital
        {
            Name = "St. Paul's Hospital Millennium Medical College",
            SubCity = "Gullele",
            Address = "Swaziland St, Addis Ababa",
            Latitude = 9.0583,
            Longitude = 38.7381,
            ContactPhone = "+251112750125"
        };

        await context.Hospitals.AddRangeAsync(tikurAnbessa, stPaul);

        var beds = new List<Bed>
        {
            new() { BedNumber = "ICU-01", WardType = "ICU", Status = BedStatus.Available, Hospital = tikurAnbessa },
            new() { BedNumber = "ICU-02", WardType = "ICU", Status = BedStatus.Available, Hospital = tikurAnbessa },
            new() { BedNumber = "EMG-01", WardType = "Emergency", Status = BedStatus.Available, Hospital = tikurAnbessa },
            new() { BedNumber = "ICU-01", WardType = "ICU", Status = BedStatus.Available, Hospital = stPaul },
            new() { BedNumber = "EMG-01", WardType = "Emergency", Status = BedStatus.Available, Hospital = stPaul }
        };

        await context.Beds.AddRangeAsync(beds);
        await context.SaveChangesAsync();
    }
}