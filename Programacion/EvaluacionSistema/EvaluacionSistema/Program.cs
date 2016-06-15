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
            Console.WriteLine("Iniciando programa...\r\n");

            Properties properties = new Properties("Properties.properties");
            MySqlConnection conn = new MySqlConnection(cs);
            
            try
            {
                Console.Write("Probando conexion con la BBDD... ");

                //Prueba de conexion a la BBDD
                conn.Open();

                Console.WriteLine("¡Conexion a la BBDD correcta!\r\n\r\n");
                
                if (properties.get("TipoEvaluacion").CompareTo("Inicial") == 0)
                {
                    #region EvaluacionInicial

                    Console.WriteLine("Iniciando evaluacion inicial de la estacion\r\n");
                    
                    MySqlTransaction sqltransaction = conn.BeginTransaction();

                    if (EvaluacionHardware.EvaluacionInicial(conn, properties) && EvaluacionRegistro.EvaluacionInicial(conn, properties))
                    {
                        sqltransaction.Commit();
                        properties.set("TipoEvaluacion", "Completa");
                        properties.Save();

                        Console.WriteLine("\r\n\r\nEvaluacion inicial finalizada con éxito!");
                    }
                    else
                    {
                        sqltransaction.Rollback();

                        Console.WriteLine("\r\n\r\nEvaluacion inicial finalizada sin éxito!");
                    }

                    #endregion
                }
                else
                {
                    #region EvaluacionCompleta

                    Console.Write("Iniciando evaluacion completa de la estacion");

                    //List<String[Identificador, componente, sensor]>
                    List<String[]> fallosHardware = EvaluacionHardware.EvaluacionCompleta(conn, properties);
                    
                    //List<String[ruta, nombreClave]>[Corregidos, NoCorregidos]
                    List<String[]>[] fallosRegistro = EvaluacionRegistro.EvaluacionCompleta(conn, properties);

                    //TO-DO: Hacer evaluacion completa de contadores y establecer metricas para detectar fallos
                    //var fallosContadores = EvaluacionContadores.EvaluacionCompleta(conn, properties);

                    #endregion

                    #region PostEvaluacion

                    if (fallosHardware != null && fallosHardware.Count > 0)
                        EvaluacionHardware.PostEvaluacion(fallosHardware);

                    if ((fallosRegistro[0] != null && fallosRegistro[0].Count > 0) || (fallosRegistro[1] != null && fallosRegistro[1].Count > 0))
                        EvaluacionRegistro.PostEvaluacion(fallosRegistro);



                        //Enviar informes (si hay errores)
                        //Añadir registro a la tabla Evaluacion
                        //Añadir registro a la tabla Incidencia (si la hay)
                        //Ejecutar scripts

                        //Incidencia incidencia = new Incidencia(fallosHardware, fallosRegistro, fallosContadores);
                        
                        //AÑADIR NUEVA INCIDENCIA A LA BBDD
                        //COMPROBAR SI ESTA RESUELTA O NO
                        //ACTUAR EN CONSECUENCIA
                        

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
                //TO-DO: ¿Qué hacer si no hay conexion a la BBDD?
                    //Ejecutar script para intentar arreglar la conexion
                    //Reejecutar el programa una vez arreglado el fallo
                    //Quitar el mensaje de abajo o cambiarlo a un log o yo k se
                Console.WriteLine("¡Conexion a la BBDD fallida!\r\n");
                Console.WriteLine("Error: {0}", ex.ToString());
                return;
            }

            Console.Write("Cerrando conexion con la BBDD... ");

            conn.Close();

            Console.WriteLine("Conexion cerrada!\r\n\r\nFin del programa!");
            Console.Read();
        }
    }
}
