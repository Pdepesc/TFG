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

                        Console.WriteLine("\r\nEvaluacion inicial finalizada con éxito!");
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

                    Console.WriteLine("Iniciando evaluacion completa de la estacion...\r\n");

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
                Console.WriteLine("\r\n¡Conexion a la BBDD remota fallida!\r\n");
                Console.WriteLine("Error: {0}", ex.ToString());

                //Console.WriteLine("Conectando a la BBDD local");
                ////Si no hay conexion a la BBDD remota usamos la local
                //conn.ConnectionString = ConfigurationManager.ConnectionStrings["LocalBBDD"].ConnectionString;
                //conn.Open();
                
                /*
                 **ASUMIMOS QUE DURANTE LA EVALUACION INICIAL HABRA CONEXION AL SERVIDOR
                  - BLOQUE: Comportamiento que deberia tener sin conexion (Qué hacer cuando haya conexion)

                  - HARDWARE: COMPARACION CON BBDD LOCAL 
                                (actualizar BBDD local, tablas Estacion y Hardware y, vista Medias)
                  - REGISTRO: COMPARACION CON FICHERO LOCAL 
                                (actualizar BBDD local, tablas Registro y Estacion;
                                    comprobar version del fichero y, si es necesario, actualizarlo)
                  - EVENTOS: BUSCAR EVENTOS Y EJECUTAR LOS SCRIPTS DISPONIBLES EN LOCAL QUE CORRESPONDAN 
                                (actualizar BBDD local, tablas Evento_Solucion;
                                    descargar scripts disponibles en el servidor)
                  - INFORMES: GENERARLOS PERO NO ENVIARLOS
                                (enviar todos los que haya y borrarlos tras el envio)
                  - RESULTADOS EVALUACION: REFLEJARLOS EN LA BBDD LOCAL
                                (sincronizar BBDD local a Remota, INSERT/UPDATE tabla Evaluacion)
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
