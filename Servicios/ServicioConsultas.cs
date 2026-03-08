using System; 
using System.Collections.Generic; 
using System.Threading.Tasks; 
using System.Data; 
using System.Text.Json; 
using System.Text.RegularExpressions; 
using Microsoft.Extensions.Configuration; 
using ProyectoAula.Servicios.Abstracciones; 
using ProyectoAula.Repositorios.Abstracciones; 

namespace ProyectoAula.Servicios 
{ 
    public sealed class ServicioConsultas : IServicioConsultas 
    { 
        private readonly IRepositorioConsultas _repositorioConsultas; 
        private readonly IConfiguration _configuration; 

        public ServicioConsultas(IRepositorioConsultas repositorioConsultas, IConfiguration configuration) 
        { 
            _repositorioConsultas = repositorioConsultas ?? throw new ArgumentNullException( 
                nameof(repositorioConsultas), 
                "IRepositorioConsultas no puede ser null. Verificar registro en Program.cs."); 

            _configuration = configuration ?? throw new ArgumentNullException( 
                nameof(configuration), 
                "IConfiguration no puede ser null. Problema en configuración de ASP.NET Core."); 
        }
        public (bool esValida, string? mensajeError) ValidarConsultaSQL(string consulta, string[] tablasProhibidas) 
        { 
            if (string.IsNullOrWhiteSpace(consulta)) 
                return (false, "La consulta no puede estar vacía."); 

            string consultaNormalizada = consulta.Trim().ToUpperInvariant(); 

            if (!consultaNormalizada.StartsWith("SELECT") && !consultaNormalizada.StartsWith("WITH")) 
                return (false, "Solo se permiten consultas SELECT y WITH por motivos de seguridad."); 

            foreach (var tabla in tablasProhibidas) 
            { 
                if (consulta.Contains(tabla, StringComparison.OrdinalIgnoreCase)) 
                    return (false, $"La consulta intenta acceder a la tabla prohibida '{tabla}'."); 
            } 

            return (true, null); 
        }
        private Dictionary<string, object?> ConvertirParametrosDesdeJson(Dictionary<string, object?>? parametros) 
        { 
            var parametrosGenericos = new Dictionary<string, object?>(); 

            if (parametros == null) return parametrosGenericos; 

            foreach (var parametro in parametros) 
            { 
                string nombre = parametro.Key.StartsWith("@") ? parametro.Key : "@" + parametro.Key; 

                if (!Regex.IsMatch(nombre, @"^@\w+$")) 
                    throw new ArgumentException($"Nombre de parámetro inválido: {nombre}"); 

                object? valorTipado; 

                if (parametro.Value == null) 
                { 
                    valorTipado = null; 
                } 
                else if (parametro.Value is JsonElement json) 
                { 
                    valorTipado = json.ValueKind switch 
                    { 
                        JsonValueKind.String => DetectarTipoDesdeString(json.GetString()), 
                        JsonValueKind.Number => DetectarTipoNumerico(json), 
                        JsonValueKind.True => true, 
                        JsonValueKind.False => false, 
                        JsonValueKind.Null => null, 
                        JsonValueKind.Array => json.GetRawText(), 
                        JsonValueKind.Object => json.GetRawText(), 
                        JsonValueKind.Undefined => null, 
                        _ => json.GetRawText() ?? "" 
                    }; 
                } 
                else 
                { 
                    valorTipado = parametro.Value; 
                } 

                parametrosGenericos[nombre] = valorTipado; 
            } 

            return parametrosGenericos; 
        } 

        private object DetectarTipoDesdeString(string? valor) 
        { 
            if (string.IsNullOrEmpty(valor)) 
                return valor ?? ""; 

            if (DateTime.TryParse(valor, out DateTime fechaValor)) 
                return fechaValor; 

            if (int.TryParse(valor, out int intValor)) 
                return intValor; 

            if (long.TryParse(valor, out long longValor)) 
                return longValor; 

            if (double.TryParse(valor, out double doubleValor)) 
                return doubleValor; 

            if (bool.TryParse(valor, out bool boolValor)) 
                return boolValor; 

            if (Guid.TryParse(valor, out Guid guidValor)) 
                return guidValor; 

            return valor; 
        } 

        private object DetectarTipoNumerico(JsonElement element) 
        { 
            if (element.TryGetInt32(out int intVal)) 
                return intVal; 
            if (element.TryGetInt64(out long longVal)) 
                return longVal; 
            if (element.TryGetDouble(out double doubleVal)) 
                return doubleVal; 

            return element.ToString() ?? ""; 
        }
        public async Task<DataTable> EjecutarConsultaParametrizadaAsync( 
            string consulta, 
            Dictionary<string, object?> parametros, 
            int maximoRegistros, 
            string? esquema) 
        { 
            var tablasProhibidas = _configuration.GetSection("TablasProhibidas").Get<string[]>() ?? Array.Empty<string>(); 
            var (esConsultaValida, mensajeError) = ValidarConsultaSQL(consulta, tablasProhibidas); 

            if (!esConsultaValida) 
                throw new UnauthorizedAccessException(mensajeError ?? "Consulta no autorizada."); 

            return await _repositorioConsultas.EjecutarConsultaParametrizadaConDictionaryAsync( 
                consulta, parametros, maximoRegistros, esquema); 
        } 

        public async Task<DataTable> EjecutarConsultaParametrizadaAsync( 
            string consulta, 
            List<Microsoft.Data.SqlClient.SqlParameter> parametros, 
            int maximoRegistros, 
            string? esquema) 
        { 
            var parametrosGenericos = new Dictionary<string, object?>(); 
            foreach (var param in parametros) 
                parametrosGenericos[param.ParameterName] = param.Value; 

            return await EjecutarConsultaParametrizadaAsync(consulta, parametrosGenericos, maximoRegistros, esquema); 
        } 

