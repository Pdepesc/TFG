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
            //string query = "*[System[(Level=1  or Level=2) and TimeCreated[timediff(@SystemTime) <= 86400000]]]";
            string query = "*[System[(Level = 1  or Level = 2)]]";
            EventLogQuery eventsQuery = new EventLogQuery("System", PathType.LogName, query);

            try
            {
                EventLogReader logReader = new EventLogReader(eventsQuery);

                string id, qualifiers, version, level, task, opcode;
                string modelo = ConfigurationManager.AppSettings["Modelo"];

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
                        Console.WriteLine("Evento añadido");
                    }
                    catch (MySqlException e)
                    {
                        Console.WriteLine("Evento duplicado");
                        continue;
                    }

                }
                return true;
            }
            catch (EventLogNotFoundException e)
            {
                Console.WriteLine("Error while reading the event logs");
                return false;
            }
        }

        #endregion EvaluacionInicial

        #region EvaluacionCompleta

        public static int EvaluacionCompleta(MySqlConnection conn)
        {
            try
            {
                Console.WriteLine("---Eventos---\r\n");

                Console.Write("\tComprobando eventos del sistema... ");
                
                //List<EventRecord>[Critico, Error, Advertencia]
                List<EventRecord>[] eventos = ReadEventLog();   //Lee eventos desde el ultimo inicio del sistema

                Console.WriteLine("Eventos del sistema comprobados!");

                if (eventos[0].Count > 0 || eventos[1].Count > 0 || eventos[2].Count > 0)
                {
                    Informe(eventos);
                    if (eventos[0].Count > 0 || eventos[1].Count > 0)
                    {
                        eventos[0].AddRange(eventos[1]);
                        SolucionarEventos(eventos[0], conn);
                    }
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

            PerformanceCounter systemUpTime = new PerformanceCounter("System", "System Up Time");

            systemUpTime.NextValue();
            float time = systemUpTime.NextValue() * 1000;

            string query = "*[System[(Level = 1  or Level = 2 or Level = 3) and TimeCreated[timediff(@SystemTime) <= " + time + "]]]";
            //string query = "*[System[(Level = 1  or Level = 2 or Level = 3) and TimeCreated[timediff(@SystemTime) <= 43200000]]]";
            EventLogQuery eventsQuery = new EventLogQuery("System", PathType.LogName, query);

            try
            {
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
            catch (EventLogNotFoundException e)
            {
                Console.WriteLine("Error while reading the event logs");
                return null;
            }
        }

        public static void SolucionarEventos(List<EventRecord> eventos, MySqlConnection conn)
        {
            string id, qualifiers, version, level, task, opcode;
            string modelo = ConfigurationManager.AppSettings["Modelo"];
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
                            Util.ProgramarScript(pathScript, nombreEvento, false, id, task);

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

        public static void Informe(List<EventRecord>[] eventos)
        {
            Console.Write("\tPostEvaluacion de Eventos....");

            String path = "Informes/InformeEventos-" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + ".txt";
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

            Console.WriteLine("Informe de Eventos hecho!");
        }

        #endregion Informe
    }
}
