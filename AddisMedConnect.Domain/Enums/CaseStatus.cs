namespace AddisMedConnect.Domain.Enums;

public enum CaseStatus
{
    PendingDispatch = 0,
    Dispatched = 1,
    InTransit = 2,
    ArrivedAtTriage = 3,
    Resolved = 4,
    Cancelled = 5
}