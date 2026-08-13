using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Models;

public class TrackingRequest
{
    public required string UserSignature { get; set; }
}
