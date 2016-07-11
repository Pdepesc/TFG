using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvaluacionSistema
{
    class EvaluacionEventos
    {
        #region EvaluacionInicial

        public static bool EvaluacionInicial(MySqlConnection conn)
        {
            try
            {
                string query = "*[System[(Level = 1  or Level = 2 or Level = 3)]]";
                EventLogQuery eventsQuery = new EventLogQuery("System", PathType.LogName, query);
                EventLogReader logReader = new EventLogReader(eventsQuery);

                string id, qualifiers, version, level, task, opcode;
                string modelo = Util.ReadSetting("Modelo");

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.Prepare();

                for (EventRecord evento = logReader.ReadEvent(); evento != null; evento = logReader.ReadEvent())
                {
                    //Obtener valores del evento
                    id = evento.Id.ToString();
                    qualifiers = (evento.Qualifiers.HasValue) ? evento.Qualifiers.Value.ToString() : "null";
                    version = (evento.Version.HasValue) ? evento.Version.Value.ToString() : "null";
                    level = (evento.Level.HasValue) ? evento.Level.Value.ToString() : "null";
                    task = (evento.Task.HasValue) ? evento.Task.Value.ToString() : "null";
                    opcode = (evento.Opcode.HasValue) ? evento.Opcode.Value.ToString() : "null";

                    try
                    {
                        string sqlInsert = "INSERT INTO Evento_Solucion(ID, Qualifiers, Version, Level, Task, Opcode, ModeloEstacion) " +
                        "VALUES (" + id + ", '" + qualifiers + "'" + ", '" + version + "'" + ", '" + level + "'" +
                        ", '" + task + "'" + ", '" + opcode + "'" + ", '" + modelo + "'" + ")";

                        cmd.CommandText = sqlInsert;
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        //Si el evento ya esta insertado en la BBDD continuamos con el resto del bucle
                        continue;
                    }

                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\n\r\nError en la evaluacion inicial de los eventos: \r\n\t{0}", e.ToString());
                return false;
            }
        }

        #endregion EvaluacionInicial

        #region EvaluacionCompleta

        public static int EvaluacionCompleta(MySqlConnection conn)
        {
            try
            {
                Console.WriteLine("---Eventos---\r\n\r\n");

                Console.Write("\tComprobando eventos del sistema... ");
                
                //List<EventRecord>[Critico, Error, Advertencia]
                List<EventRecord>[] eventos = ReadEventLog();

                Console.WriteLine("Eventos del sistema comprobados!");

                if (eventos[0].Count > 0 || eventos[1].Count > 0 || eventos[2].Count > 0)
                {
                    Informe(eventos);
                    eventos[0].AddRange(eventos[1]);
                    eventos[0].AddRange(eventos[2]);
                    SolucionarEventos(eventos[0], conn);
                }
                return eventos[0].Count + eventos[1].Count + eventos[2].Count;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }
        }

        //List<EventRecord>[Critico, Error, Advertencia]
        public static List<EventRecord>[] ReadEventLog()
        {
            List<EventRecord> criticos = new List<EventRecord>();
            List<EventRecord> errores = new List<EventRecord>();
            List<EventRecord> advertencias = new List<EventRecord>();

            float time = float.Parse(Util.ReadSetting("IntervaloEjecucion")) * 60 * 60 * 1000; //horas * minutos * segundos * milisegundos

            string query = "*[System[(Level = 1  or Level = 2 or Level = 3) and " +
                "TimeCreated[timediff(@SystemTime) <= " + time + "]]]";
            EventLogQuery eventsQuery = new EventLogQuery("System", PathType.LogName, query);
            
            EventLogReader logReader = new EventLogReader(eventsQuery);

            for (EventRecord eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
            {
                // Read Event details
                if (eventdetail.Level == 1)
                    criticos.Add(eventdetail);
                else if (eventdetail.Level == 2)
                    errores.Add(eventdetail);
                else
                    advertencias.Add(eventdetail);
            }
            return new List<EventRecord>[]{ criticos, errores, advertencias};
        }

        public static void SolucionarEventos(List<EventRecord> eventos, MySqlConnection conn)
        {
            string id, qualifiers, version, level, task, opcode;
            string modelo = Util.ReadSetting("Modelo");
            string sqlGet = "SELECT * FROM Evento_Solucion WHERE ID = @id" +
                " AND QUALIFIERS = @qualifiers" +
                " AND VERSION = @version" +
                " AND LEVEL = @level" +
                " AND TASK = @task" +
                " AND OPCODE = @opcode" +
                " AND MODELOESTACION = @modelo";

            MySqlCommand cmd = new MySqlCommand(sqlGet, conn);
            cmd.Prepare();
            
            foreach (EventRecord evento in eventos)
            {
                //Obtener valores del evento
                id = evento.Id.ToString();
                qualifiers = (evento.Qualifiers.HasValue)? evento.Qualifiers.Value.ToString() : "null";
                version = (evento.Version.HasValue)? evento.Version.Value.ToString() : "null";
                level = (evento.Level.HasValue) ? evento.Level.Value.ToString() : "null";
                task = (evento.Task.HasValue) ? evento.Task.Value.ToString() : "null";
                opcode = (evento.Opcode.HasValue) ? evento.Opcode.Value.ToString() : "null";

                string nombreEvento = String.Join("-", new string[] { id, qualifiers, version, task, opcode, modelo });

                //Comprobamos que el evento no tenga ya asociado un script (para evitar duplicados)
                if (Util.ExisteScriptParaEvento(nombreEvento)) continue;

                //Preparar consulta para comprobar si el evento ya se ha registrado en la BBDD
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@qualifiers", qualifiers);
                cmd.Parameters.AddWithValue("@version", version);
                cmd.Parameters.AddWithValue("@level", level);
                cmd.Parameters.AddWithValue("@task", task);
                cmd.Parameters.AddWithValue("@opcode", opcode);
                cmd.Parameters.AddWithValue("@modelo", modelo);

                MySqlDataReader rdr = cmd.ExecuteReader();

                //Si el evento ya esta registrado en la BBDD
                if (rdr.Read())
                {
                    #region EventoEnBBDD

                    //Obtenemos el tipo de solucion (si la hay)
                    string tipoSolucion = rdr.GetString("TipoSolucion");

                    switch (tipoSolucion)
                    {
                        case "Script":
                            string nombreScript = rdr.GetString("UrlDescargaScript");
                            string pathScript = "Scripts/" + nombreScript;

                            //Esta en local?
                            if (!File.Exists(pathScript))
                            {
                                Util.SFTPDownload(pathScript, pathScript);
                            }

                            //si el bool lo ponemos a true el script se asocia al evento
                            Util.ProgramarScript(pathScript, nombreEvento);

                            break;
                        case "Manual":
                            //Avisar a tecnico
                            break;
                        case "SinSolucion":
                        case "Desconocida":
                            break;
                    }

                    #endregion EventoEnBBDD
                }
                //Si no esta registrado en la BBDD lo añadimos
                else
                {
                    #region EventoNoEnBBDD

                    string sqlInsert = "INSERT INTO Evento_Solucion(ID, Qualifiers, Version, Level, Task, Opcode, ModeloEstacion) " +
                        "VALUES (" + id + ", '" + qualifiers + "'" + ", '" + version + "'" + ", '" + level + "'" +
                        ", '" + task + "'" + ", '" + opcode + "'" + ", '" + modelo + "'" + ")";

                    cmd.CommandText = sqlInsert;
                    cmd.ExecuteNonQuery();

                    #endregion EventoNoEnBBDD
                }

                rdr.Close();
                cmd.CommandText = sqlGet;
                cmd.Parameters.Clear();
            }
        }

        #endregion EvaluacionCompleta

        #region Informe

        private static void Informe(List<EventRecord>[] eventos)
        {
            Console.Write("\tRealizando informe de Eventos....");

            String path = "Informes/InformeEventos-" +
                DateTime.Now.Day + "." +
                DateTime.Now.Month + "." +
                DateTime.Now.Year + " (" +
                DateTime.Now.Hour + "." +
                DateTime.Now.Minute + ").txt";
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine("Eventos Criticos"); sw.WriteLine();

                foreach (EventRecord eventoCritico in eventos[0])
                {
                    sw.WriteLine(eventoCritico.ToXml());
                }

                sw.WriteLine(); sw.WriteLine("Eventos de Error"); sw.WriteLine();

                foreach (EventRecord eventoError in eventos[1])
                {
                    sw.WriteLine(eventoError.ToXml());
                }

                sw.WriteLine(); sw.WriteLine("Eventos de Advertencia"); sw.WriteLine();

                foreach (EventRecord eventoAdvertencia in eventos[2])
                {
                    sw.WriteLine(eventoAdvertencia.ToXml());
                }
            }

            Console.WriteLine("Informe realizado!");Console.Write("\tRealizando informe de Hardware....");
        }

        #endregion Informe
    }
}
