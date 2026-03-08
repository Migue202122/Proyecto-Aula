using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProyectoAula.Servicios.Abstracciones
{
    public interface IServicioCrud
    {
        Task<IReadOnlyList<Dictionary<string, object?>>> ListarAsync(string nombreTabla, string? esquema,int? limite);
        Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerPorClaveAsync(string nombreTabla, string? esquema, string nombreClave, string valor);
        Task<bool> CrearAsync(string nombreTabla,string? esquema, Dictionary<string, object?> datos, string? camposEncriptar = null);
        Task<int> ActualizarAsync(string nombreTabla, string? esquema, string nombreClave, string valorClave, Dictionary<string, object?> datos, string? camposEncriptar = null);
        Task<int> EliminarAsync(string nombreTabla,string? esquema,string nombreClave,string valorClave); 
        Task<(int codigo, string mensaje)> VerificarContrasenaAsync(string nombreTabla,string? esquema,string campoUsuario,string campoContrasena,string valorUsuario,string valorContrasena);
    }
}