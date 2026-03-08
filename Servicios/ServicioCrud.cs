using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProyectoAula.Servicios.Abstracciones;
using ProyectoAula.Repositorios.Abstracciones;


namespace ProyectoAula.Servicios
{
    public class ServicioCrud : IServicioCrud
    {
        private readonly IPoliticaTablasProhibidas _politicaTablasProhibidas;
        private readonly IRepositorioLecturaTabla _repositorioLectura;
          public ServicioCrud( 
            IRepositorioLecturaTabla repositorioLectura, 
            IPoliticaTablasProhibidas politicaTablasProhibidas) 
        {  
            _repositorioLectura = repositorioLectura ?? throw new ArgumentNullException( 
                nameof(repositorioLectura), 
                "IRepositorioLecturaTabla no puede ser null. " + 
                "Verificar que esté registrado en Program.cs con AddScoped<IRepositorioLecturaTabla, ...>()" 
            ); 
 
            _politicaTablasProhibidas = politicaTablasProhibidas ?? throw new ArgumentNullException( 
                nameof(politicaTablasProhibidas), 
                "IPoliticaTablasProhibidas no puede ser null. " + 
                "Verificar que esté registrado en Program.cs con AddSingleton<IPoliticaTablasProhibidas,PoliticaTablasProhibidasDesdeJson>()");


         }
          public async Task<IReadOnlyList<Dictionary<string, object?>>> ListarAsync(string nombreTabla,string? esquema, int? limite )
        {
             if (string.IsNullOrWhiteSpace(nombreTabla)) 
                throw new ArgumentException( 
                    "El nombre de la tabla no puede estar vacío.", 
                    nameof(nombreTabla) 
                );      
                 if (!_politicaTablasProhibidas.EsTablaPermitida(nombreTabla)) 
            { 
                throw new UnauthorizedAccessException( 
                    $"Acceso denegado: La tabla '{nombreTabla}' está restringida y no puede ser consultada. " + 
                    $"Verifique los permisos de acceso o contacte al administrador del sistema." 
                );
            }  
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim(); 
            int? limiteNormalizado = (limite is null || limite <= 0) ? null : limite;
            var filas = await _repositorioLectura.ObtenerFilasAsync(nombreTabla, esquemaNormalizado, limiteNormalizado);
            return filas;
        }
          public async Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerPorClaveAsync( 
            string nombreTabla, 
            string? esquema, 
            string nombreClave, 
            string valor 
        ) 
        { 
           if (string.IsNullOrWhiteSpace(nombreClave)) 
                throw new ArgumentException("El nombre de la clave no puede estar vacío.", nameof(nombreClave)); 
 
            if (string.IsNullOrWhiteSpace(valor)) 
                throw new ArgumentException("El valor no puede estar vacío.", nameof(valor)); 
 
            // VALIDACIÓN DE TABLAS PROHIBIDAS (usando la política inyectada) 
            if (!_politicaTablasProhibidas.EsTablaPermitida(nombreTabla)) 
                throw new UnauthorizedAccessException( 
                    $"Acceso denegado: La tabla '{nombreTabla}' está restringida y no puede ser consultada." 
                ); 
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim(); 
            string nombreClaveNormalizado = nombreClave.Trim(); 
            string valorNormalizado = valor.Trim(); 
             var filas = await _repositorioLectura.ObtenerPorClaveAsync( 
                nombreTabla, 
                esquemaNormalizado, 
                nombreClaveNormalizado, 
                valorNormalizado 
            ); 
            return filas;
         }
          public async Task<bool> CrearAsync( 
            string nombreTabla, 
            string? esquema, 
            Dictionary<string, object?> datos, 
            string? camposEncriptar = null 
        ) 
        { 
            // FASE 1: VALIDACIONES DE REGLAS DE NEGOCIO 
            if (string.IsNullOrWhiteSpace(nombreTabla)) 
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla)); 
 
            if (datos == null || !datos.Any()) 
                throw new ArgumentException("Los datos no pueden estar vacíos.", nameof(datos)); 
 
            // VALIDACIÓN DE TABLAS PROHIBIDAS (usando la política inyectada) 
            if (!_politicaTablasProhibidas.EsTablaPermitida(nombreTabla)) 
                throw new UnauthorizedAccessException( 
                    $"Acceso denegado: La tabla '{nombreTabla}' está restringida y no puede ser modificada." 
                ); 
 
