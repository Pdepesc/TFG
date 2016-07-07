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

            MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["RemoteBBDD"].ConnectionString);

            //Pruebas

            //EvaluacionEventos.ReadLog();
            //Process.Start("schtasks.exe");
            conn.Open();
            EvaluacionEventos.EvaluacionCompleta(conn);
            Console.Read();
            return;

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

                    //Lo de programar reejecucion y hacer la consulta para añadir registro podria ir en el bloque finally (despues del try-catch)

                    //Añadir registro a la tabla Evaluacion (Cambiar los campos de tipo bool por tipo int en los que ponga el Nº de errores pudeindo ser 0)
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

                    //Programar reejecucion del programa

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

                  SI PONGO METODOS ESPECIFICOS PARA FUNCIONAR SIN CONEXION HABRA QUE ACTUALIZAR LOS DE QUE SÍ TIENEN CONEXION
                  PARA QUE ACTUALICEN LAS COSAS DE LA BBDD LOCAL DE MODO QUE LOS METODOS SIN CONEXION PUEDAN FUNCIONAR
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
                
                Console.WriteLine("\r\nFin del programa!");
            }
            
            Console.Read();
        }
    }
}
