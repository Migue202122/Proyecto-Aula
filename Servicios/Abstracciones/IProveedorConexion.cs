using System;
namespace ProyectoAula.Servicios.Abstracciones
{
    public interface IProveedorConexion
    {
        string ProveedorActual {get;}
        string ObtenerCadenaConexion();
    }
}