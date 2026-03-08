using Microsoft.Extensions.Configuration;
using System;
using ProyectoAula.Servicios.Abstracciones;

namespace ProyectoAula.Servicios.Conexion
{
    public class ProveedorConexion : IProveedorConexion
    {
        private readonly IConfiguration _configuration;

        public ProveedorConexion(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration), "La configuración no puede ser nula.");
        }
        public string ProveedorActual
        {
            get
            {
                var valor = _configuration.GetValue<string>("DatabaseProvider");
                return string.IsNullOrWhiteSpace(valor)? "SqlServer" : valor.Trim();
            }
        }
        public string ObtenerCadenaConexion()
        {
            string? cadena = _configuration.GetConnectionString(ProveedorActual);
            if(string.IsNullOrWhiteSpace(cadena))
            {
                throw new InvalidOperationException($"No se encontró una cadena de conexión para el proveedor '{ProveedorActual}'."+ 
                    $"Verificar que existe 'ConnectionStrings:{ProveedorActual}' en appsettings.json" +
                    $"y que 'DatabaseProvider' este configurado correctamente");
            }
            return cadena;            
        }
    }
}
