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

        //Obtiene un Reporte con toda la información necesaria (por linea de comandos)
        public static void GetReport()
        {
            miPc.Open();
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("Sensors");
            Console.WriteLine();
            GetSensorsReport(miPc.Hardware, "");
            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("Parameters");
            Console.WriteLine();
            GetParametersReport(miPc.Hardware, "");
            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("Hardware");
            Console.WriteLine();
            GetHardwareReport(miPc.Hardware);
            miPc.Close();
            Console.Read();
        }

        //Si el metodo solo se ejecuta una vez por Sensor, Sensor.Value == Sensor.Min == Sensor.Max
        private static void GetSensorsReport(IHardware[] hardwareCollection, String prefijo)
        {
            foreach (IHardware hardware in hardwareCollection)
            {
                hardware.Update();
                Console.WriteLine(prefijo + "|");
                Console.WriteLine(prefijo + "+- " + hardware.Name + " (" + hardware.Identifier + ")");
                if (hardware.SubHardware.Length > 0) GetSensorsReport(hardware.SubHardware, "|  " + prefijo);
                foreach (ISensor sensor in hardware.Sensors)
                {
                    String tab = "";
                    int contador = ((26 - (sensor.Name.Length + prefijo.Length)) / 8) + (((prefijo.Length + 6 + sensor.Name.Length) % 8) != 0 ? 1 : 0);
                    for (int i = 0; i < contador; i++) tab += "\t";
                    float value = (float)(Math.Truncate((Convert.ToDouble(sensor.Value)) * 100.0) / 100.0);
                    float min = (float)(Math.Truncate((Convert.ToDouble(sensor.Min)) * 100.0) / 100.0);
                    float max = (float)(Math.Truncate((Convert.ToDouble(sensor.Max)) * 100.0) / 100.0);
                    Console.WriteLine(prefijo + "|  +- " + sensor.Name + tab + ":\t" +
                        value.ToString().PadLeft(7) + "\t" +
                        min.ToString().PadLeft(7) + "\t" +
                        max.ToString().PadLeft(7) + "\t(" +
                        sensor.Identifier + ")");
                }
            }
        }

        private static void GetParametersReport(IHardware[] hardwareCollection, String prefijo)
        {
            foreach (IHardware hardware in hardwareCollection)
            {
                hardware.Update();
                Console.WriteLine(prefijo + "|");
                Console.WriteLine(prefijo + "+- " + hardware.Name + " (" + hardware.Identifier + ")");
                if (hardware.SubHardware.Length > 0) GetParametersReport(hardware.SubHardware, "|  " + prefijo);
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
        
        //Metodos de ejemplo para acceder a toda la información (necesaria o no)
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
                if(sensor.Control != null) GetControl(sensor.Control, tab);
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

        //Metodos de comprobación de funcionamiento (comparación y determinación del correcto funcionamiento del sistema)
    }
}
