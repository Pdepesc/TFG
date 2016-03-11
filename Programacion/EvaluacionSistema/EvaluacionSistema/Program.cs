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
            //Hardware
            EvaluacionHardware.GetReport();     //Metodo de la clase OpenHardwareMonitor que devuelve un informe
            EvaluacionHardware.GetHardware();   //Metodo hecho por mi que accede a las componentes y devulve valores e informacion

            //Software
            EvaluacionSoftware.GetCategorias(); //Falta obtener contadores de cada categoria y valores de cada contador
            EvaluacionSoftware.GetRegistro();   //No todo el registro por ahora
            Console.Read();
        }
    }
}
