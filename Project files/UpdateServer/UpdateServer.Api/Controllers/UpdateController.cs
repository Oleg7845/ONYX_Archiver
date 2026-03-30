using Microsoft.AspNetCore.Mvc;
using UpdateServer.Application.DTOs;
using UpdateServer.Application.Services;

namespace UpdateServer.Api.Controllers;

/// <summary>
/// Provides API endpoints for application update management.
/// Acts as the primary interface for Update Agents to query version availability.
/// </summary>
[ApiController]
[Route("api/update")]
public class UpdateController : ControllerBase
{
    private readonly UpdateService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateController"/>.
    /// </summary>
    /// <param name="service">The application service handling update business logic.</param>
    public UpdateController(UpdateService service)
    {
        _service = service;
    }

    /// <summary>
    /// Checks for the availability of a newer version for a specific application and update channel.
    /// </summary>
    /// <param name="app">The unique identifier or name of the application.</param>
    /// <param name="version">The current version of the client application (e.g., "1.0.0").</param>
    /// <param name="channel">The update delivery channel (e.g., "stable", "beta", "dev"). Defaults to "stable".</param>
    /// <returns>
    /// An <see cref="UpdateCheckResponse"/> containing update metadata if a new version exists, 
    /// or information indicating the client is up to date.
    /// </returns>
    /// <response code="200">Successfully processed the update check request.</response>
    /// <response code="400">The request was malformed or the application identifier is invalid.</response>
    [HttpGet("check")]
    public async Task<IActionResult> Check(
        [FromQuery] string app,
        [FromQuery] string version,
        [FromQuery] string channel = "stable")
    {
        try
        {
            // Delegate the version comparison and metadata retrieval to the application layer.
            UpdateCheckResponse result = await _service.CheckAsync(app, version, channel);

            // Standard HTTP 200 OK for successful queries.
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Logically, business errors (e.g., "App not found") are returned as 400 BadRequest.
            // In a more advanced setup, consider using an ExceptionFilter to handle this globally.
            return BadRequest(new { error = ex.Message });
        }
    }
}