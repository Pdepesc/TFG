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

            /*

            if(fichero.getProperty('EvaluacionInicial') == 0){
                EvaluacionInicial.Evaluacion();
                //Añadir la estacion a la BBDD (obtener NombreEstacion, Modelo, VersionRegistro por teclado o del fichero de propiedades)
                //Añadir los componentes Hardware a la BBDD con sus respectivos valores (Minimo, Maximo, Media, Ultimo)
                //Comprobar versión del Registro y comprobar que todos los registros están bien configurados
                    //Si el registro está mal, arreglarlo
                //Hacer evaluacion inicial del software (PerformanceCounters) o no -- Depende de lo que digan en la empresa
                fichero.setProperty('EvaluacionInicial', '1');
            }
            else {
                //Realizar comprobaciones para ver que todo funca bien
                int errorHardware = 0;
                List<List<String>> registrosDefectuosos;
                int errorSoftware = 0;

                EvaluacionHardware.Evaluacion(errorHardware);
                    //Descargar valores de fabrica de la propia estacion 
                    //Descargar valores de las estaciones con el mismo modelo
                    //Hacer comprobaciones

                registrosDefectuosos = EvaluacionSoftware.EvaluacionRegistro(); return List<List<String>> registrosMalos; (Clave, valor, tipo)

                EvaluacionSoftware.EvaluacionContadores(errorSoftware);

                if(registrosDefectuosos.Count() > 0)
                    //Comprobar si hay actualizacion del Registro (nueva version) -- Quizas esto haya que comprobarlo antes de evaluar
                    //Actualizar fichero con el registro bueno
                    //Actualizar registros defectuosos con los valores buenos
                    //Enviar informe con los resultados (registros modificados y version del registro a la que se ha actualizado)

                if(errorHardware > 0)
                    //Enviar informe de que es lo que esta fuera de los valores normales

                if(errorSoftware > 0)
                    //Enviar informe de los errores encontrados -- Para esto tendria que esperar a la contestacion de la empresa

            }


            */
            

            EvaluacionInicial.Evaluacion();

            Console.WriteLine("Escoge una opción:");
            Console.WriteLine("1: Hardware - Evaluación (Reporte)");
            Console.WriteLine("2: Hardware - GetAll");
            Console.WriteLine("3: Software - Evaluación (Reporte) --En construccion--");
            Console.WriteLine("4: Software - GetAll (PerformanceCounters) --Prueba limitada a 2 items--");
            Console.WriteLine("5: Software - GetPerformanceCounters in a file");
            Console.WriteLine("6: Software - GetAll (RegistryKey) --En construccion--");
            String var = Console.ReadLine();
            switch (var)
            {
                case "1":
                    EvaluacionHardware.GetReport();
                    break;
                case "2":
                    EvaluacionHardware.GetHardware();
                    break;
                case "3":
                    EvaluacionSoftware.GetReport(); //En construcción (a partir del GetCategories)
                    break;
                case "4":
                    EvaluacionSoftware.GetCategories();
                    break;
                case "5":
                    EvaluacionSoftware.GetCategorias(); //En construccion
                    break;
                case "6":
                    EvaluacionSoftware.GetRegistro(); //En construccion
                    break;
                default: break;
            }
            Console.Read();
        }
    }
}
