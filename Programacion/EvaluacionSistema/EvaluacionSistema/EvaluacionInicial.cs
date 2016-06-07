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

        public static bool Evaluacion(MySqlConnection conn, Properties properties)
        {
            return EvaluacionHardware(conn, properties) && EvaluacionSoftware(conn, properties);
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
                //FTP.Download(url, Registro.xml);      Esto sería así si hago una clase FTPManager o algo del estilo

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
        
        public static void PruebaFTP()
        {
            /*
            PUEDE QUE SEA MEJOR HACER UNA CLASE FTPMANAGER O ALGO ASI,
            QUE TENGA LAS CONSTANTES DE CONEXION HOST, USERNAME, PASSWORD, PORT
            Y QUE TENGA DOS METODOS
                - DOWNLOAD(REMOTEFILENAME, LOCALDESTINATIONFILENAME)
                - UPLOAD(LOCALFILENAME, REMOTEDESTINATIONFILENAME)
            */
            String Host = "192.168.1.10";
            int Port = 22;
            String RemoteFileName = "/var/www/ejemplo.txt";
            String LocalDestinationFilename = "ejemplo.txt";
            String Username = "pi";
            String Password = "raspberry";

            using (var sftp = new SftpClient(Host, Port, Username, Password))
            {
                sftp.Connect();

                using (var file = File.OpenWrite(LocalDestinationFilename))
                {
                    sftp.DownloadFile(RemoteFileName, file);
                }

                sftp.Disconnect();
            }
            /*
            
            const int port = 22;
            const string host = "domainna.me";
            const string username = "chucknorris";
            const string password = "norrischuck";
            const string workingdirectory = "/highway/hell";
            const string uploadfile = @"c:yourfilegoeshere.txt";

            Console.WriteLine("Creating client and connecting");
            using (var client = new SftpClient(host, port, username, password))
            {
                client.Connect();
                Console.WriteLine("Connected to {0}", host);

                client.ChangeDirectory(workingdirectory);
                Console.WriteLine("Changed directory to {0}", workingdirectory);

                var listDirectory = client.ListDirectory(workingdirectory);
                Console.WriteLine("Listing directory:");
                foreach (var fi in listDirectory)
                {
                    Console.WriteLine(" - " + fi.Name);
                }

                using (var fileStream = new FileStream(uploadfile, FileMode.Open))
                {
                    Console.WriteLine("Uploading {0} ({1:N0} bytes)",
                                        uploadfile, fileStream.Length);
                    client.BufferSize = 4 * 1024; // bypass Payload error large files
                    client.UploadFile(fileStream, Path.GetFileName(uploadfile));
                }
            }
            */
        }
    }
}
