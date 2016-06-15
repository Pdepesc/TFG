using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.IO;
using MySql.Data.MySqlClient;

namespace EvaluacionSistema
{
    class EvaluacionContadores
    {
        #region EvaluacionCompleta

        public static void EvaluacionCompleta(MySqlConnection conn, Properties properties)
        {
            String path = "Informes/InformeContadores-" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + ".txt";
            using (StreamWriter sw = File.CreateText(path))
            {
                GetReport(sw);
            }
        }

        #endregion EvaluacionCompleta

        #region PostEvaluacion

        public static void PostEvaluacion()
        {

        }

        #endregion PostEvaluacion

        public static void GetReport(StreamWriter sw)
        {
            sw.WriteLine("--------------------------------------------------------------------------------"); sw.WriteLine();
            GetCategories(sw);
        }

        //PerformanceCounterCategory[]
        public static void GetCategories(StreamWriter sw)
        {
            PerformanceCounterCategory[] categorias = PerformanceCounterCategory.GetCategories();
            foreach (PerformanceCounterCategory categoria in categorias)
            {
                sw.WriteLine("CategoryName: " + categoria.CategoryName);
                sw.WriteLine("CategoryHelp: " + categoria.CategoryHelp);
                sw.WriteLine("CategoryType: " + categoria.CategoryType);     //SingleInstance / MultiInstance
                sw.WriteLine("GetType (): " + categoria.GetType());
                sw.WriteLine("Nº Instances: " + categoria.GetInstanceNames().Length);
                if (categoria.GetInstanceNames().Length > 0)      //MultiInstance
                    GetInstances(sw, categoria);
                else                                              //SingleInstance
                    ReadCounters(sw, categoria.GetCounters());
                ReadCategory(sw, categoria);
            }
            //for (int x = 0; x < 2; x++)
            //{
            //    PerformanceCounterCategory categoria = categorias[x];
            //    sw.WriteLine("CategoryName: " + categoria.CategoryName);
            //    sw.WriteLine("CategoryHelp: " + categoria.CategoryHelp);
            //    sw.WriteLine("CategoryType: " + categoria.CategoryType);
            //    sw.WriteLine("GetType (): " + categoria.GetType());
            //    sw.WriteLine("Nº Instances: " + categoria.GetInstanceNames().Length);
            //    if (categoria.GetInstanceNames().Length > 0)
            //        GetInstances(sw, categoria);
            //    else
            //        ReadCounters(sw, categoria.GetCounters());
            //    ReadCategory(sw, categoria);
            //}
        }

        //PerformanceCounterCategory.Instances[]
        private static void GetInstances(StreamWriter sw, PerformanceCounterCategory categoria)
        {
            String[] instancias = categoria.GetInstanceNames();
            foreach (String instancia in instancias)
            {
                sw.WriteLine("Instance '" + instancia + "'");
                ReadCounters(sw, categoria.GetCounters(instancia));
            }
            //for (int x = 0; x < ((instancias.Length <= 2) ? instancias.Length : 2); x++)
            //{
            //    String instancia = instancias[x];
            //    sw.WriteLine("Instance '" + instancia + "'");
            //    ReadCounters(sw, categoria.GetCounters(instancia));
            //}
        }

