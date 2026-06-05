using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

[ApiController]
[Route("api/hallazgos")]
[Authorize]
public class HallazgoController : ControllerBase
{
    private readonly HallazgoService _service;

    public HallazgoController(HallazgoService service)
    {
        _service = service;
    }

    [HttpGet("auditoria/{auditoriaId:int}")]
    public async Task<IActionResult> ObtenerPorAuditoria(int auditoriaId)
    {
        var hallazgos = await _service.ObtenerPorAuditoriaAsync(auditoriaId);
        return Ok(hallazgos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var hallazgo = await _service.ObtenerPorIdAsync(id);
        return hallazgo is null ? NotFound() : Ok(hallazgo);
    }
}
