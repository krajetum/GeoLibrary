using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Models;

public class LibraryTrackingRequest : TrackingRequest
{
    public Guid LibraryId { get; set; }
}
