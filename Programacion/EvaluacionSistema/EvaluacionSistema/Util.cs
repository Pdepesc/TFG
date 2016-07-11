using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using Microsoft.Win32;
using System.IO;
using System.Configuration;
using Renci.SshNet;
using System.Diagnostics;
using Microsoft.Win32.TaskScheduler;
using System.Diagnostics.Eventing.Reader;
using MySql.Data.MySqlClient;

namespace EvaluacionSistema
{
    class Util
    {
        #region RegistrarEstacion

        public static void RegistrarEstacion(MySqlConnection conn)
        {
            Console.Write("\r\nAñadiendo estacion a la BBDD... ");

            //Añadir esta estación a la BBDD y obtener su ID
            string sql = "INSERT INTO Estacion(Empresa, Modelo, VersionRegistro) VALUES (@empresa, @modelo, @version)";

            MySqlCommand cmd = new MySqlCommand(sql, conn);
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@empresa", Util.ReadSetting("Empresa"));
            cmd.Parameters.AddWithValue("@modelo", Util.ReadSetting("Modelo"));
            cmd.Parameters.AddWithValue("@version", Util.ReadSetting("VersionRegistro"));
            cmd.ExecuteNonQuery();
            
            Util.AddUpdateAppSettings("IdEstacion", cmd.LastInsertedId.ToString());

            Console.WriteLine("Estacion añadida!\r\n");
        }

        #endregion RegistrarEstacion

        #region SFTPManager

        public static void SFTPDownload(String remoteUrl, String localDestinationFilename)
        {
            String host = ConfigurationManager.ConnectionStrings["SftpHost"].ConnectionString;
            String port = ConfigurationManager.ConnectionStrings["SftpPort"].ConnectionString;
            String user = ConfigurationManager.ConnectionStrings["SftpUser"].ConnectionString;
            String pass = ConfigurationManager.ConnectionStrings["SftpPassword"].ConnectionString;
            String workDir = ConfigurationManager.ConnectionStrings["SftpWorkingDirectory"].ConnectionString;

            using (var sftp = new SftpClient(host, int.Parse(port), user, pass))
            {
                sftp.Connect();

                using (var file = File.OpenWrite(localDestinationFilename))
                {
                    sftp.DownloadFile(workDir + remoteUrl, file);
                }

                sftp.Disconnect();
            }
        }

        public static void SFTPUpload(String remoteDirectory, String localFilename)
        {
            String host = ConfigurationManager.ConnectionStrings["SftpHost"].ConnectionString;
            String port = ConfigurationManager.ConnectionStrings["SftpPort"].ConnectionString;
            String user = ConfigurationManager.ConnectionStrings["SftpUser"].ConnectionString;
            String pass = ConfigurationManager.ConnectionStrings["SftpPassword"].ConnectionString;
            String workDir = ConfigurationManager.ConnectionStrings["SftpWorkingDirectory"].ConnectionString;

            using (var client = new SftpClient(host, int.Parse(port), user, pass))
            {
                client.Connect();
                //Console.WriteLine("Connected to {0}", host);

                client.ChangeDirectory(workDir + remoteDirectory);
                //Console.WriteLine("Changed directory to {0}", remoteDirectory);

                /*
                var listDirectory = client.ListDirectory(SFTPManager.work_dir + remoteDirectory);
                Console.WriteLine("Listing directory:");
                foreach (var fi in listDirectory)
                {
                    Console.WriteLine(" - " + fi.Name);
                }*/

                using (var fileStream = new FileStream(localFilename, FileMode.Open))
                {
                    //Console.WriteLine("Uploading {0} ({1:N0} bytes)",
                    //                    localFilename, fileStream.Length);
                    client.BufferSize = 4 * 1024; // bypass Payload error large files
                    client.UploadFile(fileStream, Path.GetFileName(localFilename));
                }

                client.Disconnect();
            }
        }

        #endregion SFTPManager

        #region ConfigurationSettings

        public static String ReadSetting(String key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                return appSettings[key] ?? null;
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
                return null;
            }
        }

