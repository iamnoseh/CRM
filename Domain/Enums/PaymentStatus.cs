namespace Domain.Enums;

public enum PaymentStatus
{
    Failed,
    Completed,
    Pending,
    Paid = Completed  // Adding Paid as an alias for Completed to maintain backward compatibility
}