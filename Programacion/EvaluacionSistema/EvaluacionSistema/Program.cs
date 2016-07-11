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
                                                     
            try
            {
                Console.Write("Probando conexion con la BBDD... ");

                //Prueba de conexion a la BBDD remota
                conn.Open();

                Console.WriteLine("¡Conexion a la BBDD correcta!\r\n\r\n");
                
                if (Util.ReadSetting("ModoEvaluacion").ToString().CompareTo("Inicial") == 0)
                {
                    #region EvaluacionInicial

                    Util.RegistrarEstacion(conn);

                    Console.WriteLine("Iniciando evaluacion inicial de la estacion\r\n\r\n");

                    MySqlTransaction sqltransaction = conn.BeginTransaction();

                    if (EvaluacionHardware.EvaluacionInicial(conn) && 
                        EvaluacionRegistro.EvaluacionInicial(conn) &&
                        EvaluacionEventos.EvaluacionInicial(conn))
                    {
                        sqltransaction.Commit();
                        Util.InicializarTareaProgramada();
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

                    Console.WriteLine("Iniciando evaluacion completa de la estacion...");

                    ResultadoEvaluacion resultado = new ResultadoEvaluacion(
                        EvaluacionHardware.EvaluacionCompleta(conn),
                        EvaluacionRegistro.EvaluacionCompleta(conn),
                        EvaluacionEventos.EvaluacionCompleta(conn));

                    Console.WriteLine("\r\nEvaluacion completa finalizada!");
                    
                    #endregion EvaluacionCompleta

                    #region PostEvaluacion
                    
                    Util.EnviarInformes();

                    #region consultaAñadirEvaluacion
                    
                    String sql = "INSERT INTO Evaluacion(ID_Estacion, Fecha, ErroresHardware, ErroresRegistro, ErroresEventos) " + 
                            "VALUES (@idestacion, @fecha, @erroresHardware, @erroresRegistro, @erroresEventos)";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Prepare();

                    cmd.Parameters.AddWithValue("@idestacion", Util.ReadSetting("IdEstacion"));
                    cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                    cmd.Parameters.AddWithValue("@erroresHardware", resultado.GetErroresHardware());
                    cmd.Parameters.AddWithValue("@erroresRegistro", resultado.GetErroresRegistro());
                    cmd.Parameters.AddWithValue("@erroresEventos", resultado.GetErroresEventos());
                    
                    cmd.ExecuteNonQuery();

                    #endregion consultaAñadirEvaluacion

                    #endregion PostEvaluacion
                }

            }
            catch (MySqlException ex)
            {
                Console.WriteLine("¡Conexion a la BBDD remota fallida!\r\n");
                Console.WriteLine("Error: {0}", ex.ToString());
                Console.WriteLine("Conectando a la BBDD local");
                //Si no hay conexion a la BBDD remota usamos la local
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["LocalBBDD"].ConnectionString;
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
            }
            finally
            {
                Console.Write("Cerrando conexion con la BBDD... ");

                conn.Close();

                Console.WriteLine("Conexion cerrada!");

                Util.ProgramarReejecucion();
                
                Console.WriteLine("\r\nFin del programa!");
            }
            
            Console.Read();
        }
    }
}
