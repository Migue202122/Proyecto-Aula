using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ProyectoAula.Repositorios.Abstracciones;

namespace ProyectoAula.Controllers
{
    [Route("api/diagnostico")]
    [ApiController]
    public class DiagnosticoController : ControllerBase
    {
        private readonly IRepositorioLecturaTabla _repositorio;
        private readonly ILogger<DiagnosticoController> _logger;
        private readonly IConfiguration _configuration;

        public DiagnosticoController(
            IRepositorioLecturaTabla repositorio,
            ILogger<DiagnosticoController> logger,
            IConfiguration configuration)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        [AllowAnonymous]
        [HttpGet("conexion")]
        public async Task<IActionResult> ObtenerDiagnosticoConexionAsync()
        {
            try
            {
                _logger.LogInformation("INICIO diagnóstico de conexión");
                var proveedorConfigurado = _configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
                var diagnostico = await _repositorio.ObtenerDiagnosticoConexionAsync();
                _logger.LogInformation(
                    "DIAGNÓSTICO exitoso - Proveedor: {Proveedor}",
                    proveedorConfigurado
                );
                return Ok(new
                {
                    estado = 200,
                    mensaje = "Diagnóstico de conexión obtenido exitosamente.",
                    servidor = diagnostico,
                    configuracion = new
                    {
                        proveedorConfigurado = proveedorConfigurado,
                        descripcion = "Proveedor configurado en appsettings.json"
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (NotImplementedException)
            {
                var proveedor = _configuration.GetValue<string>("DatabaseProvider") ?? "desconocido";
                _logger.LogWarning(
                    "Diagnóstico no implementado para proveedor: {Proveedor}",
                    proveedor
                );
                return StatusCode(501, new
                {
                    estado = 501,
                    mensaje = $"El diagnóstico de conexión no está implementado para el proveedor '{proveedor}'.",
                    detalle = "Esta funcionalidad aún no está disponible para este motor de base de datos.",
                    proveedorConfigurado = proveedor
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla en diagnóstico de conexión"
                );
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno al obtener diagnóstico de conexión.",
                    detalle = excepcionGeneral.Message,
                    tipoError = excepcionGeneral.GetType().Name,
                    timestamp = DateTime.UtcNow,
                    sugerencia = "Revise los logs del servidor para más detalles."
                });
            }
        }
    }
}

