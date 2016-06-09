using MySql.Data.MySqlClient;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvaluacionSistema
{
    class EvaluacionRegistro
    {
        public static bool EvaluacionInicial(MySqlConnection conn, Properties properties)
        {
            Console.WriteLine("Iniciando evaluacion inicial del Registro...");

            Console.WriteLine("Comprobando version del registro...");

            //Get version local
            int versionLocal = int.Parse(properties.get("VersionRegistro"));

            Console.WriteLine("Version local: " + versionLocal);

            //Get version BBDD
            string query = "SELECT Version, UrlDescarga FROM Registro WHERE Modelo = @modelo";
            MySqlCommand cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@modelo", properties.get("Modelo"));

            MySqlDataReader rdr = cmd.ExecuteReader();

            rdr.Read();

            int versionBD = rdr.GetInt32("Version");
            string url = rdr.GetString("UrlDescarga");

            rdr.Close();

            Console.WriteLine("Version BBDD: " + versionBD);

            //Comparar versiones
            if (versionLocal != versionBD)
            {
                Console.WriteLine("Actualizando el fichero del registro...");

                //Actualizar por FTP el fichero del registro local
                SFTPManager.Download(url, "Registro.xml");

                //Actualizar version en el fichero de props
                properties.set("VersionRegistro", versionBD.ToString());

                //Actualziar version BBDD de la estacion local
                query = "UPDATE Estacion SET VersionRegistro = @version WHERE ID = @id";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@version", versionBD.ToString());
                cmd.Parameters.AddWithValue("@id", properties.get("IdEstacion"));
                cmd.ExecuteNonQuery();

                Console.WriteLine("Registro actualizado!");
            }

            //Comprobar registro local con el del fichero
            //Abrir fichero con el registro
            //Comprobar registro s uno a uno...
            //Si [registros_modificados] == 0 -> return true
            //Sino -> Comprobar de nuevo hasta que se cumpla el primer Si (hacer el metodo recursivo)

            Console.WriteLine("Evaluacion inicial del Registro finalizada!");
            return true;
        }

        //Metodo de comprobación del registro (local vs fichero xml) que devuelva
            //- bool
            //- lista de registros que han sido corregidos
    }
}
