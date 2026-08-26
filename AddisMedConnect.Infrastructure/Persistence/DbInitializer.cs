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

        // 1. Define Hospitals with Fixed GUIDs matching your frontend dropdown IDs
        var tikurAnbessa = new Hospital
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Tikur Anbessa Specialized Hospital (Black Lion)",
            Code = "TIKUR",
            SubCity = "Lideta",
            Address = "Zewditu Street, Addis Ababa",
            Latitude = 9.0158,
            Longitude = 38.7508,
            ContactPhone = "+251115511211"
        };

        var stPaul = new Hospital
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "St. Paul’s Hospital Millennium Medical College",
            Code = "STPAUL",
            SubCity = "Gullele",
            Address = "Swaziland St, Addis Ababa",
            Latitude = 9.0583,
            Longitude = 38.7381,
            ContactPhone = "+251112750125"
        };

        var aabet = new Hospital
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Addis Ababa Burn, Emergency & Trauma Hospital (AaBET)",
            Code = "AABET",
            SubCity = "Kolfe Keranio",
            Address = "Alert Compound, Addis Ababa",
            Latitude = 9.0021,
            Longitude = 38.6912,
            ContactPhone = "+251112753300"
        };

        var zewditu = new Hospital
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Name = "Zewditu Memorial Hospital",
            Code = "ZEWDITU",
            SubCity = "Kirkos",
            Address = "Menelik II Ave, Addis Ababa",
            Latitude = 9.0125,
            Longitude = 38.7612,
            ContactPhone = "+251115151111"
        };

        var yekatit = new Hospital
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Name = "Yekatit 12 Hospital Medical College",
            Code = "YEKATIT",
            SubCity = "Arada",
            Address = "Sidist Kilo, Addis Ababa",
            Latitude = 9.0321,
            Longitude = 38.7654,
            ContactPhone = "+251111112233"
        };

        var amin = new Hospital
        {
            Id = Guid.Parse("15151515-1515-1515-1515-151515151515"),
            Name = "Amin General Hospital",
            Code = "AMIN",
            SubCity = "Addis Ketema",
            Address = "Abenet Area, Addis Ababa",
            Latitude = 9.0234,
            Longitude = 38.7341,
            ContactPhone = "+251112732000"
        };

        var mcm = new Hospital
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Name = "Myungsung Christian Medical Center (Korean Hospital)",
            Code = "MCM",
            SubCity = "Bole Subcity",
            Address = "Gerji, Addis Ababa",
            Latitude = 8.9912,
            Longitude = 38.8123,
            ContactPhone = "+251116295441"
        };

        await context.Hospitals.AddRangeAsync(tikurAnbessa, stPaul, aabet, zewditu, yekatit, amin, mcm);

        // 2. Seed Initial Beds for these facilities
        var beds = new List<Bed>
        {
            new() { BedNumber = "ICU-01", WardType = "ICU", Status = BedStatus.Available, Hospital = tikurAnbessa },
            new() { BedNumber = "ICU-02", WardType = "ICU", Status = BedStatus.Available, Hospital = tikurAnbessa },
            new() { BedNumber = "EMG-01", WardType = "Emergency", Status = BedStatus.Available, Hospital = tikurAnbessa },
            new() { BedNumber = "ICU-01", WardType = "ICU", Status = BedStatus.Available, Hospital = stPaul },
            new() { BedNumber = "EMG-01", WardType = "Emergency", Status = BedStatus.Available, Hospital = stPaul },
            new() { BedNumber = "EMG-01", WardType = "Emergency", Status = BedStatus.Available, Hospital = amin },
            new() { BedNumber = "ICU-01", WardType = "ICU", Status = BedStatus.Available, Hospital = aabet }
        };

        await context.Beds.AddRangeAsync(beds);
        await context.SaveChangesAsync();
    }
}