using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProyectoAula.Servicios.Abstracciones;

namespace ProyectoAula.Controllers
{
    [Route("api/consultas")]
    [ApiController]
    public class ConsultasController : ControllerBase
    {

        private readonly IServicioConsultas _servicioConsultas;
        private readonly ILogger<ConsultasController> _logger;
        public ConsultasController(
            IServicioConsultas servicioConsultas,
            ILogger<ConsultasController> logger
        )
        {
            _servicioConsultas = servicioConsultas ?? throw new ArgumentNullException(
                nameof(servicioConsultas),
                "IServicioConsultas no fue inyectado correctamente. Verificar registro de servicios en Program.cs"
            );

            _logger = logger ?? throw new ArgumentNullException(
                nameof(logger),
                "ILogger no fue inyectado correctamente. Problema en configuración de logging de ASP.NET Core"
            );
        }
        [Authorize]
        [HttpPost("ejecutarconsultaparametrizada")]
        public async Task<IActionResult> EjecutarConsultaParametrizadaAsync([FromBody] Dictionary<string, object?> cuerpoSolicitud)
        {
            const int maximoRegistros = 10000;
            try
            {
                if (!cuerpoSolicitud.TryGetValue("consulta", out var consultaObj) || consultaObj is null)
                    return BadRequest("La consulta no puede estar vacía.");
                string consulta = consultaObj switch
                {
                    string texto => texto,
                    JsonElement json when json.ValueKind == JsonValueKind.String => json.GetString() ?? string.Empty,
                    _ => string.Empty
                };
                if (string.IsNullOrWhiteSpace(consulta))
                    return BadRequest("La consulta no puede estar vacía.");
                Dictionary<string, object?>? parametros = null;

                if (cuerpoSolicitud.TryGetValue("parametros", out var parametrosObj) &&
                    parametrosObj is JsonElement jsonParametros &&
                    jsonParametros.ValueKind == JsonValueKind.Object)
                {
                    parametros = new Dictionary<string, object?>();
                    foreach (var p in jsonParametros.EnumerateObject())
                    {
                        parametros[p.Name] = p.Value;
                    }
                }
                _logger.LogInformation(
                    "INICIO ejecución consulta SQL - Consulta: {Consulta}, Parámetros: {CantidadParametros}",
                    consulta.Length > 100 ? consulta.Substring(0, 100) + "..." : consulta,  // Truncar consultas muy largas en logs 
                    parametros?.Count ?? 0                                        // Cantidad de parámetros recibidos 
                );
                var resultado = await _servicioConsultas.EjecutarConsultaParametrizadaDesdeJsonAsync(consulta, parametros);
                var lista = new List<Dictionary<string, object?>>();
                foreach (DataRow fila in resultado.Rows)
                {
                    var filaDiccionario = resultado.Columns.Cast<DataColumn>()
                        .ToDictionary(
                            col => col.ColumnName.ToLower(),
                            col => fila[col] == DBNull.Value ? null : fila[col]
                        );
                    lista.Add(filaDiccionario);
                }
                _logger.LogInformation(
                    "ÉXITO ejecución consulta SQL - Registros obtenidos: {Cantidad}",
                    lista.Count
                );
                if (lista.Count == 0)
                {
                    _logger.LogInformation("SIN DATOS - Consulta ejecutada correctamente pero no devolvió registros");
                    return NotFound("La consulta se ejecutó correctamente pero no devolvió resultados.");
                }
                return Ok(new
                {
                    Resultados = lista,
                    Total = lista.Count,
                    Advertencia = lista.Count == maximoRegistros ?
                        $"Se alcanzó el límite de {maximoRegistros} registros." : null
                });
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                _logger.LogWarning(
                    "ACCESO DENEGADO - Consulta rechazada por políticas de seguridad: {Mensaje}",
                    excepcionAcceso.Message
                );
                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado por políticas de seguridad.",
                    detalle = excepcionAcceso.Message,
                    sugerencia = "Verifique que la consulta cumple con las políticas de seguridad configuradas"
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                _logger.LogWarning(
                    "PARÁMETROS INVÁLIDOS - Formato de entrada incorrecto: {Mensaje}",
                    excepcionArgumento.Message
                );
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Parámetros de entrada inválidos.",
                    detalle = excepcionArgumento.Message,
                    sugerencia = "Verifique el formato de la consulta y los nombres de parámetros"
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla inesperada ejecutando consulta SQL"
                );
                var detalleError = new System.Text.StringBuilder();
                detalleError.AppendLine($"Tipo de error: {excepcionGeneral.GetType().Name}");
                detalleError.AppendLine($"Mensaje: {excepcionGeneral.Message}");
                if (excepcionGeneral.InnerException != null)
                {
                    detalleError.AppendLine($"Error interno: {excepcionGeneral.InnerException.Message}");
                }
                if (!string.IsNullOrEmpty(excepcionGeneral.StackTrace))
                {
                    var stackLines = excepcionGeneral.StackTrace.Split('\n').Take(3);
                    detalleError.AppendLine("Stack trace:");
                    foreach (var line in stackLines)
                    {
                        detalleError.AppendLine($"  {line.Trim()}");
                    }
                }
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor al ejecutar consulta SQL.",
                    tipoError = excepcionGeneral.GetType().Name,
                    detalle = excepcionGeneral.Message,
                    detalleCompleto = detalleError.ToString(),
                    errorInterno = excepcionGeneral.InnerException?.Message,
                    timestamp = DateTime.UtcNow,
                    sugerencia = "Revise los logs del servidor para más detalles o contacte al administrador."
                });
            }
        }
    }
}



