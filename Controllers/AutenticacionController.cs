using Microsoft.AspNetCore.Mvc;                      // Para el controlador y las acciones 
using Microsoft.Extensions.Options;                  // Para inyectar configuraciones (IOptions) 
using Microsoft.IdentityModel.Tokens;                // Para firmar y generar el token JWT 
using System.IdentityModel.Tokens.Jwt;               // Para manipular JWT 
using System.Security.Claims;                        // Para definir los claims dentro del token 
using System.Text;                                   // Para codificar la clave secreta 
using ProyectoAula.Modelos;                          // Para la clase ConfiguracionJwt 
using ProyectoAula.Servicios.Abstracciones;           // Para la interfaz IServicioCrud 

namespace ProyectoAul.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutenticacionController : ControllerBase
    {
        private readonly ConfiguracionJwt _configuracionJwt;
        private readonly IServicioCrud _servicioCrud;
        public AutenticacionController(
        IOptions<ConfiguracionJwt> opcionesJwt,
        IServicioCrud servicioCrud)
        {
            _configuracionJwt = opcionesJwt.Value;
            _servicioCrud = servicioCrud;
        }
        [HttpPost("token")]
        public async Task<IActionResult> GenerarToken([FromBody] CredencialesGenericas credenciales)
        {
            // ----------------------------------------------------- 
            // VALIDACIONES BÁSICAS DEL BODY 
            // ----------------------------------------------------- 
            if (string.IsNullOrWhiteSpace(credenciales.Tabla) ||
                string.IsNullOrWhiteSpace(credenciales.CampoUsuario) ||
                string.IsNullOrWhiteSpace(credenciales.CampoContrasena) ||
                string.IsNullOrWhiteSpace(credenciales.Usuario) ||
                string.IsNullOrWhiteSpace(credenciales.Contrasena))
            {
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Debe enviar tabla, campos y credenciales completas.",
                    ejemplo = new
                    {
                        tabla = "TablaDeUsuarios",
                        campoUsuario = "ejemploCampoUsuario",
                        campoContrasena = "ejemploCampoContrasena",
                        usuario = "ejemplo@correo.com",
                        contrasena = "admin123"
                    }
                });
            }
            var (codigo, mensaje) = await _servicioCrud.VerificarContrasenaAsync(
                credenciales.Tabla,
                null, // Esquema opcional 
                credenciales.CampoUsuario,
                credenciales.CampoContrasena,
                credenciales.Usuario,
                credenciales.Contrasena
            );
            if (codigo == 404)
                return NotFound(new { estado = 404, mensaje = "Usuario no encontrado." });

            if (codigo == 401)
                return Unauthorized(new { estado = 401, mensaje = "Contraseña incorrecta." });

            if (codigo != 200)
                return StatusCode(500, new { estado = 500, mensaje = "Error interno durante la verificación.", detalle = mensaje });
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, credenciales.Usuario),       // Nombre de usuario 
                new Claim("tabla", credenciales.Tabla),                 // Tabla usada para autenticación 
                new Claim("campoUsuario", credenciales.CampoUsuario)    // Campo de usuario utilizado 
            };
            var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracionJwt.Key)); 

            // Se especifica el algoritmo de firma HMAC-SHA256 
            var credencialesFirma = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256); 

            // Duración configurable del token (en minutos) 
            var duracion = _configuracionJwt.DuracionMinutos > 0 ? _configuracionJwt.DuracionMinutos : 60; 

            // Construcción del token con sus parámetros principales 
            var token = new JwtSecurityToken( 
                issuer: _configuracionJwt.Issuer,       // Emisor del token 
                audience: _configuracionJwt.Audience,   // Público autorizado 
                claims: claims,                         // Datos del usuario dentro del token 
                expires: DateTime.UtcNow.AddMinutes(duracion), // Fecha de expiración 
                signingCredentials: credencialesFirma   // Firma digital 
            );
            string tokenGenerado = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new 
            { 
                estado = 200, 
                mensaje = "Autenticación exitosa.", 
                usuario = credenciales.Usuario, 
                token = tokenGenerado, 
                expiracion = token.ValidTo 
            }); 
        }
        public class CredencialesGenericas 
    { 
        // Nombre de la tabla que contiene los usuarios (por ejemplo: "usuario", "vendedor", "cliente") 
        public string Tabla { get; set; } = string.Empty; 

        // Nombre del campo que almacena el identificador de usuario (por ejemplo: "email", "nombre", "login") 
        public string CampoUsuario { get; set; } = string.Empty; 

        // Nombre del campo que almacena la contraseña (por ejemplo: "clave", "password", "contrasena") 
        public string CampoContrasena { get; set; } = string.Empty; 

        // Valor del usuario que intenta autenticarse 
        public string Usuario { get; set; } = string.Empty; 

        // Contraseña enviada por el usuario (texto plano para comparar con hash en BD) 
        public string Contrasena { get; set; } = string.Empty; 

        } 
    }
}
