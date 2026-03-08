using Microsoft.Extensions.Configuration; 
using System; 
using System.Collections.Generic; 
using System.Linq; 
using ProyectoAula.Servicios.Abstracciones;

namespace ApigenericaCsharp.Servicios.Politicas
{
    public class PoliticaTablasProhibidasDesdeJson : IPoliticaTablasProhibidas
    {
        private readonly HashSet<string> _tablasProhibidas;
          public PoliticaTablasProhibidasDesdeJson(IConfiguration configuration) 
        { 
            if (configuration == null) 
                throw new ArgumentNullException( 
                    nameof(configuration), 
                    "IConfiguration no puede ser null." 
                ); 
 
            // Leer la sección "TablasProhibidas" del JSON 
            var tablasProhibidasArray = configuration.GetSection("TablasProhibidas") 
                .Get<string[]>() ?? Array.Empty<string>(); 
 
            // Convertir a HashSet con comparación case-insensitive 
            _tablasProhibidas = new HashSet<string>( 
                tablasProhibidasArray.Where(t => !string.IsNullOrWhiteSpace(t)), 
                StringComparer.OrdinalIgnoreCase 
            ); 
        }
          public bool EsTablaPermitida(string nombreTabla) 
        { 
            // Nombres vacíos no están permitidos 
            if (string.IsNullOrWhiteSpace(nombreTabla)) 
                return false; 
 
            // Retorna true si NO está en la lista prohibida 
            return !_tablasProhibidas.Contains(nombreTabla); 
        }
          public IReadOnlyCollection<string> ObtenerTablasProhibidas() 
        { 
            return _tablasProhibidas; 
        } 
          public bool TieneRestricciones() 
        { 
            return _tablasProhibidas.Count > 0; 
        } 
        
    }
}