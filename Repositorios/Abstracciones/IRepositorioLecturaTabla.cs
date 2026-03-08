using Microsoft.AspNetCore.Razor.TagHelpers;

//IRepositorioLecturaTabla.cs  Interfaz para repositorio de lectura de tablas
using System.Collections.Generic; //Habilita List<T> y Dictionary<TKey, TValue> para colecciones genéricas
using System.Threading.Tasks; //Habilita Task para programación asíncrona

namespace ProyectoAula.Repositorios.Abstracciones
{
    public interface IRepositorioLecturaTabla
    {
        Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerFilasAsync(string nombreTablla, string? esquema, int? limite);
        Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerPorClaveAsync(string nombreTabla, string? esquema, string nombreClave, string valor);
        Task<bool> CrearAsync(string nombreTabla, string? esquema, Dictionary<string, object?> datos, string? camposEncriptar = null);
        Task<int> ActualizarAsync(string nombreTabla, string? esquema, string nombreClave, string valorClave, Dictionary<string, object?> datos, string? camposEncriptar = null);
        Task<int> EliminarAsync(string nombreTabla, string? esquema, string nombreClave, string valorClave);
        
        Task<string?> ObtenerHashContrasenaAsync( string nombreTabla, string? esquema, string campoUsuario, string campoContraseña, string valorUsuario);
        Task<Dictionary<string, object?>> ObtenerDiagnosticoConexionAsync();

        
    }

}