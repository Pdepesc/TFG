using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;
using MySql.Data.MySqlClient;
using System.Threading;
using System.IO;

namespace EvaluacionSistema
{
    class EvaluacionHardware
    {
        private static Computer miPc = new Computer() { CPUEnabled = true, FanControllerEnabled = true, GPUEnabled = true, HDDEnabled = true, MainboardEnabled = true, RAMEnabled = true };
        
        #region EvaluacionInicial

        public static bool EvaluacionInicial(MySqlConnection conn)
        {
            try
            {
                Console.WriteLine("---Hardware---\r\n\r\n");

                //Actualizar componentes hardware 100 veces
                Console.Write("\tActualizando componentes hardware... ");
                ActualizarHardware(101);
                Console.WriteLine("Componentes actualizados!");

                //Leer componenetes Hardware y guardarlos en la BBDD
                Console.Write("\tAñadiendo componentes hardware a la BBDD... ");
                string sql = LeerCompoenentes(miPc.Hardware);
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                
                Console.WriteLine("Componentes añadidos!\r\n");

                Console.WriteLine("---Hardware/FIN---\r\n\r\n");

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\n\r\nError en la evaluacion inicial del hardware: \r\n\t{0}", e.ToString());
                return false;
            }
        }
        
        private static string LeerCompoenentes(IHardware[] hardwareCollection)
        {
            string id = Util.ReadSetting("IdEstacion");
            string sql = "INSERT INTO Hardware(ID_Estacion, Identificador, Componente, Sensor, Minimo, Maximo, Media) VALUES ";
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
                        + "), ";
                }
                foreach (IHardware subhardware in hardware.SubHardware)
                {
                    foreach (ISensor sensor in subhardware.Sensors)
                    {
                        sql += "(" + id
                        + ", '" + sensor.Identifier + "'"
                        + ", '" + hardware.Name + "'"
                        + ", '" + sensor.Name + "'"
                        + ", '" + (float)(Math.Truncate((Convert.ToDouble(sensor.Min)) * 100.0) / 100.0) + "'"
                        + ", '" + (float)(Math.Truncate((Convert.ToDouble(sensor.Max)) * 100.0) / 100.0) + "'"
                        + ", '" + (float)(Math.Truncate((Convert.ToDouble(Media(sensor.Values))) * 100.0) / 100.0) + "'"
                        + "), ";
                    }
                }
            }
            return sql.Remove(sql.LastIndexOf(","), 2);
        }
        
        #endregion EvaluacionInicial

        #region EvaluacionCompleta

        public static int EvaluacionCompleta(MySqlConnection conn)
        {
            try
            {
                Console.WriteLine("---Hardware---\r\n");

                //Actualizar componentes hardware 100 veces
                Console.Write("\tActualizando componentes hardware...");
                ActualizarHardware(101);
                Console.WriteLine("Componentes actualizados!");

                //Obtener los componentes a comparar en busca de fallo
                Dictionary<String, ISensor> componentes = ObtenerComponentes(miPc.Hardware);

                //List<String[Identificador, componente, sensor]>
                List<String[]> fallos = CompararComponentes(componentes, conn);

                if (fallos.Count > 0)
                {
                    Informe(fallos);
                }
                return fallos.Count;
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\n\r\nError en la evaluacion completa del hardware: \r\n\t{0}", e.ToString());
                return -1;
            }
        }

        private static List<String[]> CompararComponentes(Dictionary<String, ISensor> componentes, MySqlConnection conn)
        {
            Console.Write("\tObteniendo datos de la BBDD... ");

            //Obtener datos de Hardware
            string sql = "select H.Identificador, M.Minimo, M.Maximo, H.UmbralFallo " +
                "from (select * from Hardware where ID_Estacion = @idEstacion) as H " +
                "left join Medias as M on H.Identificador = M.Identificador";

            MySqlCommand cmd = new MySqlCommand(sql, conn);
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@modeloEstacion", Util.ReadSetting("Modelo"));

            MySqlDataReader rdr = cmd.ExecuteReader();

            Console.WriteLine("Datos obtenidos!");

            Console.Write("\tRealizando diagnostico... ");
            List<String[]> fallos = new List<String[]>();
            while (rdr.Read())
            {
                string identificador = rdr.GetString(0);
                ISensor sensor;
                componentes.TryGetValue(identificador, out sensor);
                if(Fallo(sensor, rdr))
                    fallos.Add(new String[] { identificador, sensor.Name });
            }
            
            rdr.Close();
            Console.WriteLine("Diagnostico realizado!\r\n");
            return fallos;
        }
        
        private static bool Fallo(ISensor sensor, MySqlDataReader rdr)
        {
            float minimo_local = (float)(Math.Truncate((Convert.ToDouble(sensor.Min)) * 100.0) / 100.0);
            float maximo_local = (float)(Math.Truncate((Convert.ToDouble(sensor.Max)) * 100.0) / 100.0);
            float media_local = (float)(Math.Truncate((Convert.ToDouble(Media(sensor.Values))) * 100.0) / 100.0);
            float ultimo_local = (float)(Math.Truncate((Convert.ToDouble(sensor.Values.ElementAt<SensorValue>(sensor.Values.Count() - 1).Value)) * 100.0) / 100.0);
            float minimo_bd = rdr.GetFloat(1);
            float maximo_bd = rdr.GetFloat(2);
            float umbralFallo = rdr.GetFloat(3);

            minimo_bd = minimo_bd * (1 - umbralFallo);
            maximo_bd = maximo_bd * (1 + umbralFallo);
            
            //Metrica para determinar si falla o no algun componente Hardware
            if (minimo_local < minimo_bd || maximo_local > maximo_bd || media_local < minimo_bd || media_local > maximo_bd)
                return true;
            else
                return false;
        }

        #endregion EvaluacionCompleta

        #region Informe

        private static void Informe(List<String[]> fallosHardware)
        {
            Console.Write("\tRealizando informe de Hardware....");

            String path = "Informes/InformeHardware-" +
                DateTime.Now.Day + "." +
                DateTime.Now.Month + "." +
                DateTime.Now.Year + " (" +
                DateTime.Now.Hour + "." +
                DateTime.Now.Minute + ").txt";
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine("Componentes Hardware que fallan");

                foreach(String[] fallo in fallosHardware)
                {
                    sw.WriteLine(fallo[1] + " (" + fallo[0] + ")"); sw.WriteLine();
                }
                GetReport(sw);
            }

            Console.WriteLine("Informe realizado!");
        }
        
        private static void GetReport(StreamWriter sw)
        {
            sw.WriteLine("--------------------------------------------------------------------------------"); sw.WriteLine();
            sw.WriteLine("Sensors"); sw.WriteLine();
            GetSensorsReport(sw, miPc.Hardware, ""); sw.WriteLine();
            sw.WriteLine("--------------------------------------------------------------------------------"); sw.WriteLine();
            sw.WriteLine("Parameters"); sw.WriteLine();
            GetParametersReport(sw, miPc.Hardware, ""); sw.WriteLine();
            sw.WriteLine("--------------------------------------------------------------------------------"); sw.WriteLine();
            sw.WriteLine("Hardware"); sw.WriteLine();
            GetHardwareReport(sw, miPc.Hardware);
            miPc.Close();
        }
        
        private static void GetSensorsReport(StreamWriter sw, IHardware[] hardwareCollection, String prefijo)
        {
            foreach (IHardware hardware in hardwareCollection)
            {
                hardware.Update();
                sw.WriteLine(prefijo + "|");
                sw.WriteLine(prefijo + "+- " + hardware.Name + " (" + hardware.Identifier + ")");
                if (hardware.SubHardware.Length > 0) GetSensorsReport(sw, hardware.SubHardware, "|  " + prefijo);
                foreach (ISensor sensor in hardware.Sensors)
                {
                    String tab = "";
                    int contador = ((26 - (sensor.Name.Length + prefijo.Length)) / 8) + (((prefijo.Length + 6 + sensor.Name.Length) % 8) != 0 ? 1 : 0);
                    for (int i = 0; i < contador; i++) tab += "\t";
                    float value = (float)(Math.Truncate((Convert.ToDouble(sensor.Value)) * 100.0) / 100.0);
                    float min = (float)(Math.Truncate((Convert.ToDouble(sensor.Min)) * 100.0) / 100.0);
                    float max = (float)(Math.Truncate((Convert.ToDouble(sensor.Max)) * 100.0) / 100.0);
                    sw.WriteLine(prefijo + "|  +- " + sensor.Name + tab + ":\t" +
                        value.ToString().PadLeft(7) + "\t" +
                        min.ToString().PadLeft(7) + "\t" +
                        max.ToString().PadLeft(7) + "\t(" +
                        sensor.Identifier + ")");
                }
            }
        }

        private static void GetParametersReport(StreamWriter sw, IHardware[] hardwareCollection, String prefijo)
        {
            foreach (IHardware hardware in hardwareCollection)
            {
                hardware.Update();
                sw.WriteLine(prefijo + "|");
                sw.WriteLine(prefijo + "+- " + hardware.Name + " (" + hardware.Identifier + ")");
                if (hardware.SubHardware.Length > 0) GetParametersReport(sw, hardware.SubHardware, "|  " + prefijo);
                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (sensor.Parameters.Length > 0)
                    {
                        sw.WriteLine(prefijo + "|  +- " + sensor.Name + " (" + sensor.Identifier + ")");
                        foreach (IParameter parametro in sensor.Parameters)
                        {
                            sw.WriteLine(prefijo + "|  |  +- " + parametro.Name + " : " + parametro.DefaultValue + " : " + parametro.Value);
                        }
                    }
                }
            }
        }

        private static void GetHardwareReport(StreamWriter sw, IHardware[] hardwareCollection)
        {
            foreach (IHardware hardware in hardwareCollection)
            {
                sw.WriteLine("--------------------------------------------------------------------------------"); sw.WriteLine();
                sw.WriteLine(hardware.GetReport());
            }
        }

        #endregion Reporte/Informe

        #region MetodosAcceso

        //miPc.Hardware[]
        public static void GetHardware()
        {
            miPc.Open();
            Console.WriteLine("--------------------------------------------------------------------------------");
            foreach (IHardware hardware in miPc.Hardware)
            {
                hardware.Update();
                Console.WriteLine("Name: " + hardware.Name);
                Console.WriteLine("Type: " + hardware.HardwareType);
                Console.WriteLine("Identifier: " + hardware.Identifier);
                Console.WriteLine("Report: \r\n" + hardware.GetReport() + "\r\nEnd Report");
                Console.WriteLine("Nº Sensors: " + hardware.Sensors.Length);
                if (hardware.Sensors.Length > 0) GetSensors(hardware, "");
                Console.WriteLine("Nº SubHardware: " + hardware.SubHardware.Length);
                if (hardware.SubHardware.Length > 0) GetSubHardware(hardware, "");
                Console.WriteLine();
                Console.WriteLine();
            }
            miPc.Close();
            Console.Read();
        }

        //Hardware.SubHardware[]
        private static void GetSubHardware(IHardware hardware, String tab)
        {
            tab = tab + "\t";
            foreach (IHardware subhardware in hardware.SubHardware)
            {
                subhardware.Update();
                Console.WriteLine(tab + "Name: " + subhardware.Name);
                Console.WriteLine(tab + "Type: " + subhardware.HardwareType);
                Console.WriteLine(tab + "Identifier: " + subhardware.Identifier);
                Console.WriteLine(tab + "Report: \r\n" + subhardware.GetReport() + "\r\nEnd Report");
                Console.WriteLine(tab + "Nº Sensors: " + subhardware.Sensors.Length);
                if (subhardware.Sensors.Length > 0) GetSensors(subhardware, tab);
                Console.WriteLine(tab + "Nº SubHardware: " + subhardware.SubHardware.Length);
                if (subhardware.SubHardware.Length > 0) GetSubHardware(subhardware, tab);
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        //Hardware.Sensors[] / SubHardware.Sensors[]
        private static void GetSensors(IHardware hardware, String tab)
        {
            tab = tab + "\t";
            foreach (ISensor sensor in hardware.Sensors)
            {
                Console.WriteLine(tab + "Name: " + sensor.Name);
                Console.WriteLine(tab + "Type: " + sensor.SensorType);
                Console.WriteLine(tab + "Identifier: " + sensor.Identifier);
                Console.WriteLine(tab + "Hardware: " + sensor.Hardware);
                Console.WriteLine(tab + "Index: " + sensor.Index);
                Console.WriteLine(tab + "IsDefaultHidden: " + sensor.IsDefaultHidden);
                Console.WriteLine(tab + "Max: " + sensor.Max + " - Min: " + sensor.Min + " - Value: " + sensor.Value);
                Console.WriteLine(tab + "Nº Parameters: " + sensor.Parameters.Length);
                if (sensor.Parameters.Length > 0) GetParameters(sensor, tab);
                Console.WriteLine(tab + "Nº Values: " + sensor.Values.Count());
                if (sensor.Values.Count() > 0) GetValues(sensor, tab);
                Console.WriteLine(tab + "Control: " + sensor.Control);
                if (sensor.Control != null) GetControl(sensor.Control, tab);
            }
        }

        //Sensor.Parameters[]
        private static void GetParameters(ISensor sensor, String tab)
        {
            tab = tab + "\t";
            foreach (IParameter parametro in sensor.Parameters)
            {
                Console.WriteLine(tab + "Name: " + parametro.Name);
                Console.WriteLine(tab + "DefaultValue: " + parametro.DefaultValue);
                Console.WriteLine(tab + "Description: " + parametro.Description);
                Console.WriteLine(tab + "Identifier: " + parametro.Identifier);
                Console.WriteLine(tab + "Sensor: " + parametro.Sensor);
                Console.WriteLine(tab + "Value: " + parametro.Value);
            }
        }

        //Sensor.Values[]
        private static void GetValues(ISensor sensor, String tab)
        {
            tab = tab + "\t";
            foreach (SensorValue valor in sensor.Values)
            {
                Console.WriteLine(tab + "- " + valor.Time + ": " + valor.Value);
            }
        }

        //Sensor.Control
        private static void GetControl(IControl control, String tab)
        {
            tab = tab + "\t";
            Console.WriteLine(tab + "ControlMode: " + control.ControlMode);
            Console.WriteLine(tab + "Identifier: " + control.Identifier);
            Console.WriteLine(tab + "MaxSoftwareValue: " + control.MaxSoftwareValue);
            Console.WriteLine(tab + "MinSoftwareValue: " + control.MinSoftwareValue);
            Console.WriteLine(tab + "SoftwareValue: " + control.SoftwareValue);
        }

        #endregion MetodosAcceso

        #region Utilidad

        private static void ActualizarHardware(int contador)
        {
            miPc.Open();
            while (contador-- != 0)
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

        private static Dictionary<String, ISensor> ObtenerComponentes(IHardware[] hardwareCollection)
        {
            Dictionary<String, ISensor> componentes = new Dictionary<string, ISensor>();
            foreach (IHardware hardware in hardwareCollection)
            {
                foreach (ISensor sensor in hardware.Sensors)
                {
                    componentes.Add(sensor.Identifier.ToString(), sensor);
                }
                if (hardware.SubHardware.Length > 0)
                {
                    foreach (IHardware subhardware in hardware.SubHardware)
                    {
                        foreach (ISensor sensor in subhardware.Sensors)
                        {
                            componentes.Add(sensor.Identifier.ToString(), sensor);
                        }
                    }
                }
            }
            return componentes;
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

        #endregion Utilidad
    }
}
