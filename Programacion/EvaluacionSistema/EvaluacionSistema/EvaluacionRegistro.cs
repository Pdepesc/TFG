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

        public static bool EvaluacionInicial(MySqlConnection conn)
        {
            try
            {
                Console.WriteLine("---Registro---\r\n");

                Console.Write("\tComprobando version del registro... ");
                ComprobarVersionRegistro(conn);
                Console.WriteLine("Version del registro comprobada!");

                Console.Write("\tComprobando valores del registro... ");
                int[] fallos = ComprobarContenidoRegistro();
                Console.WriteLine("Valores del registro comprobados!\r\n");

                Console.WriteLine("\t{0} registros erróneos: {1} corregidos - {2} sin corregir", fallos[0] + fallos[1], fallos[0], fallos[1]);

                Console.WriteLine("\r\n---Registro/FIN---\r\n\r\n");

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\n\r\nError en la evaluacion inicial del registro: \r\n\t{0}", e.ToString());
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

            if(value_nodes.Count != 0)
            {
                foreach (XmlNode nodo in value_nodes)
                {
                    path = GetPath(nodo);
                    nombre = nodo.Attributes.Item(0).Value;
                    valor = nodo.Attributes.Item(1).Value;
                    tipo = nodo.Attributes.Item(2).Value;
                    try
                    {
                        if (FalloRegistro(path, nombre, valor, tipo)) fallosCorregidos++;
                    }
                    catch (Exception e)
                    {
                        fallosNoCorregidos++;
                    }
                }
            }
            
            return new int[] { fallosCorregidos, fallosNoCorregidos};
        }

        #endregion EvaluacionInicial

        #region EvaluacionCompleta

        public static int EvaluacionCompleta(MySqlConnection conn)
        {
            try
            {
                Console.WriteLine("---Registro---\r\n\r\n");

                Console.Write("\tComprobando version del registro... ");
                ComprobarVersionRegistro(conn);
                Console.WriteLine("Version del registro comprobada!");

                Console.Write("\tComprobando valores del registro... ");

                //List<String[ruta, nombreClave]>[Corregidos, NoCorregidos]
                List<String[]>[] fallos = EvaluarContenidoRegistro();

                Console.WriteLine("Valores del registro comprobados!\r\n");

                Console.WriteLine("\t{0} registro erróneos: {1} corregidos - {2} sin corregir\r\n", fallos[0].Count + fallos[1].Count, fallos[0].Count, fallos[1].Count);

                if ((fallos[0].Count > 0) || (fallos[1].Count > 0))
                {
                    Informe(fallos);
                }
                return fallos[0].Count + fallos[1].Count;
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\n\r\nError en la evaluacion completa del registro: \r\n\t{0}", e.ToString());
                return -1;
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

            return new List<String[]>[] { registrosCorregidos, registrosNoCorregidos };
        }

        #endregion EvaluacionCompleta

        #region Informe

        private static void Informe(List<String[]>[] fallosRegistro)
        {
            Console.Write("\tRealizando informe de Registro....");

            String path = "Informes/InformeRegistro-" +
                DateTime.Now.Day + "." +
                DateTime.Now.Month + "." +
                DateTime.Now.Year + " (" +
                DateTime.Now.Hour + "." +
                DateTime.Now.Minute + ").txt";
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
            
            Console.WriteLine("Informe realizado!\r\n");
        }

        #endregion Informe

        #region Utilidad

        private static void ComprobarVersionRegistro(MySqlConnection conn)
        {
            int versionLocal = int.Parse(Util.ReadSetting("VersionRegistro"));

            string query = "SELECT Version, UrlDescarga FROM Registro WHERE Modelo = @modelo";
            MySqlCommand cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@modelo", Util.ReadSetting("Modelo"));

            MySqlDataReader rdr = cmd.ExecuteReader();

            rdr.Read();

            int versionBD = rdr.GetInt32("Version");
            string url = "Registro/" + rdr.GetString("UrlDescarga");

            rdr.Close();

            if (versionLocal != versionBD)
            {
                Util.SFTPDownload(url, "Registro.xml");

                Util.AddUpdateAppSettings("VersionRegistro", versionBD.ToString());

                query = "UPDATE Estacion SET VersionRegistro = @version WHERE ID = @id";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@version", versionBD.ToString());
                cmd.Parameters.AddWithValue("@id", Util.ReadSetting("IdEstacion"));
                cmd.ExecuteNonQuery();
            }
        }

        private static bool FalloRegistro(String path, String nombre, String valor, String tipo)
        {
            Object registro = Registry.GetValue(path, nombre, null);
            switch (tipo)
            {
                case "String":
                    if (registro != null && valor.CompareTo(Util.SanitizeXmlString((String)registro)) != 0)
                    {
                        Registry.SetValue(path, nombre, valor, RegistryValueKind.String);
                        return true;
                    }
                    break;
                case "ExpandString":
                    if (registro != null && Environment.ExpandEnvironmentVariables(valor).CompareTo(Util.SanitizeXmlString((String)registro)) != 0)
                    {
                        Registry.SetValue(path, nombre, valor, RegistryValueKind.ExpandString);
                        return true;
                    }
                    break;
                case "MultiString":
                    if (registro != null && valor.CompareTo(String.Join(" ", (String[])registro)) != 0)
                    {
                        Registry.SetValue(path, nombre, valor.Split(' '), RegistryValueKind.MultiString);
                        return true;
                    }
                    break;
                case "Binary":
                    if (registro != null && valor.CompareTo(Convert.ToBase64String((byte[])registro)) != 0)
                    {
                        Registry.SetValue(path, nombre, Convert.FromBase64String(valor), RegistryValueKind.Binary);
                        return true;
                    }
                    break;
                case "None":
                    if (registro != null && valor.CompareTo(Convert.ToBase64String((byte[])registro)) != 0)
                    {
                        Registry.SetValue(path, nombre, Convert.FromBase64String(valor), RegistryValueKind.None);
                        return true;
                    }
                    break;
                case "DWord":
                    if (registro != null && valor.CompareTo(((Int32)registro).ToString()) != 0)
                    {
                        Registry.SetValue(path, nombre, Int32.Parse(valor), RegistryValueKind.DWord);
                        return true;
                    }
                    break;
                case "QWord":
                    if (registro != null && valor.CompareTo(((Int64)registro).ToString()) != 0)
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
