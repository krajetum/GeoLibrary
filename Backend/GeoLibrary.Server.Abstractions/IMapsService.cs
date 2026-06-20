using GeoLibrary.Server.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions;

public interface IMapsService
{
    Task<List<OpenStreetMapAddressSearch>> GetAddressInfo(string address);
}
