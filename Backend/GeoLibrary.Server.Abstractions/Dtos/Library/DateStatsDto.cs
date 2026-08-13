using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Dtos.Library;

public class DateStats
{
    public required DateOnly Date { get; set; }
    public required long ViewsCount { get; set; }
}
