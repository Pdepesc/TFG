using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MySql.Data.MySqlClient;

using OpenHardwareMonitor.Hardware;
using Renci.SshNet;
using System.IO;

namespace EvaluacionSistema
{
    class EvaluacionInicial
    {
        private static Computer miPc = new Computer() { CPUEnabled = true, FanControllerEnabled = true, GPUEnabled = true, HDDEnabled = true, MainboardEnabled = true, RAMEnabled = true };

        public static void Evaluacion(MySqlConnection conn, Properties properties)
        {
            if(EvaluacionHardware(conn, properties) && EvaluacionSoftware(conn, properties))
                properties.set("TipoEvaluacion", "Completa");
        }

        public static bool EvaluacionHardware(MySqlConnection conn, Properties properties)
        {
            MySqlTransaction sqltransaction = conn.BeginTransaction();

            try
            {
                //Añadir esta estación a la BBDD y obtener su ID
                string sql = "INSERT INTO estacion(Empresa, Modelo, VersionRegistro) VALUES (@empresa, @modelo, @version)";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@empresa", properties.get("Empresa"));
                cmd.Parameters.AddWithValue("@modelo", properties.get("Modelo"));
                cmd.Parameters.AddWithValue("@version", properties.get("VersionRegistro"));
                cmd.ExecuteNonQuery();

                long id = cmd.LastInsertedId;
                properties.set("IdEstacion", id.ToString());
                Console.WriteLine(id);

                //Actualizar componentes hardware 100 veces
                Console.WriteLine("Actualizando componentes hardware...");
                ActualizarHardware();
                Console.WriteLine("Actualizacion finalizada!");

                //Leer componenetes Hardware y guardarlos en la BBDD
                sql = LeerCompoenentes(id, miPc.Hardware);
                Console.WriteLine("Ejecutar la siguiente consulta: \r\n" + sql);
                Console.Read();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();

                sqltransaction.Commit();

                Console.WriteLine("Insercion en la BBDD realizada!");

                return true;                    
            }
            catch (MySqlException ex)
            {
                sqltransaction.Rollback();
                Console.WriteLine("Error: {0}", ex.ToString());
                return false;
            }
        }
        
        public static bool EvaluacionSoftware(MySqlConnection conn, Properties properties)
        {
            //Get version local
            int versionLocal = int.Parse(properties.get("VersionRegistro"));

            //Get version BBDD
            string query = "SELECT Version, UrlDescarga FROM Registro WHERE Modelo = @modelo";
            MySqlCommand cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@modelo", properties.get("Modelo"));

            MySqlDataReader rdr = cmd.ExecuteReader();

            int versionBD = rdr.GetInt32("Version");
            string url = rdr.GetString("UrlDescarga");
            
            rdr.Close();

            //Comparar versiones
            if(versionLocal != versionBD)
            {
                //Actualizar por FTP el fichero del registro local
                ActualizarRegistroSFTP(url);

                //Actualizar version en el fichero de props
                properties.set("VersionRegistro", versionBD.ToString());

                //Actualziar version BBDD de la estacion local
                query = "UPDATE Estacion SET VersionRegistro = @version WHERE ID = @id";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@version", versionBD);
                cmd.Parameters.AddWithValue("@id", properties.get("IdEstacion"));
                cmd.ExecuteNonQuery();
            }
            
            //Comprobar registro local con el del fichero
                //Abrir fichero con el registro
                //Comprobar registro s uno a uno...
            //Si [registros_modificados] == 0 -> return true
            //Sino -> Comprobar de nuevo hasta que se cumpla el primer Si (hacer el metodo recursivo)
            return false;
        }

        private static void ActualizarHardware()
        {
            miPc.Open();
            int contador = 0;
            while (contador++ != 101)
                ActualizarComponentes(miPc.Hardware); Thread.Sleep(100);
        }

        private static void ActualizarComponentes(IHardware[] hardwareCollection)
        {
            foreach (IHardware hardware in hardwareCollection)
            {
                hardware.Update();
                if (hardware.SubHardware.Length > 0) ActualizarComponentes(hardware.SubHardware);
            }
        }
        
        private static string LeerCompoenentes(long id, IHardware[] hardwareCollection)
        {
            string sql = "INSERT INTO hardware(ID_Estacion, Identificador, Componente, Sensor, Minimo, Maximo, Media, Ultimo) VALUES ";
            foreach (IHardware hardware in hardwareCollection)
            {
                foreach (ISensor sensor in hardware.Sensors)
                {
                    sql += "(" + id
                        + ", '" + sensor.Identifier + "'"
                        + ", '" + hardware.Name + "'"
                        + ", '" + sensor.Name + "'"
                        + ", '" + (float)(Math.Truncate((Convert.ToDouble(sensor.Min)) * 100.0) / 100.0) + "'"
                        + ", '" + (float)(Math.Truncate((Convert.ToDouble(sensor.Max)) * 100.0) / 100.0) + "'"
                        + ", '" + (float)(Math.Truncate((Convert.ToDouble(Media(sensor.Values))) * 100.0) / 100.0) + "'"
                        + ", '" + (float)(Math.Truncate((Convert.ToDouble(sensor.Values.ElementAt<SensorValue>(sensor.Values.Count() - 1).Value)) * 100.0) / 100.0) + "'"
                        + "), ";
                }
            }
            return sql.Remove(sql.LastIndexOf(","), 2);
        }

        private static float Media(IEnumerable<SensorValue> valores)
        {
            float suma = 0;
            foreach (SensorValue valor in valores)
            {
                suma += valor.Value;
            }
            return suma / valores.Count();
        }

        private static void ActualizarRegistroSFTP(String url)
        {
            String RemoteFileName = Program.work_dir + url;
            String LocalDestinationFilename = "Registro.xml";

            using (var sftp = new SftpClient(Program.host, Program.port_ftp, Program.user_sftp, Program.pass_sftp))
            {
                sftp.Connect();

                using (var file = File.OpenWrite(LocalDestinationFilename))
                {
                    sftp.DownloadFile(RemoteFileName, file);
                }

                sftp.Disconnect();
            }
        }
    }
}
