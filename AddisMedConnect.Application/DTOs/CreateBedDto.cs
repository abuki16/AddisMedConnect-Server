public record CreateBedDto(
    string BedNumber, 
    string WardType, 
    string Code, 
    Guid HospitalId
);