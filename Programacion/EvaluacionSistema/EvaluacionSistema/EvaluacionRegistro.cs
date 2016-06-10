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
        public static bool EvaluacionInicial(MySqlConnection conn, Properties properties)
        {
            try {
                Console.WriteLine("Iniciando evaluacion inicial del Registro...");

                Console.WriteLine("Comprobando version del registro...");

                ComprobarVersionRegistro(conn, properties);

                Console.WriteLine("Comprobando valores del registro...");

                int fallos = ComprobarRegistro();
                if (fallos > 0)
                    Console.WriteLine("Se han corergido " + fallos + " registros erroneos!");

                Console.WriteLine("Evaluacion inicial del Registro finalizada!");
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

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
                Console.WriteLine("Actualizando el fichero del registro...");
                
                SFTPManager.Download(url, "Registro.xml");
                
                properties.set("VersionRegistro", versionBD.ToString());
                
                query = "UPDATE Estacion SET VersionRegistro = @version WHERE ID = @id";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@version", versionBD.ToString());
                cmd.Parameters.AddWithValue("@id", properties.get("IdEstacion"));
                cmd.ExecuteNonQuery();
            }
        }
        
        private static int ComprobarRegistro()
        {
            List<String[]> values = ParseXML();
            String path, nombre, valor, tipo;
            int fallos = 0;
            
            foreach(String[] value in values){
                path = value[0];
                nombre = value[1];
                valor = value[2];
                tipo = value[3];
                Object registro = Registry.GetValue(path, nombre, null);
                switch (tipo)
                {
                    case "String":
                        if (valor.CompareTo(Util.SanitizeXmlString((String)registro)) != 0)
                            Registry.SetValue(path, nombre, valor, RegistryValueKind.String); fallos++;
                        break;
                    case "ExpandString":
                        if (valor.CompareTo(Util.SanitizeXmlString((String)registro)) != 0)
                            Registry.SetValue(path, nombre, valor.Split(' '), RegistryValueKind.ExpandString); fallos++;
                        break;
                    case "MultiString":
                        if (valor.CompareTo(String.Join(" ", (String[])registro)) != 0)
                            Registry.SetValue(path, nombre, valor.Split(' '), RegistryValueKind.MultiString); fallos++;
                        break;
                    case "Binary":
                        if (valor.CompareTo(Convert.ToBase64String((byte[])registro)) != 0)
                            Registry.SetValue(path, nombre, Convert.FromBase64String(valor), RegistryValueKind.Binary); fallos++;
                        break;
                    case "None":
                        if (valor.CompareTo(Convert.ToBase64String((byte[])registro)) != 0)
                            Registry.SetValue(path, nombre, Convert.FromBase64String(valor), RegistryValueKind.None); fallos++;
                        break;
                    case "DWord":
                        if (valor.CompareTo(((Int32)registro).ToString()) != 0)
                            Registry.SetValue(path, nombre, Int32.Parse(valor), RegistryValueKind.DWord); fallos++;
                        break;
                    case "QWord":
                        if (valor.CompareTo(((Int64)registro).ToString()) != 0)
                            Registry.SetValue(path, nombre, Int64.Parse(valor), RegistryValueKind.QWord); fallos++;
                        break;
                }
            }
            
            return fallos;
        }

        private static List<String[]> ParseXML()
        {
            XmlDocument xml = new XmlDocument();
            xml.Load("");
            XmlNodeList value_nodes = xml.GetElementsByTagName("v");
            List<String[]> values = new List<string[]>();

            String path, nombre, valor, tipo;

            for (int i = 0; i < value_nodes.Count; i++)
            {
                path = GetPath(value_nodes.Item(i));
                nombre = value_nodes.Item(i).Attributes.Item(0).Value;
                valor = value_nodes.Item(i).Attributes.Item(1).Value;
                tipo = value_nodes.Item(i).Attributes.Item(2).Value;
                values.Add(new String[]{ path, nombre, valor, tipo});
            }

            return values;
        }

        private static String GetPath(XmlNode nodo)
        {
            String path = "";
            while((nodo = nodo.ParentNode).Attributes.Item(0).Value.CompareTo("registro") != 0)
            {
                path = nodo.Attributes.Item(0).Value + "\\" + path;
            }
            return path.Remove(path.LastIndexOf("\\"));
        }
    }
}
