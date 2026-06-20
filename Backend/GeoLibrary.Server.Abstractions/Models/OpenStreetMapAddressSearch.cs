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
    [JsonPropertyName("house_number")]
    public required string Neighbourhood { get; set; }
    [JsonPropertyName("postcode")]
    public required string PostalCode { get; set; }
}

public class OpenStreetMapAddressSearch
{
    public Address address { get; set; }
    public string addresstype { get; set; }
    public List<string> boundingbox { get; set; }
    public string category { get; set; }
    public string display_name { get; set; }
    public double importance { get; set; }
    public string lat { get; set; }
    public string licence { get; set; }
    public string lon { get; set; }
    public string name { get; set; }
    public int osm_id { get; set; }
    public string osm_type { get; set; }
    public int place_id { get; set; }
    public int place_rank { get; set; }
    public string type { get; set; }
}
