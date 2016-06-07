using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;

using OpenHardwareMonitor.Hardware;

namespace EvaluacionSistema
{
    class EvaluacionInicial
    {
        private static Computer miPc = new Computer() { CPUEnabled = true, FanControllerEnabled = true, GPUEnabled = true, HDDEnabled = true, MainboardEnabled = true, RAMEnabled = true };

        public static void Evaluacion()
        {
            Properties properties = new Properties("Configuracion.properties");
            string cs = @"server=192.168.1.10;userid=paris;password=paris;database=tfg";
            MySqlConnection conn = new MySqlConnection(cs);


            if (properties.get("EvaluacionInicial").CompareTo("0") == 0) {
                try
                {
                    //Abrir conexion con MySQL
                    conn.Open();
                    MySqlTransaction sqltransaction = conn.BeginTransaction();

                    try {
                        //Añadir esta estación a la BBDD y obtener su ID
                        string sql = "INSERT INTO estacion(Estacion, Modelo, VersionRegistro) VALUES (@estacion, @modelo, @version)";

                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.Prepare();

                        cmd.Parameters.AddWithValue("@estacion", properties.get("Estacion"));
                        cmd.Parameters.AddWithValue("@modelo", properties.get("Modelo"));
                        cmd.Parameters.AddWithValue("@version", properties.get("VersionRegistro"));
                        cmd.ExecuteNonQuery();

                        long id = cmd.LastInsertedId;
                        Console.WriteLine(id);

                        //Actualizar componentes hardware 100 veces
                        Console.WriteLine("Actualizando componentes hardware...");
                        ActualizarHardware();
                        Console.WriteLine("Actualizacion finalizada!");

                        //Leer componenetes Hardware y guardarlos en la BBDD
                        sql = LeerCompoenentes(id, miPc.Hardware);
                        Console.WriteLine("Ejecutar la siguiente consulta: \r\n" + sql);
                        Console.Read();
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();

                        sqltransaction.Commit();

                        Console.WriteLine("Insercion en la BBDD realizada!");

                        //Modificar fichero de propiedades
                        properties.set("EvaluacionInicial", 1);
                        properties.Save();
                    }
                    catch (MySqlException ex)
                    {
                        sqltransaction.Rollback();
                        Console.WriteLine("Error: {0}", ex.ToString());
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("Error: {0}", ex.ToString());
                }
            }

            conn.Close();
        }

        private static void ActualizarHardware()
        {
            miPc.Open();
            int contador = 0;
            while (contador++ != 101)
                ActualizarComponentes(miPc.Hardware); Thread.Sleep(100);
        }

        private static void ActualizarComponentes(IHardware[] hardwareCollection)
        {
            foreach (IHardware hardware in hardwareCollection)
            {
                hardware.Update();
                if (hardware.SubHardware.Length > 0) ActualizarComponentes(hardware.SubHardware);
            }
        }
        
        private static string LeerCompoenentes(long id, IHardware[] hardwareCollection)
        {
            string sql = "INSERT INTO hardware(IDEstacion, Componente, Sensor, Identificador, Minimo, Maximo, Media, Ultimo) VALUES ";
            foreach (IHardware hardware in hardwareCollection)
            {
                foreach (ISensor sensor in hardware.Sensors)
                {
                    sql += "(" + id
                        + ", '" + hardware.Name + "'"
                        + ", '" + sensor.Name + "'"
                        + ", '" + sensor.Identifier + "'"
                        + ", '" + (float)(Math.Truncate((Convert.ToDouble(sensor.Min)) * 100.0) / 100.0) + "'"
                        + ", '" + (float)(Math.Truncate((Convert.ToDouble(sensor.Max)) * 100.0) / 100.0) + "'"
                        + ", '" + (float)(Math.Truncate((Convert.ToDouble(Media(sensor.Values))) * 100.0) / 100.0) + "'"
                        + ", '" + (float)(Math.Truncate((Convert.ToDouble(sensor.Values.ElementAt<SensorValue>(sensor.Values.Count() - 1).Value)) * 100.0) / 100.0) + "'"
                        + "), ";
                }
            }
            return sql.Remove(sql.LastIndexOf(","), 2);
        }

        private static float Media(IEnumerable<SensorValue> valores)
        {
            float suma = 0;
            foreach (SensorValue valor in valores)
            {
                suma += valor.Value;
            }
            return suma / valores.Count();
        }
    }
}
