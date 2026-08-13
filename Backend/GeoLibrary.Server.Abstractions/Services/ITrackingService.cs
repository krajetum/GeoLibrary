using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Services;

public interface ITrackingService<T>
{

    Task<bool> TrackAsync(T request);


}
