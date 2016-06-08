using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using Microsoft.Win32;
using System.IO;

namespace EvaluacionSistema
{
    class Util
    {

        public static void GetRegistro()
        {
            XElement registro = new XElement("registro");

            Console.WriteLine("Iniciando reporte del registro...");
            
            PrintKeys(Registry.ClassesRoot, registro);
            Console.WriteLine("ClassesRoot done");
            //Console.Read();
            PrintKeys(Registry.CurrentConfig, registro);
            Console.WriteLine("CurrentConfig done");
            //Console.Read();
            PrintKeys(Registry.CurrentUser, registro);
            Console.WriteLine("CurrentUser done");
            //Console.Read();
            PrintKeys(Registry.LocalMachine, registro);
            Console.WriteLine("LocalMachine done");
            //Console.Read();
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
                value.SetAttributeValue("name", (s.CompareTo("") == 0)? "(Predeterminado)" : SanitizeXmlString(s));
                
                RegistryValueKind rvk = rkey.GetValueKind(s);
                switch (rvk)
                {
                    //FUNCIONA
                    case RegistryValueKind.DWord:
                    case RegistryValueKind.QWord:
                        value.SetAttributeValue("value", rkey.GetValue(s));
                        break;
                    case RegistryValueKind.MultiString:
                        String[] multistringValue = (String[])rkey.GetValue(s);
                        String valores = "";
                        for (int i = 0; i < multistringValue.Length; i++)
                        {
                            valores += multistringValue[i] + "\\0";
                        }
                        value.SetAttributeValue("value", valores);
                        break;
                    case RegistryValueKind.Binary:
                    case RegistryValueKind.None:
                        byte[] bytesValue = (byte[])rkey.GetValue(s);
                        String bytes = "";
                        for (int i = 0; i < bytesValue.Length; i++)
                        {
                            //registro.Write("{0} ", bytesValue[i]);    //Decimal
                            bytes += bytesValue[i] + " ";    //Hexadecimal
                        }
                        value.SetAttributeValue("value", bytes);
                        break;
                    default:
                        value.SetAttributeValue("value", rkey.GetValue(s));
                        break;
                }

                value.SetAttributeValue("type", rkey.GetValueKind(s));
                
                key.Add(value);
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



        #region Crear XML 
        public static void crear_XML()
        {
            XDocument miXML = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Empleados",

                new XElement("Empleado",
                new XAttribute("Id_Empleado", "321654"),
                new XElement("Nombre", "Miguel Suarez"),
                new XElement("Edad", "30")),

                new XElement("Empleado",
                new XAttribute("Id_Empleado", "123456"),
                new XElement("Nombre", "Maria Martinez"),
                new XElement("Edad", "27")),

                new XElement("Empleado",
                new XAttribute("Id_Empleado", "987654"),
                new XElement("Nombre", "Juan Gonzales"),
                new XElement("Edad", "25"))
                ));
            miXML.Save("MiDoc.xml");
        }
        #endregion

        #region Buscar en XML 

        private void buscarEnXML(string idempleado)
        {
            XDocument miXML = XDocument.Load(@"C:\Prueba\MiDoc.xml");

            var nombreusu = from nombre in miXML.Elements("Empleados").Elements("Empleado")
                            where nombre.Attribute("Id_Empleado").Value == idempleado
                            select nombre.Element("Nombre").Value;
        }
        #endregion

        #region Agregar Nodo
        private void addNode(string idEmpleado, string nombre, string edad)
        {
            XDocument miXML = XDocument.Load(@"C:\Prueba\MiDoc.xml");
            miXML.Root.Add(
                new XElement("Empleado",
                    new XAttribute("Id_Empleado", idEmpleado),
                    new XElement("Nombre", nombre),
                    new XElement("Edad", edad))
                    );
            miXML.Save(@"C:\Prueba\MiDoc.xml");
        }
        #endregion

        #region Eliminar Nodo
        private void dropNode(string idEmpleado)
        {
            XDocument miXML = XDocument.Load(@"C:\Prueba\MiDoc.xml");

            var consul = from persona in miXML.Elements("Empleados").Elements("Empleado")
                         where persona.Attribute("Id_Empleado").Value == idEmpleado
                         select persona;
            consul.ToList().ForEach(x => x.Remove());
            miXML.Save("MiDoc.xml");
        }
        #endregion

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
    }
}
