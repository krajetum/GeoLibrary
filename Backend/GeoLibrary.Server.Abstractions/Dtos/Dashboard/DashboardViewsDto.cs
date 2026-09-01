using GeoLibrary.Server.Abstractions.Dtos.Library;

namespace GeoLibrary.Server.Abstractions.Dtos.Dashboard;

/// <summary>
/// Serie storiche aggregate su tutta la piattaforma: le due serie condividono
/// lo stesso asse dei tempi, perché entrambe passano da StatsExtensions.FillStats.
/// </summary>
public class DashboardViewsDto
{
    public List<DateStats> LibraryViews { get; set; } = [];
    public List<DateStats> BookViews { get; set; } = [];
}
