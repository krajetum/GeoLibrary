using GeoLibrary.Server.Abstractions.Models;
using GeoLibrary.Server.Abstractions.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GeoLibrary.Server.Services;

public class OpenStreetMapService(HttpClient httpClient, ILogger<OpenStreetMapService> logger) : IMapsService
{
    /// <summary>
    /// Fetches address informations from OpenStreetMap API based on the provided address.
    /// </summary>
    /// <param name="address">The address to search</param>
    /// <returns>A list of matching address objects</returns>
    public async Task<List<OpenStreetMapAddressSearch>> GetAddressInfo(string address)
    {
        try
        {
            var addresses = await httpClient.GetFromJsonAsync<OpenStreetMapAddressSearch[]>($"search?q={address}&limit=5&addressdetails=1&format=jsonv2");

            return addresses?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError("Error occurred while fetching address information: {Error}", ex);
            throw;
        }

    }


}
