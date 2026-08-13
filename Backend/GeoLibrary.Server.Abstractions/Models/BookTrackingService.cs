using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Models;

public class BookTrackingRequest : TrackingRequest
{
    public required Guid BookId { get; set; }
    public required Guid LibraryId { get; set; }
}
