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
        #region EvaluacionCompleta
        
        public static bool EvaluacionCompleta(MySqlConnection conn)
        {
            try
            {
                Console.WriteLine("---Eventos---\r\n");

                Console.Write("\tComprobando eventos del sistema... ");
                
                //List<EventRecord>[Critico, Error, Advertencia]
                List<EventRecord>[] eventos = ReadEventLog();

                Console.WriteLine("Eventos del sistema comprobados!");

                if (eventos[0].Count > 0 || eventos[1].Count > 0 || eventos[2].Count > 0)
                {
                    Informe(eventos);
                    if (eventos[0].Count > 0 || eventos[1].Count > 0)
                        ProgramarScripts(eventos, conn);
                    return true;
                }
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
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

        public static void ProgramarScripts(List<EventRecord>[] eventos, MySqlConnection conn)
        {
            bool insert = false;
            int id, qualifiers;
            byte version;
            string level, task, opcode;
            string modelo = ConfigurationManager.AppSettings["Modelo"];

            string sqlGet = "SELECT * FROM Evento WHERE ID = @id" + 
                " AND QUALIFIERS = @qualifiers" +
                " AND VERSION = @version" +
                " AND LEVEL = @level" +
                " AND TASK = @task" +
                " AND OPCODE = @opcode" +
                " AND MODELOESTACION = @modelo";

            string sqlInsert = "INSERT INTO Evento(ID, Qualifiers, Version, Level, Task, Opcode, ModeloEstacion) VALUES ";

            MySqlCommand cmd = new MySqlCommand(sqlGet, conn);
            cmd.Prepare();

            //Juntamos los eventos criticos y los de error en un solo array
            eventos[0].AddRange(eventos[1]);
            
            foreach (EventRecord evento in eventos[0])
            {
                //Obtener valores del evento
                id = evento.Id;
                qualifiers = (int)evento.Qualifiers;
                version = (byte)evento.Version;
                level = evento.LevelDisplayName;
                task = evento.TaskDisplayName;
                opcode = evento.OpcodeDisplayName;
                
                //Preparar consulta para comprobar si el evento ya se ha registrado
                cmd.Parameters.AddWithValue("@id", evento.Id);
                cmd.Parameters.AddWithValue("@qualifiers", evento.Qualifiers);
                cmd.Parameters.AddWithValue("@version", evento.Version);
                cmd.Parameters.AddWithValue("@level", evento.LevelDisplayName);
                cmd.Parameters.AddWithValue("@task", evento.TaskDisplayName);
                cmd.Parameters.AddWithValue("@opcode", evento.OpcodeDisplayName);
                cmd.Parameters.AddWithValue("@modelo", modelo);

                MySqlDataReader rdr = cmd.ExecuteReader();
                
                if (rdr.Read())
                {
                    //El evento ya ha sido registrado

                    if(rdr.GetInt32("ID_Solucion") != null) //Hay que revisar esta comprobacion
                    {
                        //Realizar consulta para obtener el nombre del script
                        //Comprobar si esta en local y sino descargarlo
                        //Programar la ejecucion del sxript y ademas asociarlo al evento para futuras ocasiones
                    }
                }
                else
                {
                    //El evento no ha sido registrado (y hay que hacerlo)
                    insert = true;

                    sqlInsert += "(" + id + ", '" + qualifiers + "'" + ", '" + version + "'" + ", '" + level + "'" +
                        ", '" + task + "'" + ", '" + opcode + "'" + ", '" + modelo + "'" + "), ";
                }

                rdr.Close();
            }

            if (insert)
            {
                sqlInsert.Remove(sqlInsert.LastIndexOf(","), 2);
                cmd.CommandText = sqlInsert;
                cmd.ExecuteNonQuery();
            }

            //Depende de si decido borrar los eventos o obtener solo los de un determinado intervalo de tiempo
            //EventLog eventLog = new EventLog();
            //eventLog.Log = "System";
            //eventLog.Clear();
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

        #region Utilidad/Ejemplos

        public static void ReadEventLogMalo()
        {
            //Para leer logs hace falta especificar el Log y el MachineName, aunque si no se especifica MachineName coge por defecto el local (".");
            //EventLog myLog = new EventLog();
            //myLog.Log = "System";
            //foreach (EventLogEntry entry in myLog.Entries)
            //{
            //    Console.WriteLine(
            //        "\r\nCategory:" + entry.Category +
            //        "\r\nCategoryNumber:" + entry.CategoryNumber +
            //        "\r\nContainer:" + entry.Container +
            //        "\r\nData:" + entry.Data +
            //        "\r\nEntryType:" + entry.EntryType +
            //        "\r\nEventId:" + entry.EventID +
            //        "\r\nIndex:" + entry.Index +
            //        "\r\nInstanceId:" + entry.InstanceId +
            //        "\r\nMachineName:" + entry.MachineName +
            //        "\r\nMessage:" + entry.Message +
            //        "\r\nReplacementStrings:" + entry.ReplacementStrings +
            //        "\r\nSite:" + entry.Site +
            //        "\r\nSource:" + entry.Source +
            //        "\r\nTimeGenerated:" + entry.TimeGenerated +
            //        "\r\nTimeWritten:" + entry.TimeWritten +
            //        "\r\nUserName:" + entry.UserName
            //        );
            //    Console.Read();
            //    foreach(String s in entry.ReplacementStrings)
            //    {
            //        Console.WriteLine("\t" + s);
            //    }
            //    foreach(Byte b in entry.Data)
            //    {
            //        Console.Write(b);
            //    }
            //Posible event.clear() para borrar las entradas
            //}
        }

        #endregion Utilidad/Ejemplos
    }
}
