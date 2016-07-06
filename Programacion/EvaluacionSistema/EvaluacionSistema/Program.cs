using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using System.Configuration;
using System.Diagnostics;

namespace EvaluacionSistema
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando programa...\r\n");

            //Pruebas

            //EvaluacionEventos.ReadLog();
            //Process.Start("schtasks.exe");
            //EvaluacionEventos.EvaluacionCompleta();
            //Console.Read();
            //return;
            
            MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["RemoteBBDD"].ConnectionString);
            
            try
            {
                Console.Write("Probando conexion con la BBDD... ");

                //Prueba de conexion a la BBDD
                conn.Open();

                Console.WriteLine("¡Conexion a la BBDD correcta!\r\n\r\n");
                
                if (ConfigurationManager.AppSettings["ModoEvaluacion"].ToString().CompareTo("Inicial") == 0)
                {
                    #region EvaluacionInicial

                    Console.WriteLine("Iniciando evaluacion inicial de la estacion\r\n");

                    MySqlTransaction sqltransaction = conn.BeginTransaction();

                    if (EvaluacionHardware.EvaluacionInicial(conn) && EvaluacionRegistro.EvaluacionInicial(conn))
                    {
                        sqltransaction.Commit();
                        Util.AddUpdateAppSettings("ModoEvaluacion", "Completa");

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

                    Console.Write("Iniciando evaluacion completa de la estacion...");

                    ResultadoEvaluacion resultado = new ResultadoEvaluacion(EvaluacionHardware.EvaluacionCompleta(conn),
                        EvaluacionRegistro.EvaluacionCompleta(conn),
                        EvaluacionEventos.EvaluacionCompleta(conn));

                    Console.WriteLine("\tEvaluacion completa finalizada!");
                    
                    #endregion EvaluacionCompleta

                    #region PostEvaluacion
                    
                    Util.EnviarInformes();

                    //Añadir registro a la tabla Evaluacion
                    //Añadir registro a la tabla Incidencia (si la hay) -- Cambiar esta tabla por la tabla Evento (IdEvento, Nivel, Origen, Categoria )

                    //Programar reejecucion del programa
                    //Programar ejecucion de scripts (y asociarlos al evento ocurrido para que en futuras ocasiones se ejecuten solos - si la empresa lo aprueba)

                    #region consultaAñadirEvaluacion
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

                    #endregion consultaAñadirEvaluacion

                    #endregion PostEvaluacion
                }

            }
            catch (MySqlException ex)
            {
                //Si no hay conexion a la BBDD remota usamos la local
                conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["LocalBBDD"].ConnectionString);
                conn.Open();
                //Revisar los metodos para que no se conecten al servidor y, cambiar las funciones que dependan de la red o quitarlas

                /*
                 SI NO HAY CONEXION:
                  - HARDWARE: COMPARACION CON BBDD LOCAL
                  - REGISTRO: OMITIR COMPROBACION DE ULTIMA VERSION Y USAR FICHERO LOCAL
                  - EVENTOS: BUSCAR ERRORES Y SI TIENEN SOLUCION EN LOCAL, SINO NO HACER NADA
                  - INFORMES: NO ENVIAR
                  - RESULTADOS EVALUACION QUE HABRIA QUE AÑADIR A LA BBDD A MODO DE HISTORIAL: NO ENVIAR
                  (VENDRIA A EQUIVALER A CAPAR TODAS LAS FUNCIONES QUE DEPENDAN DE RED Y SUSTITUIRLAS POR LOCAL O QUITARLAS)
                */
                Console.WriteLine("¡Conexion a la BBDD fallida!\r\n");
                Console.WriteLine("Error: {0}", ex.ToString());
                return;
            }
            finally
            {
                Console.Write("Cerrando conexion con la BBDD... ");

                conn.Close();

                Console.WriteLine("Conexion cerrada!");

                //Util.ProgramarReejecucion();
                //Util.ProgramarScripts();
                
                Console.WriteLine("\r\nFin del programa!");
            }
            
            Console.Read();
        }
    }
}
