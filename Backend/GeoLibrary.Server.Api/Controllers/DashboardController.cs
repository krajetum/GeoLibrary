using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GeoLibrary.Server.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class DashboardController : ControllerBase
{



}
