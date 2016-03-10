using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;

namespace EvaluacionSistema
{
    class Program
    {
        static void Main(string[] args)
        {

            Computer myPc = new Computer() { CPUEnabled=true, FanControllerEnabled=true, GPUEnabled=true, HDDEnabled=true, MainboardEnabled=true, RAMEnabled=true };

            myPc.Open();
            Console.WriteLine(myPc.GetReport());

            getHardware(myPc);
            
            myPc.Close();
            Console.Read();

        }

        public static void getHardware(Computer pc)
        {
            String tab = "\t";
            foreach (IHardware hardware in pc.Hardware)
            {
                hardware.Update();
                Console.WriteLine(hardware);
                Console.WriteLine(tab + hardware.HardwareType + " - " + hardware.Name);
                Console.WriteLine(tab + hardware.Identifier);
                Console.WriteLine(tab + "Sensores - " + hardware.Sensors.Length);
                if(hardware.Sensors.Length > 0) getSensores(hardware, tab);
                Console.WriteLine(tab + "SubHardware - " + hardware.SubHardware.Length);
                if(hardware.SubHardware.Length > 0) getSubHardware(hardware, tab);
            }
        }
        
        public static void getSubHardware(IHardware hardware, String tab)
        {
            tab = tab + "\t";
            foreach (IHardware subhardware in hardware.SubHardware)
            {
                subhardware.Update();
                Console.WriteLine(tab + subhardware);
                Console.WriteLine(tab + subhardware.HardwareType + " - " + subhardware.Name);
                Console.WriteLine(tab + subhardware.Identifier);
                Console.WriteLine(tab + "Sensores - " + subhardware.Sensors.Length);
                if (subhardware.Sensors.Length > 0) getSensores(subhardware, tab);
                Console.WriteLine(tab + "SubHardware - " + subhardware.SubHardware.Length);
                if (subhardware.SubHardware.Length > 0) getSubHardware(subhardware, tab);
            }
        }

        public static void getSensores(IHardware hardware, String tab)
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
                if (sensor.Parameters.Length > 0) getParametrosSensor(sensor, tab);
                Console.WriteLine(tab + "Valores - " + sensor.Values.Count());
                if (sensor.Values.Count() > 0) getValoresSensor(sensor, tab);
            }
        }

        public static void getParametrosSensor(ISensor sensor, String tab)
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

        public static void getValoresSensor(ISensor sensor, String tab)
        {
            tab = tab + "\t";
            foreach (SensorValue valor in sensor.Values)
            {
                Console.WriteLine(tab + "- " + valor.Time + ": " + valor.Value);
            }
        }
    }
}