            // FASE 2: NORMALIZACIÓN DE PARÁMETROS 
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim(); 
            string? camposEncriptarNormalizados = string.IsNullOrWhiteSpace(camposEncriptar) ? null : camposEncriptar.Trim(); 
            return await _repositorioLectura.CrearAsync( 
                nombreTabla, 
                esquemaNormalizado, 
                datos, 
                camposEncriptarNormalizados 
            ); 
        } 
         public async Task<int> ActualizarAsync( 
            string nombreTabla, 
            string? esquema, 
            string nombreClave, 
            string valorClave, 
            Dictionary<string, object?> datos, 
            string? camposEncriptar = null 
        ) 
        { 
            // FASE 1: VALIDACIONES DE REGLAS DE NEGOCIO 
            if (string.IsNullOrWhiteSpace(nombreTabla)) 
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla)); 
 
            if (string.IsNullOrWhiteSpace(nombreClave)) 
                throw new ArgumentException("El nombre de la clave no puede estar vacío.", nameof(nombreClave)); 
 
            if (string.IsNullOrWhiteSpace(valorClave)) 
                throw new ArgumentException("El valor de la clave no puede estar vacío.", nameof(valorClave)); 
 
            if (datos == null || !datos.Any()) 
                throw new ArgumentException("Los datos a actualizar no pueden estar vacíos.", nameof(datos)); 
 
            // VALIDACIÓN DE TABLAS PROHIBIDAS (usando la política inyectada) 
            if (!_politicaTablasProhibidas.EsTablaPermitida(nombreTabla)) 
                throw new UnauthorizedAccessException( 
                    $"Acceso denegado: La tabla '{nombreTabla}' está restringida y no puede ser modificada." 
                ); 
 
            // FASE 2: NORMALIZACIÓN DE PARÁMETROS 
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim(); 
            string nombreClaveNormalizado = nombreClave.Trim(); 
            string valorClaveNormalizado = valorClave.Trim(); 
            string? camposEncriptarNormalizados = string.IsNullOrWhiteSpace(camposEncriptar) ? null : camposEncriptar.Trim(); 
 
            // VALIDACIONES ADICIONALES DE NEGOCIO PARA ACTUALIZACIÓN (futuras expansiones) 
            // Aquí se pueden agregar: 
            // - Validación de permisos específicos para actualización 
            // - Reglas de negocio sobre qué campos se pueden actualizar 
            // - Validaciones de integridad de datos 
            // - Auditoría de cambios sensibles 
 
            // FASE 3: DELEGACIÓN AL REPOSITORIO (aplicando DIP) 
            return await _repositorioLectura.ActualizarAsync( 
                nombreTabla, 
                esquemaNormalizado, 
                nombreClaveNormalizado, 
                valorClaveNormalizado, 
                datos, 
                camposEncriptarNormalizados 
            ); 
        } 
         public async Task<int> EliminarAsync( 
            string nombreTabla, 
            string? esquema, 
            string nombreClave, 
            string valorClave 
        ) 
        { 
            // FASE 1: VALIDACIONES DE REGLAS DE NEGOCIO 
            if (string.IsNullOrWhiteSpace(nombreTabla)) 
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla)); 
 
            if (string.IsNullOrWhiteSpace(nombreClave)) 
                throw new ArgumentException("El nombre de la clave no puede estar vacío.", nameof(nombreClave)); 
 
            if (string.IsNullOrWhiteSpace(valorClave)) 
                throw new ArgumentException("El valor de la clave no puede estar vacío.", nameof(valorClave)); 
 
            // VALIDACIÓN DE TABLAS PROHIBIDAS (usando la política inyectada) 
            if (!_politicaTablasProhibidas.EsTablaPermitida(nombreTabla)) 
                throw new UnauthorizedAccessException( 
                    $"Acceso denegado: La tabla '{nombreTabla}' está restringida y no puede ser modificada." 
                ); 
 
            // FASE 2: NORMALIZACIÓN DE PARÁMETROS 
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim(); 
            string nombreClaveNormalizado = nombreClave.Trim(); 
            string valorClaveNormalizado = valorClave.Trim(); 
 
            // VALIDACIONES ADICIONALES DE NEGOCIO PARA ELIMINACIÓN (futuras expansiones) 
            // Aquí se pueden agregar: 
            // - Validación de permisos específicos para eliminación (más restrictivos) 
            // - Reglas de negocio sobre eliminación en cascada 
            // - Validaciones de integridad referencial antes de eliminar 
            // - Auditoría de eliminaciones sensibles 
            // - Soft delete vs hard delete según reglas del negocio 
 
            // FASE 3: DELEGACIÓN AL REPOSITORIO (aplicando DIP) 
            return await _repositorioLectura.EliminarAsync( 
                nombreTabla, 
                esquemaNormalizado, 
                nombreClaveNormalizado, 
                valorClaveNormalizado 
            ); 
        }
         public async Task<(int codigo, string mensaje)> VerificarContrasenaAsync( 
            string nombreTabla, 
            string? esquema, 
            string campoUsuario, 
            string campoContrasena, 
            string valorUsuario, 
            string valorContrasena 
        ) 
        { 
            // FASE 1: VALIDACIONES DE REGLAS DE NEGOCIO 
            if (string.IsNullOrWhiteSpace(nombreTabla)) 
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla)); 
 
            if (string.IsNullOrWhiteSpace(campoUsuario)) 
                throw new ArgumentException("El campo de usuario no puede estar vacío.", nameof(campoUsuario)); 
 
            if (string.IsNullOrWhiteSpace(campoContrasena)) 
                throw new ArgumentException("El campo de contraseña no puede estar vacío.", nameof(campoContrasena)); 
 
            if (string.IsNullOrWhiteSpace(valorUsuario)) 
                throw new ArgumentException("El valor de usuario no puede estar vacío.", nameof(valorUsuario)); 
 
            if (string.IsNullOrWhiteSpace(valorContrasena)) 
                throw new ArgumentException("La contraseña no puede estar vacía.", nameof(valorContrasena)); 
 
            // VALIDACIÓN DE TABLAS PROHIBIDAS (usando la política inyectada) 
            if (!_politicaTablasProhibidas.EsTablaPermitida(nombreTabla)) 
                throw new UnauthorizedAccessException( 
                    $"Acceso denegado: La tabla '{nombreTabla}' está restringida y no puede ser consultada." 
                ); 
 
            // FASE 2: NORMALIZACIÓN DE PARÁMETROS 
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim(); 
            string campoUsuarioNormalizado = campoUsuario.Trim(); 
            string campoContrasenaNormalizado = campoContrasena.Trim(); 
            string valorUsuarioNormalizado = valorUsuario.Trim(); 
 
            try 
            { 
                // FASE 3: OBTENER HASH ALMACENADO DEL REPOSITORIO 
                string? hashAlmacenado = await _repositorioLectura.ObtenerHashContrasenaAsync( 
                    nombreTabla, 
                    esquemaNormalizado, 
                    campoUsuarioNormalizado, 
                    campoContrasenaNormalizado, 
                    valorUsuarioNormalizado 
                ); 
 
                // FASE 4: EVALUAR RESULTADO DE LA BÚSQUEDA 
                if (hashAlmacenado == null) 
                { 
                    // Usuario no encontrado en la base de datos 
                    return (404, "Usuario no encontrado"); 
                } 
 
                // FASE 5: VERIFICACIÓN DE CONTRASEÑA CON BCRYPT 
                // Usar nuestra clase de utilidad para verificación segura 
                bool contrasenaCorrecta = ProyectoAula.Servicios.Utilidades.EncriptacionBCrypt.Verificar( 
                    valorContrasena,  // Contraseña en texto plano proporcionada 
                    hashAlmacenado    // Hash BCrypt almacenado en base de datos 
                ); 
 
                if (contrasenaCorrecta) 
                { 
                    return (200, "Credenciales válidas"); 
                } 
                else 
                { 
                    return (401, "Contraseña incorrecta"); 
                } 
            } 
            catch (Exception excepcion) 
            { 
                // Re-lanzar excepción para que el controlador la maneje apropiadamente 
                throw new InvalidOperationException( 
                    $"Error durante la verificación de credenciales: {excepcion.Message}", 
                    excepcion 
                ); 
            } 
        }


    }
}
