using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Microsoft.Win32;

namespace EvaluacionSistema
{
    class Program
    {
        private static String cs = @"server=192.168.1.10;userid=paris;password=paris;database=TFG";

        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando programa...");

            Properties properties = new Properties("Properties.properties");
            MySqlConnection conn = new MySqlConnection(cs);

            Console.WriteLine();
            
            try
            {
                Console.WriteLine("Probando conexion con la BBDD...");

                //Prueba de conexion a la BBDD
                conn.Open();

                Console.WriteLine("¡Conexion a la BBDD correcta!\r\n\r\n");
                
                if (properties.get("TipoEvaluacion").CompareTo("Inicial") == 0)
                {
                    #region EvaluacionInicial
                    
                    MySqlTransaction sqltransaction = conn.BeginTransaction();

                    if (EvaluacionHardware.EvaluacionInicial(conn, properties) && EvaluacionRegistro.EvaluacionInicial(conn, properties))
                    {
                        sqltransaction.Commit();
                        properties.set("TipoEvaluacion", "Completa");
                        properties.Save();
                    }
                    else
                    {
                        sqltransaction.Rollback();
                    }

                    Console.WriteLine("\r\n\r\nFin de la evaluacion inicial!");

                    #endregion
                }
                //Evaluacion completa y deteccion y solucion de errores
                else
                {
                    #region EvaluacionCompleta
                    /*

                    List<String[]>[] registrosDefectuosos = EvaluacionRegistro.EvaluacionCompleta();  //List<String[ruta, nombreClave]>[Corregidos, no corregidos]
                    if(registrosDefectuosos.Count > 0)
                        EvaluacionRegistro.EnviarInforme();    //

                    
                    bool errorHardware = EvaluacionHardware.EvaluacionCompleta();
                    if(errorHardware)
                        EvaluacionHardware.EnviarInforme();



                    UTILIZAR UNA CLASE INCIDENCIA QUE ALMACENE LOS STRINGS CON EL ERROR HARDWARE Y EL ERROR SOFTWARE? Y SE UTILICE COMO
                    VALOR DEVUELTO POR LOS METODOS EVALUACIONHARDWARE.EVALUACION Y EVALUACIONSOFTWARE.EVALUACION O YO K SE
                    
                    //Realizar comprobaciones para ver que todo funca bien
                    int errorHardware = 0;
                    List<List<String>> registrosDefectuosos;
                    int errorSoftware = 0;

                    

                    registrosDefectuosos = EvaluacionSoftware.EvaluacionRegistro(); return List < List < String >> registrosMalos; (Clave, valor, tipo)
         
                         EvaluacionSoftware.EvaluacionContadores(errorSoftware);

                    if (registrosDefectuosos.Count() > 0)
                        //Comprobar si hay actualizacion del Registro (nueva version) -- Quizas esto haya que comprobarlo antes de evaluar
                        //Actualizar fichero con el registro bueno
                        //Actualizar registros defectuosos con los valores buenos
                        //Enviar informe con los resultados (registros modificados y version del registro a la que se ha actualizado)

                        if (errorHardware > 0)
                            //Enviar informe de que es lo que esta fuera de los valores normales

                            if (errorSoftware > 0)
                    //Enviar informe de los errores encontrados -- Para esto tendria que esperar a la contestacion de la empresa
                    */
                    //FIN DE LA EVALUACION COMPLETA
                    #endregion

                    #region PostEvaluacion

                    #endregion
                }

            } catch (MySqlException ex)
            {
                //Ejecutar script para intentar arreglar la conexion
                //Reejecutar el programa una vez arreglado el fallo
                //Quitar el mensaje de abajo o cambiarlo a un log o yo k se
                Console.WriteLine("¡Conexion a la BBDD fallida!");
                Console.WriteLine("Error: {0}", ex.ToString());
            }

            conn.Close();
            /*
            Console.Read();
            
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
            }*/
            Console.Read();
        }
    }
}
