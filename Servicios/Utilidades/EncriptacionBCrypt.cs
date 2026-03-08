using BCrypt.Net; // Habilita el uso de la biblioteca BCrypt para encriptación de contraseñas
using System;

namespace ProyectoAula.Servicios.Utilidades
{
     public static class EncriptacionBCrypt
    {
        private const int CostoPorDefecto = 12; // Costo recomendado para BCrypt, balance entre seguridad y rendimiento
        public static string Encriptar(string valorOriginal, int costo = CostoPorDefecto)
        {
            if(string.IsNullOrEmpty(valorOriginal))
                throw new ArgumentException("El valor a encriptar no puede estar vacío.", nameof(valorOriginal));
            
            if(costo < 4 || costo > 31)
            throw new ArgumentOutOfRangeException(nameof(costo), "El costo de BCrypt debe estar entre 4 y 31. Recomendado: 10-15.");

            try
            {
                return BCrypt.Net.BCrypt.HashPassword(valorOriginal, costo);
            }
            catch(Exception excepcion)
            {
                throw new InvalidOperationException(
                    $"Error al generar hash BCrypt con costo {costo}:{excepcion.Message}", excepcion);               
            }
        }

        public static bool Verificar(string valorOriginal, string hashExistente)
        {
            if (string.IsNullOrWhiteSpace(valorOriginal)) throw new ArgumentException("El valor a verificar no puede estar vacio.", nameof(valorOriginal));
            if (string.IsNullOrWhiteSpace(hashExistente)) throw new ArgumentException("El hash existente no puede estar vacio.", nameof(hashExistente));

            try
            {
                return BCrypt.Net.BCrypt.Verify(valorOriginal, hashExistente);
            }
            catch (Exception)
            {
                return false; // Si ocurre un error al verificar, se asume que no coincide
            }
        }

        public static bool NecesitaRehaseo(string hashExistente, int costoDeseado = CostoPorDefecto)
        {
            if(string.IsNullOrWhiteSpace(hashExistente))
            return true;

            try
            {
                if (hashExistente.Length >=7 && hashExistente.StartsWith($"2"))
                {
                    string costoParte = hashExistente.Substring(4,2);
                    if(int.TryParse(costoParte,out int costoActual))
                    {
                        return costoActual < costoDeseado;
                    }
                }
                return true;
            }
            catch
            {
                return true; // Si ocurre un error al analizar el hash, se asume que necesita rehaseo
            }
        }
    }

}