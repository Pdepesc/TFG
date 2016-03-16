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
            Console.WriteLine("1: Hardware - Evaluación");
            Console.WriteLine("2: Software - Evaluación");
            Console.WriteLine("3: Softdware - GetCategorias");
            Console.WriteLine("4: Softdware - GetRegistro");
            String var = Console.ReadLine();
            switch (var)
            {
                case "1":
                    EvaluacionHardware.GetReport();
                    break;
                case "2":
                    EvaluacionSoftware.GetReport();
                case "3":
                    EvaluacionSoftware.GetCategorias();
                    break;
                case "4":
                    EvaluacionSoftware.GetRegistro();
                    break;
                default: break;
            }
            //Software
            //EvaluacionSoftware.GetCategorias(); //Falta obtener contadores de cada categoria y valores de cada contador
            //EvaluacionSoftware.GetRegistro();   //No todo el registro por ahora
            Console.Read();
        }
    }
}
