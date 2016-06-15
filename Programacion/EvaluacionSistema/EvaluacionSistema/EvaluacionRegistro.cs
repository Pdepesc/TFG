using Microsoft.Win32;
using MySql.Data.MySqlClient;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace EvaluacionSistema
{
    class EvaluacionRegistro
    {
        #region EvaluacionInicial

        public static bool EvaluacionInicial(MySqlConnection conn, Properties properties)
        {
            try {
                Console.WriteLine("---Registro---\r\n");

                Console.Write("\tComprobando version del registro... ");

                ComprobarVersionRegistro(conn, properties);

                Console.WriteLine("Version del registro comprobada!");

                Console.Write("\tComprobando valores del registro... ");

                int[] fallos = ComprobarContenidoRegistro();

                Console.WriteLine("Valores del registro comprobados!\r\n");

                if (fallos[0] > 0)
                    Console.WriteLine("\t- " + fallos[0] + " registros erroneos corregidos!");
                if (fallos[1] > 0)
                    Console.WriteLine("\t- " + fallos[1] + " registros erroneos no se han podido corregir!");
                
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }
        
        private static int[] ComprobarContenidoRegistro()
        {
            XmlDocument xml = new XmlDocument();
            xml.Load("Registro.xml");
            XmlNodeList value_nodes = xml.GetElementsByTagName("v");

            String path, nombre, valor, tipo;
            int fallosCorregidos = 0;
            int fallosNoCorregidos = 0;

            foreach(XmlNode nodo in value_nodes)
            {
                path = GetPath(nodo);
                nombre = nodo.Attributes.Item(0).Value;
                valor = nodo.Attributes.Item(1).Value;
                tipo = nodo.Attributes.Item(2).Value;
                try {
                    if (FalloRegistro(path, nombre, valor, tipo)) fallosCorregidos++;
                }
                catch (Exception e)
                {
                    fallosNoCorregidos++;
                }
            }
            return new int[] { fallosCorregidos, fallosNoCorregidos};
        }

        #endregion EvaluacionInicial

        #region EvaluacionCompleta

        public static List<String[]>[] EvaluacionCompleta(MySqlConnection conn, Properties properties)
        {
            try
            {
                Console.WriteLine("---Registro---\r\n");

                Console.Write("\tComprobando version del registro... ");

                ComprobarVersionRegistro(conn, properties);

                Console.WriteLine("Version del registro comprobada!");

                Console.Write("\tComprobando valores del registro... ");
                
                return EvaluarContenidoRegistro();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        private static List<String[]>[] EvaluarContenidoRegistro()
        {
            List<String[]> registrosCorregidos = new List<String[]>();
            List<String[]> registrosNoCorregidos = new List<String[]>();

            XmlDocument xml = new XmlDocument();
            xml.Load("Registro.xml");
            XmlNodeList value_nodes = xml.GetElementsByTagName("v");

            String path, nombre, valor, tipo;

            foreach (XmlNode nodo in value_nodes)
            {
                path = GetPath(nodo);
                nombre = nodo.Attributes.Item(0).Value;
                valor = nodo.Attributes.Item(1).Value;
                tipo = nodo.Attributes.Item(2).Value;
                try
                {
                    if (FalloRegistro(path, nombre, valor, tipo))
                        registrosCorregidos.Add(new String[] { path, nombre });
                }
                catch (Exception e)
                {
                    registrosNoCorregidos.Add(new String[] { path, nombre });
                }
            }
            Console.WriteLine("Valores del registro comprobados!\r\n");
            return new List<String[]>[] { registrosCorregidos, registrosNoCorregidos };
        }

        #endregion EvaluacionCompleta

        #region PostEvaluacion

        public static void PostEvaluacion(List<String[]>[] fallosRegistro)
        {
            Console.WriteLine("PostEvaluacion de Registro....");

            String path = "Informes/InformeRegistro-" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + ".txt";
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine("Registros erroneos que se han corregido"); sw.WriteLine();

                foreach(String[] registro in fallosRegistro[0])
                {
                    sw.WriteLine(registro[0] + "\\" + registro[1]);
                }

                sw.WriteLine(); sw.WriteLine("Registros erroneos que no se han podido corregir"); sw.WriteLine();

                foreach (String[] registro in fallosRegistro[1])
                {
                    sw.WriteLine(registro[0] + "\\" + registro[1]);
                }
            }
            
            SFTPManager.Upload("Informes/", path); Console.WriteLine("Informe enviado!");
        }

        #endregion PostEvaluacion

        #region Utilidad

        private static void ComprobarVersionRegistro(MySqlConnection conn, Properties properties)
        {
            int versionLocal = int.Parse(properties.get("VersionRegistro"));

            string query = "SELECT Version, UrlDescarga FROM Registro WHERE Modelo = @modelo";
            MySqlCommand cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@modelo", properties.get("Modelo"));

            MySqlDataReader rdr = cmd.ExecuteReader();

            rdr.Read();

            int versionBD = rdr.GetInt32("Version");
            string url = rdr.GetString("UrlDescarga");

            rdr.Close();

            if (versionLocal != versionBD)
            {
                Console.WriteLine("\t\tActualizando el fichero del registro...");

                SFTPManager.Download(url, "Registro.xml");

                properties.set("VersionRegistro", versionBD.ToString());

                query = "UPDATE Estacion SET VersionRegistro = @version WHERE ID = @id";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@version", versionBD.ToString());
                cmd.Parameters.AddWithValue("@id", properties.get("IdEstacion"));
                cmd.ExecuteNonQuery();

                Console.WriteLine("\t\tFichero del registro actualizado!");
            }
        }

        private static bool FalloRegistro(String path, String nombre, String valor, String tipo)
        {
            Object registro = Registry.GetValue(path, nombre, null);
            switch (tipo)
            {
                case "String":
                    if (valor.CompareTo(Util.SanitizeXmlString((String)registro)) != 0)
                    {
                        Registry.SetValue(path, nombre, valor, RegistryValueKind.String);
                        return true;
                    }
                    break;
                case "ExpandString":
                    if (Environment.ExpandEnvironmentVariables(valor).CompareTo(Util.SanitizeXmlString((String)registro)) != 0)
                    {
                        Registry.SetValue(path, nombre, valor, RegistryValueKind.ExpandString);
                        return true;
                    }
                    break;
                case "MultiString":
                    if (valor.CompareTo(String.Join(" ", (String[])registro)) != 0)
                    {
                        Registry.SetValue(path, nombre, valor.Split(' '), RegistryValueKind.MultiString);
                        return true;
                    }
                    break;
                case "Binary":
                    if (valor.CompareTo(Convert.ToBase64String((byte[])registro)) != 0)
                    {
                        Registry.SetValue(path, nombre, Convert.FromBase64String(valor), RegistryValueKind.Binary);
                        return true;
                    }
                    break;
                case "None":
                    if (valor.CompareTo(Convert.ToBase64String((byte[])registro)) != 0)
                    {
                        Registry.SetValue(path, nombre, Convert.FromBase64String(valor), RegistryValueKind.None);
                        return true;
                    }
                    break;
                case "DWord":
                    if (valor.CompareTo(((Int32)registro).ToString()) != 0)
                    {
                        Registry.SetValue(path, nombre, Int32.Parse(valor), RegistryValueKind.DWord);
                        return true;
                    }
                    break;
                case "QWord":
                    if (valor.CompareTo(((Int64)registro).ToString()) != 0)
                    {
                        Registry.SetValue(path, nombre, Int64.Parse(valor), RegistryValueKind.QWord);
                        return true;
                    }
                    break;
            }
            return false;
        }

        private static String GetPath(XmlNode nodo)
        {
            nodo = nodo.ParentNode;
            String path = nodo.Attributes.Item(0).Value;
            while (nodo.ParentNode.Name.CompareTo("registro") != 0)
            {
                nodo = nodo.ParentNode;
                path = nodo.Attributes.Item(0).Value + "\\" + path;
            }
            return path;
        }

        #endregion Utilidad
    }
}