        //PerformanceCounter
        private static void ReadCounters(StreamWriter sw, PerformanceCounter[] contadores)
        {
            sw.WriteLine("Nº Counters: " + contadores.Length);
            foreach (PerformanceCounter contador in contadores)
            {
                sw.WriteLine();
                sw.WriteLine("\tCounter Name: " + contador.CounterName);
                sw.WriteLine("\tCategory Name: " + contador.CategoryName);
                sw.WriteLine("\tCounter Help: " + contador.CounterHelp);
                sw.WriteLine("\tCounter Type: " + contador.CounterType);
                sw.WriteLine("\tGet Type (): " + contador.GetType());
                sw.WriteLine("\tInstance Lifetime: " + contador.InstanceLifetime);
                sw.WriteLine("\tInstance Name: " + contador.InstanceName);
                sw.WriteLine("\tMachine Name: " + contador.MachineName);
                sw.WriteLine("\tNext Value (): " + contador.NextValue());
                sw.WriteLine("\tNext Value () again: " + contador.NextValue());
                sw.WriteLine("\tRaw Value: " + contador.RawValue);
                sw.WriteLine("\tReadOnly: " + contador.ReadOnly);
                sw.WriteLine();
            }
            //for (int x = 0; x < ((contadores.Length <= 2) ? contadores.Length : 2); x++)
            //{
            //    PerformanceCounter contador = contadores[x];
            //    sw.WriteLine();
            //    sw.WriteLine("\tCounter Name: " + contador.CounterName);
            //    sw.WriteLine("\tCategory Name: " + contador.CategoryName);
            //    sw.WriteLine("\tCounter Help: " + contador.CounterHelp);
            //    sw.WriteLine("\tCounter Type: " + contador.CounterType);
            //    sw.WriteLine("\tGet Type (): " + contador.GetType());
            //    sw.WriteLine("\tInstance Lifetime: " + contador.InstanceLifetime);
            //    sw.WriteLine("\tInstance Name: " + contador.InstanceName);
            //    sw.WriteLine("\tMachine Name: " + contador.MachineName);
            //    sw.WriteLine("\tNext Value (): " + contador.NextValue());
            //    sw.WriteLine("\tNext Value () again: " + contador.NextValue());
            //    sw.WriteLine("\tRaw Value: " + contador.RawValue);
            //    sw.WriteLine("\tReadOnly: " + contador.ReadOnly);
            //    sw.WriteLine();
            //}
        }
        
        private static void ReadCategory(StreamWriter sw, PerformanceCounterCategory categoria)
        {
            InstanceDataCollectionCollection idColCol = categoria.ReadCategory();

            ICollection idColColKeys = idColCol.Keys;
            string[] idCCKeysArray = new string[idColColKeys.Count];
            idColColKeys.CopyTo(idCCKeysArray, 0);

            ICollection idColColValues = idColCol.Values;
            InstanceDataCollection[] idCCValuesArray = new InstanceDataCollection[idColColValues.Count];
            idColColValues.CopyTo(idCCValuesArray, 0);

            sw.WriteLine("InstanceDataCollectionCollection for \"{0}\" " +
                "has {1} elements.", categoria.CategoryName, idColCol.Count);

            // Display the InstanceDataCollectionCollection Keys and Values.
            // The Keys and Values collections have the same number of elements.
            int index;
            int limit = (idCCKeysArray.Length <= 2) ? idCCKeysArray.Length : 2;
            //for (index = 0; index < idCCKeysArray.Length; index++)
            for (index = 0; index < limit; index++)
            {
                sw.WriteLine("  Next InstanceDataCollectionCollection " +
                    "Key is \"{0}\"", idCCKeysArray[index]);
                ProcessInstanceDataCollection(sw, idCCValuesArray[index]);
                sw.WriteLine();
            }
        }
        
        // Display the contents of an InstanceDataCollection.
        public static void ProcessInstanceDataCollection(StreamWriter sw, InstanceDataCollection idCol)
        {

            ICollection idColKeys = idCol.Keys;
            string[] idColKeysArray = new string[idColKeys.Count];
            idColKeys.CopyTo(idColKeysArray, 0);

            ICollection idColValues = idCol.Values;
            InstanceData[] idColValuesArray = new InstanceData[idColValues.Count];
            idColValues.CopyTo(idColValuesArray, 0);

            sw.WriteLine("  InstanceDataCollection for \"{0}\" " +
                "has {1} elements.", idCol.CounterName, idCol.Count);

            // Display the InstanceDataCollection Keys and Values.
            // The Keys and Values collections have the same number of elements.
            int index;
            int limit = (idColKeysArray.Length <= 2)? idColKeysArray.Length : 2;
            //for (index = 0; index < idColKeysArray.Length; index++)
            for (index = 0; index < limit; index++)
            {
                sw.WriteLine("    Next InstanceDataCollection " +
                    "Key is \"{0}\"", idColKeysArray[index]);
                ProcessInstanceDataObject(sw, idColValuesArray[index]);
            }
        }

