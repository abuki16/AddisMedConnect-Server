namespace AddisMedConnect.Domain.Enums;

public enum CaseStatus
{
    PendingDispatch = 0,
    PendingTriage = 1,
    Dispatched = 2,
    InTransit = 3,
    ArrivedAtTriage = 4,
    Admitted = 5,       // <-- Add this missing state
    Resolved = 6,       // Shift subsequent values accordingly
    Cancelled = 7
}