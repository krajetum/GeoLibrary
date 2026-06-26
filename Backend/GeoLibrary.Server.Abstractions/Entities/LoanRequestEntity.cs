using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Entities;

public class LoanRequestEntity
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required Guid BookId { get; set; }
    public required DateTime BookingDate { get; set; }
    public required DateTime ReturnDate { get; set; }
    public required LoanRequestStatus Status { get; set; } = LoanRequestStatus.Pending;
}

public enum LoanRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Returned
}