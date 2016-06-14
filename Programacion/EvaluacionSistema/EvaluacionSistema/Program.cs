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
                else
                {
                    #region EvaluacionCompleta

                    //List<String[Identificador, componente, sensor]>
                    List<String[]> fallosHardware = EvaluacionHardware.EvaluacionCompleta(conn, properties);
                    
                    //List<String[ruta, nombreClave]>[Corregidos, NoCorregidos]
                    List<String[]>[] fallosRegistro = EvaluacionRegistro.EvaluacionCompleta(conn, properties);

                    //var fallosContadores = EvaluacionContadores.EvaluacionCompleta(conn, properties);

                    #endregion

                    #region PostEvaluacion
                
                    //Incidencia incidencia = new Incidencia(fallosHardware, fallosRegistro, fallosContadores);
                    /*
                    
                    //AÑADIR NUEVA INCIDENCIA A LA BBDD
                    //COMPROBAR SI ESTA RESUELTA O NO
                    //ACTUAR EN CONSECUENCIA
                        
                    */

                    //PUEDE QUE LOS METODOS POSTEVALUACION LOS JUNTE EN UNA SOLA CLASE LLAMADA POSTEVALUACION/INCIDENCIA
                    //QUE ALMACENE TODOS LOS OBJETOS DEVUELVOS POR LOS METODOS EVALUACIONCOMPLETA Y LOS PROCESE
                    //  SE ENCARGARÍA DE ENVIAR INFORMES AL SERVIDOR Y APLICAR SCRIPTS O SOLUCIONES

                    /*
                    String sql = "INSERT INTO Evaluacion(ID_Estacion, Fecha, ErrorHardware, ErrorRegistro, ErrorContadores) " + 
                            "VALUES (@idestacion, @fecha, @errorHardware, @errorRegistro, @errorContadores)";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Prepare();

                    cmd.Parameters.AddWithValue("@idestacion", properties.get("IdEstacion"));
                    cmd.Parameters.AddWithValue("@fecha", DateTime.Now);

                    if (fallosHardware != null && fallosHardware.Count > 0)
                    {
                        EvaluacionHardware.PostEvaluacion(fallosHardware);
                        cmd.Parameters.AddWithValue("@errorHardware", 1);
                    }
                    else
                        cmd.Parameters.AddWithValue("@errorHardware", 0);

                    if (fallosRegistro[0].Count > 0 || fallosRegistro[1].Count > 0)
                    {
                        EvaluacionRegistro.PostEvaluacion(fallosRegistro);
                        cmd.Parameters.AddWithValue("@errorRegistro", 1);
                    }
                    else
                        cmd.Parameters.AddWithValue("@errorHardware", 0);

                    if(fallosContadores > 0)
                    {
                        EvaluacionContadores.PostEvaluacion(fallosContadores);
                        cmd.Parameters.AddWithValue("@errorContadores", 1);
                    }
                    else
                        cmd.Parameters.AddWithValue("@errorContadores", 0);

                    cmd.ExecuteNonQuery();
                    */

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
