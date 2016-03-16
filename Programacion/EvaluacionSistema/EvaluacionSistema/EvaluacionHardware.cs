using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;

namespace EvaluacionSistema
{
    class EvaluacionHardware
    {
        //Utilizar libreria OpenHardwareMonitor / Otra herramienta del estilo

        private static Computer miPc = new Computer() { CPUEnabled = true, FanControllerEnabled = true, GPUEnabled = true, HDDEnabled = true, MainboardEnabled = true, RAMEnabled = true };

        //Obtener reporte (por ahora por linea de comandos) --> pasarlo a un fichero
        public static void GetReport()
        {
            miPc.Open();
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("Sensors");
            Console.WriteLine();
            GetSensors(miPc.Hardware, "");
            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("Parameters");
            Console.WriteLine();
            GetParameters(miPc.Hardware, "");
            Console.WriteLine();
            GetHardwareReport(miPc.Hardware);
            miPc.Close();
            Console.Read();
        }

        private static void GetSensors(IHardware[] hardwareCollection, String prefijo)
        {
            foreach (IHardware hardware in hardwareCollection)
            {
                hardware.Update();
                Console.WriteLine(prefijo + "|");
                Console.WriteLine(prefijo + "+- " + hardware.Name + " (" + hardware.Identifier + ")");
                if (hardware.SubHardware.Length > 0) GetSensors(hardware.SubHardware, "|  " + prefijo);
                foreach (ISensor sensor in hardware.Sensors)
                {
                    String tab = "";
                    int contador = ((26 - (sensor.Name.Length + prefijo.Length)) / 8) + (((prefijo.Length + 6 + sensor.Name.Length) % 8) != 0 ? 1 : 0);
                    for (int i = 0; i < contador; i++) tab += "\t";
                    Console.WriteLine(prefijo + "|  +- " + sensor.Name + tab + ":\t" +
                        sensor.Value.ToString().PadLeft(7) + "\t" +
                        sensor.Min.ToString().PadLeft(7) + "\t" +
                        sensor.Max.ToString().PadLeft(7) + "\t(" +
                        sensor.Identifier + ")");
                }
            }
        }

        private static void GetParameters(IHardware[] hardwareCollection, String prefijo)
        {
            foreach (IHardware hardware in hardwareCollection)
            {
                hardware.Update();
                Console.WriteLine(prefijo + "|");
                Console.WriteLine(prefijo + "+- " + hardware.Name + " (" + hardware.Identifier + ")");
                if (hardware.SubHardware.Length > 0) GetParameters(hardware.SubHardware, "|  " + prefijo);
                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (sensor.Parameters.Length > 0)
                    {
                        Console.WriteLine(prefijo + "|  +- " + sensor.Name + " (" + sensor.Identifier + ")");
                        foreach (IParameter parametro in sensor.Parameters)
                        {
                            Console.WriteLine(prefijo + "|  |  +- " + parametro.Name + " : " + parametro.DefaultValue + " : " + parametro.Value);
                        }
                    }
                }
            }
        }

        private static void GetHardwareReport(IHardware[] hardwareCollection)
        {
            foreach (IHardware hardware in hardwareCollection)
            {
                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.WriteLine();
                Console.WriteLine(hardware.GetReport());
            }
        }

        //Metodos de "ejemplo" para ver como se obtiene todo    --  Faltan metodos para determinar el correcto funcionamiento del sistema
        public static void GetHardware()
        {
            miPc.Open();
            String tab = "\t";
            foreach (IHardware hardware in miPc.Hardware)
            {
                hardware.Update();
                Console.WriteLine(hardware);
                Console.WriteLine(tab + hardware.HardwareType + " - " + hardware.Name);
                Console.WriteLine(tab + hardware.Identifier);
                Console.WriteLine(tab + "Sensores - " + hardware.Sensors.Length);
                if (hardware.Sensors.Length > 0) GetSensores(hardware, tab);
                Console.WriteLine(tab + "SubHardware - " + hardware.SubHardware.Length);
                if (hardware.SubHardware.Length > 0) GetSubHardware(hardware, tab);
            }
            miPc.Close();
            Console.Read();
        }

        private static void GetSubHardware(IHardware hardware, String tab)
        {
            tab = tab + "\t";
            foreach (IHardware subhardware in hardware.SubHardware)
            {
                subhardware.Update();
                Console.WriteLine(tab + subhardware);
                Console.WriteLine(tab + subhardware.HardwareType + " - " + subhardware.Name);
                Console.WriteLine(tab + subhardware.Identifier);
                Console.WriteLine(tab + "Sensores - " + subhardware.Sensors.Length);
                if (subhardware.Sensors.Length > 0) GetSensores(subhardware, tab);
                Console.WriteLine(tab + "SubHardware - " + subhardware.SubHardware.Length);
                if (subhardware.SubHardware.Length > 0) GetSubHardware(subhardware, tab);
            }
        }

        private static void GetSensores(IHardware hardware, String tab)
        {
            tab = tab + "\t";
            foreach (ISensor sensor in hardware.Sensors)
            {
                Console.WriteLine(tab + sensor.SensorType + " - " + sensor.Name);
                Console.WriteLine(tab + sensor.Identifier);
                Console.WriteLine(tab + sensor.Hardware);
                Console.WriteLine(tab + sensor.Index);
                Console.WriteLine(tab + sensor.IsDefaultHidden);
                Console.WriteLine(tab + "Max: " + sensor.Max + " - Min: " + sensor.Min + " - Valor: " + sensor.Value);
                Console.WriteLine(tab + "Parametros - " + sensor.Parameters.Length);
                if (sensor.Parameters.Length > 0) GetParametrosSensor(sensor, tab);
                Console.WriteLine(tab + "Valores - " + sensor.Values.Count());
                if (sensor.Values.Count() > 0) GetValoresSensor(sensor, tab);
            }
        }

        private static void GetParametrosSensor(ISensor sensor, String tab)
        {
            tab = tab + "\t";
            foreach (IParameter parametro in sensor.Parameters)
            {
                Console.WriteLine(tab + parametro.Name);
                Console.WriteLine(tab + parametro.DefaultValue);
                Console.WriteLine(tab + parametro.Description);
                Console.WriteLine(tab + parametro.Identifier);
                Console.WriteLine(tab + parametro.Sensor + " - " + parametro.Sensor.Name + ": " + parametro.Sensor.Value);
                Console.WriteLine(tab + parametro.Value);
            }
        }

        private static void GetValoresSensor(ISensor sensor, String tab)
        {
            tab = tab + "\t";
            foreach (SensorValue valor in sensor.Values)
            {
                Console.WriteLine(tab + "- " + valor.Time + ": " + valor.Value);
            }
        }
    }
}
