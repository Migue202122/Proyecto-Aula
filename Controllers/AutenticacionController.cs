using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ProyectoAula.Modelos;
using ProyectoAula.Servicios.Abstracciones;

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
                null,
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
                new Claim(ClaimTypes.Name, credenciales.Usuario),
                new Claim("tabla", credenciales.Tabla),
                new Claim("campoUsuario", credenciales.CampoUsuario)
            };
            var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracionJwt.Key));
            var credencialesFirma = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
            var duracion = _configuracionJwt.DuracionMinutos > 0 ? _configuracionJwt.DuracionMinutos : 60;
            var token = new JwtSecurityToken(
                issuer: _configuracionJwt.Issuer,
                audience: _configuracionJwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(duracion),
                signingCredentials: credencialesFirma
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
            public string Tabla { get; set; } = string.Empty;
            public string CampoUsuario { get; set; } = string.Empty;
            public string CampoContrasena { get; set; } = string.Empty;
            public string Usuario { get; set; } = string.Empty;
            public string Contrasena { get; set; } = string.Empty;

        }
    }
}
