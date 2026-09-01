namespace GeoLibrary.Server.Abstractions.Dtos.Dashboard;

/// <summary>
/// Contatori complessivi mostrati nelle tessere del pannello di amministrazione.
/// </summary>
public class DashboardCountersDto
{
    public long UsersCount { get; set; }
    public long LibrariesCount { get; set; }
    public long BooksCount { get; set; }
    public long LoanRequestsCount { get; set; }
}
