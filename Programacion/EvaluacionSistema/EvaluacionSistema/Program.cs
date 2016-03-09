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
                Console.WriteLine(tab + hardware.Name + " - " + hardware.HardwareType);
                Console.WriteLine(tab + hardware.Identifier);
                Console.WriteLine(tab + "Sensores - " + hardware.Sensors.Length);
                getSensores(hardware, tab);
                Console.WriteLine(tab + "SubHardware - " + hardware.SubHardware.Length);
                getSubHardware(hardware, tab);
            }
        }
        
        public static void getSubHardware(IHardware hardware, String tab)
        {
            tab = tab + "\t";
            foreach (IHardware subhardware in hardware.SubHardware)
            {
                subhardware.Update();
                Console.WriteLine(tab + subhardware);
                Console.WriteLine(tab + subhardware.Name + " - " + subhardware.HardwareType);
                Console.WriteLine(tab + subhardware.Identifier);
                Console.WriteLine(tab + subhardware.Parent.Name);
                Console.WriteLine(tab + "Sensores - " + subhardware.Sensors.Length);
                getSensores(subhardware, tab);
                Console.WriteLine(tab + "SubHardware - " + subhardware.SubHardware.Length);
                getSubHardware(subhardware, tab);
            }
        }

        public static void getSensores(IHardware hardware, String tab)
        {
            tab = tab + "\t";
            foreach (ISensor sensor in hardware.Sensors)
            {
                Console.WriteLine(tab + sensor.Name);
                Console.WriteLine(tab + sensor.SensorType);
                Console.WriteLine(tab + sensor.Identifier);
                Console.WriteLine(tab + sensor.Control);
                Console.WriteLine(tab + sensor.Hardware);
                Console.WriteLine(tab + sensor.Index);
                Console.WriteLine(tab + sensor.IsDefaultHidden);
                Console.WriteLine(tab + sensor.Max);
                Console.WriteLine(tab + sensor.Min);
                Console.WriteLine(tab + "Parametros - " + sensor.Parameters.Length); //getParameters() IParameter
                getParametrosSensor(sensor, tab);
                Console.WriteLine(tab + sensor.Value);
                Console.WriteLine(tab + "Valores - " + sensor.Values.Count());     //getValues() SensorValue
                getValoresSensor(sensor, tab);
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
                Console.WriteLine(tab + valor.Time + ": " + valor.Value);
            }
        }
    }
}