        // Display the contents of an InstanceData object.
        public static void ProcessInstanceDataObject(StreamWriter sw, InstanceData instData)
        {
            CounterSample sample = instData.Sample;

            sw.WriteLine("    From InstanceData:\r\n      " +
                "InstanceName: {0,-31} RawValue: {1}", instData.InstanceName, instData.Sample.RawValue);
            sw.WriteLine("    From CounterSample:\r\n      " +
                "CounterType: {0,-32} SystemFrequency: {1}\r\n" +
                "      BaseValue: {2,-34} RawValue: {3}\r\n" +
                "      CounterFrequency: {4,-27} CounterTimeStamp: {5}\r\n" +
                "      TimeStamp: {6,-34} TimeStamp100nSec: {7}", sample.CounterType, sample.SystemFrequency, sample.BaseValue, sample.RawValue, sample.CounterFrequency, sample.CounterTimeStamp, sample.TimeStamp, sample.TimeStamp100nSec);
        }


        
        public static void GetCategorias()
        {
            Console.WriteLine("Realizando reporte...");
            PerformanceCounterCategory[] categorias = PerformanceCounterCategory.GetCategories();
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Public\SoftwareCategorias.txt"))
            {
                foreach (PerformanceCounterCategory categoria in categorias)
                {
                    file.WriteLine("-----------------------------------------------------------------------");
                    file.WriteLine(categoria.CategoryName + " - " + categoria.CategoryType);
                    file.WriteLine(categoria.CategoryHelp);
                    file.WriteLine("Instancias: " + categoria.GetInstanceNames().Count());
                    foreach (String instancia in categoria.GetInstanceNames())
                    {
                        file.WriteLine("\t" + instancia);
                    }
                    ReadCategoria(categoria, file);   //Devuelve el nombre de los contadores
                                                //categoria.GetCounters() -> da excepciones; categoria.GetInstanceNames();
                }
            }
            Console.WriteLine("Reporte finalizado!");
        }

        private static void ReadCategoria(PerformanceCounterCategory categoria, System.IO.StreamWriter file)
        {
            InstanceDataCollectionCollection idColCol = categoria.ReadCategory();
            //Console.WriteLine("InstanceDataCollectionCollection for \"{0}\" " + "has {1} elements.", categoria.CategoryName, idColCol.Count);
            file.WriteLine("Contadores: " + idColCol.Count);

            foreach (String key in idColCol.Keys)
            {
                file.WriteLine("\t" + key);
            }
        }

        private static void GetContadores(String categoria, System.IO.StreamWriter file)
        {
            string[] instanceNames;
            ArrayList counters = new ArrayList();
            PerformanceCounterCategory mycat = new PerformanceCounterCategory(categoria);
            // Retrieve the counters.
            try
            {
                instanceNames = mycat.GetInstanceNames();
                if (instanceNames.Length == 0)
                {
                    foreach (PerformanceCounter counter in mycat.GetCounters())
                    {
                        var unused = counter.NextValue(); // first call will always return 0
                        //System.Threading.Thread.Sleep(1000); // wait a second, then try again
                        file.WriteLine(counter.CounterName.ToString() + ": " + counter.NextValue() + " - " + counter.NextValue());
                    }
                }
                else
                {
                    for (int i = 0; i < instanceNames.Length; i++)
                    {
                        file.WriteLine("---INSTANCIA " + i + ": " + instanceNames[i] + "---");
                        foreach (PerformanceCounter counter in mycat.GetCounters(instanceNames[i]))
                        {
                            var unused = counter.NextValue(); // first call will always return 0
                            //System.Threading.Thread.Sleep(10); // wait a second, then try again
                            file.WriteLine(counter.CounterName.ToString() + ": " + counter.NextValue() + " - " + counter.NextValue());
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                file.WriteLine(
                    "Unable to list the counters for this category:\n"
                    + ex.Message);
            }
        }
        
    }
}
