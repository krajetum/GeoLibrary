using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GeoLibrary.Server.Abstractions.Models;

public class Address
{
    public required string City { get; set; }
    public required string Country { get; set; }
    [JsonPropertyName("country_code")]
    public required string CountryCode { get; set; }
    [JsonPropertyName("postcode")]
    public required string PostalCode { get; set; }
}

public class OpenStreetMapAddressSearch
{
    public required Address Address { get; set; }
    public required string Addresstype { get; set; }
    public List<string> Boundingbox { get; set; } = [];
    public string Category { get; set; }
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; }
    public double Importance { get; set; }
    public required string Lat { get; set; }
    public required string Licence { get; set; }
    public required string Lon { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
}