        public static void AddUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                    settings.Add(key, value);
                else
                    settings[key].Value = value;
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }

        #endregion ConfigurationSettings

        #region GetRegistryKeys

        public static void GetRegistro()
        {
            XElement registro = new XElement("registro");

            Console.WriteLine("Iniciando reporte del registro...");
            
            PrintKeys(Registry.ClassesRoot, registro);
            Console.WriteLine("ClassesRoot done");
            PrintKeys(Registry.CurrentConfig, registro);
            Console.WriteLine("CurrentConfig done");
            PrintKeys(Registry.CurrentUser, registro);
            Console.WriteLine("CurrentUser done");
            PrintKeys(Registry.LocalMachine, registro);
            Console.WriteLine("LocalMachine done");
            PrintKeys(Registry.Users, registro);
            Console.WriteLine("Users done");
            
            XDocument miXML = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                registro);
            miXML.Save("Registro.xml");

            Console.WriteLine("Reporte finalizado!");
            Console.Read();
        }
        
        private static void PrintKeys(RegistryKey rkey, XElement parent)
        {
            int index = rkey.Name.LastIndexOf("\\");
            XElement key = new XElement("k",
                new XAttribute("name", (index < 0)? rkey.Name : rkey.Name.Substring(index + 1)));

            String[] valueNames = rkey.GetValueNames();

            foreach (String s in valueNames)
            {
                
                XElement value = new XElement("v");
                value.SetAttributeValue("name", (s.CompareTo("") == 0)? "" : SanitizeXmlString(s));
                
                RegistryValueKind rvk = rkey.GetValueKind(s);
                switch (rvk)
                {
                    case RegistryValueKind.String:
                        String valor1 = (String)rkey.GetValue(s);
                        value.SetAttributeValue("value", (valor1.CompareTo("") == 0) ? "" : SanitizeXmlString(valor1));
                        break;
                    case RegistryValueKind.ExpandString:
                        String valor2 = (String)rkey.GetValue(s, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                        value.SetAttributeValue("value", (valor2.CompareTo("") == 0) ? "" : SanitizeXmlString(valor2));
                        break;
                    case RegistryValueKind.MultiString:
                        value.SetAttributeValue("value", String.Join(" ", (String[])rkey.GetValue(s)));
                        break;
                    case RegistryValueKind.Binary:
                    case RegistryValueKind.None:
                        //value.SetAttributeValue("value", BitConverter.ToString((byte[])rkey.GetValue(s)));
                        value.SetAttributeValue("value", Convert.ToBase64String((byte[])rkey.GetValue(s)));
                        break;
                    case RegistryValueKind.DWord:
                    case RegistryValueKind.QWord:
                    default:
                        value.SetAttributeValue("value", rkey.GetValue(s));
                        break;
                }

                value.SetAttributeValue("type", rkey.GetValueKind(s));
                
                if(rvk != RegistryValueKind.Unknown) key.Add(value);
            }

            try
            {
                String[] subkeys = rkey.GetSubKeyNames();

                foreach (String s in subkeys)
                {
                    try
                    {
                        RegistryKey subrkey = rkey.OpenSubKey(s);
                        PrintKeys(subrkey, key);
                    }
                    catch (System.Security.SecurityException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine("Fallo al leer las subclaves de " + rkey.Name);
            }
            
            parent.Add(key);
        }
        
        /// <summary>
        /// Remove illegal XML characters from a string.
        /// </summary>
        public static string SanitizeXmlString(string xml)
        {
            if (xml == null)
            {
                throw new ArgumentNullException("xml");
            }

            StringBuilder buffer = new StringBuilder(xml.Length);

            foreach (char c in xml)
            {
                if (IsLegalXmlChar(c))
                {
                    buffer.Append(c);
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Whether a given character is allowed by XML 1.0.
        /// </summary>
        public static bool IsLegalXmlChar(int character)
        {
            return
            (
                 character == 0x9 /* == '\t' == 9   */          ||
                 character == 0xA /* == '\n' == 10  */          ||
                 character == 0xD /* == '\r' == 13  */          ||
                (character >= 0x20 && character <= 0xD7FF) ||
                (character >= 0xE000 && character <= 0xFFFD) ||
                (character >= 0x10000 && character <= 0x10FFFF)
            );
        }

        #endregion GetRegistryKeys

        #region Informes

        public static void EnviarInformes()
        {
            string[] fileEntries = Directory.GetFiles(Directory.GetCurrentDirectory() + "/Informes");
            foreach (string fileName in fileEntries)
            {
                Util.SFTPUpload("Informes/", fileName);
                if (Util.ReadSetting("AlmacenarInformes").CompareTo("No") == 0)
                    File.Delete(fileName);
            }
        }

        #endregion Informes

        #region TaskScheduler

        //Programa la ejecucion de este programa cada vez que se inicia el sistema
        public static void InicializarTareaProgramada()
        {
            Task t = TaskService.Instance
                .Execute(Directory.GetCurrentDirectory() + "\\EvaluacionSistema.exe")
                .AtLogon()
                .AsTask("EvaluacionSistema");
            t.Definition.Principal.RunLevel = TaskRunLevel.Highest;
            t.RegisterChanges();
            Console.WriteLine("\r\nSe ha programado la ejecucion de este programa tras el proximo inicio de sesion");
        }

        //Programa la proxima ejecucion de este programa en funcion del intervalo establecido en las propiedades
        public static void ProgramarReejecucion()
        {
            Task t = TaskService.Instance
                .Execute(Directory.GetCurrentDirectory() + "\\EvaluacionSistema.exe")
                .Once()
                .Starting(DateTime.Now.AddHours(Double.Parse(Util.ReadSetting("IntervaloEjecucion"))))
                .AsTask("ReEvaluacionSistema");
            t.Definition.Principal.RunLevel = TaskRunLevel.Highest;
            t.RegisterChanges();
            Console.WriteLine("\r\nSe ha programado la ejecucion de este programa tras {0} horas", Util.ReadSetting("IntervaloEjecucion"));
        }

        public static bool ExisteScriptParaEvento(string evento)
        {
            return TaskService.Instance.FindTask(evento) != null;
        }

        public static void ProgramarScript(string script, string nombre)
        {
            DateTime ejecucion = new DateTime(DateTime.Now.Year,
                        DateTime.Now.Month,
                        DateTime.Now.Day,
                        int.Parse(Util.ReadSetting("EjecucionScriptsHoras")),
                        int.Parse(Util.ReadSetting("EjecucionScriptsMinutos")),
                        0);

            Task t = TaskService.Instance
                .Execute(Directory.GetCurrentDirectory() + "\\" + script)
                .Once()
                .Starting(ejecucion)
                .Ending(ejecucion.AddSeconds(30))
                .AsTask(nombre);
            t.Definition.Principal.RunLevel = TaskRunLevel.Highest;
            t.Definition.Settings.DeleteExpiredTaskAfter = TimeSpan.FromSeconds(30);

            t.RegisterChanges();
        }

        #endregion TaskScheduler
    }
}
