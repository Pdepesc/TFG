using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvaluacionSistema
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Escoge una opción:");
            Console.WriteLine("1: Hardware - GetReport");
            Console.WriteLine("2: Hardware - GetHardware");
            Console.WriteLine("3: Softdware - GetCategorias");
            Console.WriteLine("4: Softdware - GetRegistro");
            String var = Console.ReadLine();
            switch (var)
            {
                case "1": EvaluacionHardware.GetReport();   //Fichero de texto
                    break;
                case "2":
                    EvaluacionHardware.GetHardware();
                    break;
                case "3":
                    EvaluacionSoftware.GetCategorias();
                    break;
                case "4":
                    EvaluacionSoftware.GetRegistro();
                    break;
                default: break;
            }
            //Hardware
            //EvaluacionHardware.GetReport();     //Metodo de la clase OpenHardwareMonitor que devuelve un informe
            //EvaluacionHardware.GetHardware();   //Metodo hecho por mi que accede a las componentes y devulve valores e informacion

            //Software
            //EvaluacionSoftware.GetCategorias(); //Falta obtener contadores de cada categoria y valores de cada contador
            //EvaluacionSoftware.GetRegistro();   //No todo el registro por ahora
            Console.Read();
        }
    }
}
