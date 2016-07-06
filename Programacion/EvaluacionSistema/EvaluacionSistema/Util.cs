using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using Microsoft.Win32;
using System.IO;
using System.Configuration;
using Renci.SshNet;
using System.Diagnostics;

namespace EvaluacionSistema
{
    class Util
    {
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
                    Console.WriteLine("Uploading {0} ({1:N0} bytes)",
                                        localFilename, fileStream.Length);
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
                if (ConfigurationManager.AppSettings["AlmacenarInformes"].CompareTo("No") == 0)
                    File.Delete(fileName);
            }
        }

        #endregion Informes

        #region Comandos

        public static void ProgramarReejecucion()
        {

            //Usar Process.Start("schtasks.exe", "argumentos");
            //Ejemplo:
            Process process = new Process();
            process.StartInfo.FileName = "schtasks.exe.exe";
            process.StartInfo.Arguments = "/create /sc once";//este comando esta mal, hay que revisarlo
            process.StartInfo.CreateNoWindow = true;
            process.Start();


            //usar comando SCHTASK con alguna de las cosas de abajo para programar la reejecucion de este programa

            //Process.Start("nombre programa o fichero con su ruta","argumentos que se pasarian por linea de comandos");
            //ProcessStartInfo psi = new ProcessStartInfo(); Process.Start(psi);

            //Process process = new Process();
            //ProcessStartInfo startInfo = new ProcessStartInfo();
            //startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            //startInfo.FileName = "cmd.exe";
            //startInfo.Arguments = "/C copy /b Image1.jpg + Archive.rar Image2.jpg";
            //process.StartInfo = startInfo;
            //process.Start();
        }

        public static void ProgramarScripts()
        {

        }
        #endregion

    }
}
