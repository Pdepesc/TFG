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

            String tab = "\t";

            foreach (var hardware in myPc.Hardware)
            {
                hardware.Update();
                Console.WriteLine(hardware);
                Console.WriteLine(tab + hardware.Name);
                Console.WriteLine(tab + hardware.HardwareType);
                Console.WriteLine(tab + hardware.Identifier);
                Console.WriteLine(tab + hardware.Sensors + " - " + hardware.Sensors.Length);
                Console.WriteLine(tab + hardware.SubHardware + " - " + hardware.SubHardware.Length);
                getSubHardware(hardware, tab);
            }
            
            Console.Read();

        }

        //llamada recursiva para explorar el arbol de hardware y subhardware
        public static void getSubHardware(IHardware hardware, String tab)
        {
            tab = tab + "\t";
            foreach (IHardware subhardware in hardware.SubHardware)
            {
                subhardware.Update();
                Console.WriteLine(subhardware);
                Console.WriteLine(tab + subhardware.Name);
                Console.WriteLine(tab + subhardware.HardwareType);
                Console.WriteLine(tab + subhardware.Identifier);
                Console.WriteLine(tab + subhardware.Parent.Name);
                Console.WriteLine(tab + subhardware.Sensors + " - " + subhardware.Sensors.Length);
                Console.WriteLine(tab + subhardware.SubHardware + " - " + subhardware.SubHardware.Length);
                getSubHardware(subhardware, tab);
            }
        }
    }
}