        public async Task<DataTable> EjecutarConsultaParametrizadaDesdeJsonAsync( 
            string consulta, 
            Dictionary<string, object?>? parametros) 
        { 
            var parametrosGenericos = ConvertirParametrosDesdeJson(parametros); 
            return await EjecutarConsultaParametrizadaAsync(consulta, parametrosGenericos, 10000, null); 
        }
        public async Task<DataTable> EjecutarProcedimientoAlmacenadoAsync( 
            string nombreSP, 
            Dictionary<string, object?>? parametros, 
            List<string>? camposAEncriptar) 
        { 
            if (string.IsNullOrWhiteSpace(nombreSP)) 
                throw new ArgumentException("El nombre del procedimiento almacenado no puede estar vacío.", nameof(nombreSP)); 

            var parametrosGenericos = ConvertirParametrosConEncriptacion(parametros, camposAEncriptar); 

            return await _repositorioConsultas.EjecutarProcedimientoAlmacenadoConDictionaryAsync(nombreSP,parametrosGenericos); 
        } 

        private Dictionary<string, object?> ConvertirParametrosConEncriptacion( 
            Dictionary<string, object?>? parametros, 
            List<string>? camposAEncriptar) 
        { 
            var parametrosGenericos = ConvertirParametrosDesdeJson(parametros); 

            if (camposAEncriptar != null && camposAEncriptar.Count > 0) 
            { 
                foreach (var campo in camposAEncriptar) 
                { 
                    var claveParametro = campo.StartsWith("@") ? campo : "@" + campo; 
                    var campoSinArroba = campo.StartsWith("@") ? campo.Substring(1) : campo; 

                    // CASO 1: Campo directo en el diccionario de parámetros 
                    if (parametrosGenericos.ContainsKey(claveParametro)) 
                    { 
                        var valor = parametrosGenericos[claveParametro]; 

                        // IMPORTANTE: Convertir cualquier valor a string para encriptar 
                        string? valorTexto = null; 
                        if (valor != null && valor != DBNull.Value) 
                        { 
                            valorTexto = valor.ToString(); 
                        } 

                        if (!string.IsNullOrWhiteSpace(valorTexto) && !valorTexto.StartsWith("$2")) 
                        { 
                            parametrosGenericos[claveParametro] = BCrypt.Net.BCrypt.HashPassword(valorTexto); 
                        } 
                    } 
                    // CASO 2: Campo dentro de parámetros JSON (p_maestro, p_detalles, etc.) 
                    else 
                    { 
                        EncriptarCampoEnParametrosJson(parametrosGenericos, campoSinArroba); 
                    } 
                } 
            } 

            return parametrosGenericos; 
        }
        private void EncriptarCampoEnParametrosJson(Dictionary<string, object?> parametros, string nombreCampo) 
        { 
            // Lista de parámetros que pueden contener JSON con campos a encriptar 
            // Soporta PostgreSQL (p_maestro, p_detalles), SQL Server (maestro_json, detalles_json) y MySQL (p_maestro_json, p_detalles_json) 
            var parametrosJson = new[] { "@p_maestro", "@p_detalles", "@maestro_json", "@detalles_json", "@p_maestro_json","@p_detalles_json" }; 

            foreach (var paramKey in parametrosJson) 
            { 
                if (!parametros.ContainsKey(paramKey)) continue; 

                var valorParam = parametros[paramKey]; 
                if (valorParam == null || valorParam == DBNull.Value) continue; 

                var jsonString = valorParam.ToString(); 
                if (string.IsNullOrWhiteSpace(jsonString)) continue; 

                try 
                { 
                    // Intentar parsear como objeto JSON 
                    if (jsonString.TrimStart().StartsWith("{")) 
                    { 
                        var jsonObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonString); 
                        if (jsonObj != null && jsonObj.ContainsKey(nombreCampo)) 
                        { 
                            var valorCampo = jsonObj[nombreCampo]?.ToString(); 
                            if (!string.IsNullOrWhiteSpace(valorCampo) && !valorCampo.StartsWith("$2")) 
                            { 
                                jsonObj[nombreCampo] = BCrypt.Net.BCrypt.HashPassword(valorCampo); 
                                parametros[paramKey] = System.Text.Json.JsonSerializer.Serialize(jsonObj); 
                            } 
                        } 
                    } 
                    // Intentar parsear como array JSON 
                    else if (jsonString.TrimStart().StartsWith("[")) 
                    { 
                        var jsonArray = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(jsonString); 
                        if (jsonArray != null) 
                        { 
                            bool modificado = false; 
                            foreach (var item in jsonArray) 
                            { 
                                if (item.ContainsKey(nombreCampo)) 
                                { 
                                    var valorCampo = item[nombreCampo]?.ToString(); 
                                    if (!string.IsNullOrWhiteSpace(valorCampo) && !valorCampo.StartsWith("$2")) 
                                    { 
                                        item[nombreCampo] = BCrypt.Net.BCrypt.HashPassword(valorCampo); 
                                        modificado = true; 
                                    } 
                                } 
                            } 
                            if (modificado) 
                            { 
                                parametros[paramKey] = System.Text.Json.JsonSerializer.Serialize(jsonArray); 
                            } 
                        } 
                    } 
                } 
                catch (System.Text.Json.JsonException) 
                { 
                    // Si no es JSON válido, ignorar y continuar 
                } 
            } 
        } 
    } 
}





