using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Microsoft.Win32;

namespace EvaluacionSistema
{
    class EvaluacionSoftware
    {
        //PerformanceCounter y otros (registros, WMI, ...)
        
        public static void GetReport()
        {
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine();
            GetCategories();
            Console.Read();
        }

        //PerformanceCounterCategory[]
        public static void GetCategories()
        {
            PerformanceCounterCategory[] categorias = PerformanceCounterCategory.GetCategories();
            //foreach (PerformanceCounterCategory categoria in categorias)
            //{
            //    Console.WriteLine("CategoryName: " + categoria.CategoryName);
            //    Console.WriteLine("CategoryHelp: " + categoria.CategoryHelp);
            //    Console.WriteLine("CategoryType: " + categoria.CategoryType);     //SingleInstance / MultiInstance
            //    Console.WriteLine("GetType (): " + categoria.GetType());
            //    Console.WriteLine("Nº Instances: " + categoria.GetInstanceNames().Length);
            //    if (categoria.GetInstanceNames().Length > 0)      //MultiInstance
            //        GetInstances(categoria);
            //    else                                              //SingleInstance
            //        ReadCounters(categoria.GetCounters());
            //    ReadCategory(categoria);
            //}
            for (int x = 0; x < 2; x++)
            {
                PerformanceCounterCategory categoria = categorias[x];
                Console.WriteLine("CategoryName: " + categoria.CategoryName);
                Console.WriteLine("CategoryHelp: " + categoria.CategoryHelp);
                Console.WriteLine("CategoryType: " + categoria.CategoryType);
                Console.WriteLine("GetType (): " + categoria.GetType());
                Console.WriteLine("Nº Instances: " + categoria.GetInstanceNames().Length);
                if (categoria.GetInstanceNames().Length > 0)
                    GetInstances(categoria);
                else
                    ReadCounters(categoria.GetCounters());
                ReadCategory(categoria);
            }
        }

        //PerformanceCounterCategory.Instances[]
        private static void GetInstances(PerformanceCounterCategory categoria)
        {
            String[] instancias = categoria.GetInstanceNames();
            //foreach (String instancia in instancias)
            //{
            //    Console.WriteLine("Instance '" + instancia + "'");
            //    ReadCounters(categoria.GetCounters(instancia));
            //}
            for (int x = 0; x < ((instancias.Length <= 2) ? instancias.Length : 2); x++)
            {
                String instancia = instancias[x];
                Console.WriteLine("Instance '" + instancia + "'");
                ReadCounters(categoria.GetCounters(instancia));
            }
        }

        //PerformanceCounter
        private static void ReadCounters(PerformanceCounter[] contadores)
        {
            Console.WriteLine("Nº Counters: " + contadores.Length);
            //foreach (PerformanceCounter contador in contadores)
            //{
            //    Console.WriteLine();
            //    Console.WriteLine("\tCounter Name: " + contador.CounterName);
            //    Console.WriteLine("\tCategory Name: " + contador.CategoryName);
            //    Console.WriteLine("\tCounter Help: " + contador.CounterHelp);
            //    Console.WriteLine("\tCounter Type: " + contador.CounterType);
            //    Console.WriteLine("\tGet Type (): " + contador.GetType());
            //    Console.WriteLine("\tInstance Lifetime: " + contador.InstanceLifetime);
            //    Console.WriteLine("\tInstance Name: " + contador.InstanceName);
            //    Console.WriteLine("\tMachine Name: " + contador.MachineName);
            //    Console.WriteLine("\tNext Value (): " + contador.NextValue());
            //    Console.WriteLine("\tNext Value () again: " + contador.NextValue());
            //    Console.WriteLine("\tRaw Value: " + contador.RawValue);
            //    Console.WriteLine("\tReadOnly: " + contador.ReadOnly);
            //    Console.WriteLine();
            //}
            for(int x = 0; x < ((contadores.Length <= 2) ? contadores.Length : 2); x++)
            {
                PerformanceCounter contador = contadores[x];
                Console.WriteLine();
                Console.WriteLine("\tCounter Name: " + contador.CounterName);
                Console.WriteLine("\tCategory Name: " + contador.CategoryName);
                Console.WriteLine("\tCounter Help: " + contador.CounterHelp);
                Console.WriteLine("\tCounter Type: " + contador.CounterType);
                Console.WriteLine("\tGet Type (): " + contador.GetType());
                Console.WriteLine("\tInstance Lifetime: " + contador.InstanceLifetime);
                Console.WriteLine("\tInstance Name: " + contador.InstanceName);
                Console.WriteLine("\tMachine Name: " + contador.MachineName);
                Console.WriteLine("\tNext Value (): " + contador.NextValue());
                Console.WriteLine("\tNext Value () again: " + contador.NextValue());
                Console.WriteLine("\tRaw Value: " + contador.RawValue);
                Console.WriteLine("\tReadOnly: " + contador.ReadOnly);
                Console.WriteLine();
            }
        }
        
        private static void ReadCategory(PerformanceCounterCategory categoria)
        {
            InstanceDataCollectionCollection idColCol = categoria.ReadCategory();

            ICollection idColColKeys = idColCol.Keys;
            string[] idCCKeysArray = new string[idColColKeys.Count];
            idColColKeys.CopyTo(idCCKeysArray, 0);

            ICollection idColColValues = idColCol.Values;
            InstanceDataCollection[] idCCValuesArray = new InstanceDataCollection[idColColValues.Count];
            idColColValues.CopyTo(idCCValuesArray, 0);

            Console.WriteLine("InstanceDataCollectionCollection for \"{0}\" " +
                "has {1} elements.", categoria.CategoryName, idColCol.Count);

            // Display the InstanceDataCollectionCollection Keys and Values.
            // The Keys and Values collections have the same number of elements.
            int index;
            int limit = (idCCKeysArray.Length <= 2) ? idCCKeysArray.Length : 2;
            //for (index = 0; index < idCCKeysArray.Length; index++)
            for (index = 0; index < limit; index++)
            {
                Console.WriteLine("  Next InstanceDataCollectionCollection " +
                    "Key is \"{0}\"", idCCKeysArray[index]);
                ProcessInstanceDataCollection(idCCValuesArray[index]);
                Console.WriteLine();
            }
        }

        // Display the contents of an InstanceDataCollection.
        public static void ProcessInstanceDataCollection(InstanceDataCollection idCol)
        {

            ICollection idColKeys = idCol.Keys;
            string[] idColKeysArray = new string[idColKeys.Count];
            idColKeys.CopyTo(idColKeysArray, 0);

            ICollection idColValues = idCol.Values;
            InstanceData[] idColValuesArray = new InstanceData[idColValues.Count];
            idColValues.CopyTo(idColValuesArray, 0);

            Console.WriteLine("  InstanceDataCollection for \"{0}\" " +
                "has {1} elements.", idCol.CounterName, idCol.Count);

            // Display the InstanceDataCollection Keys and Values.
            // The Keys and Values collections have the same number of elements.
            int index;
            int limit = (idColKeysArray.Length <= 2)? idColKeysArray.Length : 2;
            //for (index = 0; index < idColKeysArray.Length; index++)
            for (index = 0; index < limit; index++)
            {
                Console.WriteLine("    Next InstanceDataCollection " +
                    "Key is \"{0}\"", idColKeysArray[index]);
                ProcessInstanceDataObject(idColValuesArray[index]);
            }
        }

        // Display the contents of an InstanceData object.
        public static void ProcessInstanceDataObject(InstanceData instData)
        {
            CounterSample sample = instData.Sample;

            Console.WriteLine("    From InstanceData:\r\n      " +
                "InstanceName: {0,-31} RawValue: {1}", instData.InstanceName, instData.Sample.RawValue);
            Console.WriteLine("    From CounterSample:\r\n      " +
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

        //REGISTRO DE WINDOWS
        public static void GetRegistro()
        {
            Console.WriteLine("Iniciando reporte del registro...");
            using (StreamWriter registro = new StreamWriter(@"C:\Users\Public\Registro.txt"))
            using (StreamWriter registroInaccesible = new StreamWriter(@"C:\Users\Public\RegistrosInaccesibles.txt"))
            {
                registro.WriteLine(Registry.ClassesRoot.Name);
                PrintKeys(Registry.ClassesRoot, registro, registroInaccesible);
                Console.WriteLine("ClassesRoot done");
                registro.WriteLine(Registry.CurrentConfig.Name);
                PrintKeys(Registry.CurrentConfig, registro, registroInaccesible);
                Console.WriteLine("CurrentConfig done");
                registro.WriteLine(Registry.CurrentUser.Name);
                PrintKeys(Registry.CurrentUser, registro, registroInaccesible);
                Console.WriteLine("CurrentUser done");
                registro.WriteLine(Registry.LocalMachine.Name);
                PrintKeys(Registry.LocalMachine, registro, registroInaccesible);
                Console.WriteLine("LocalMachine done");
                registro.WriteLine(Registry.Users.Name);
                PrintKeys(Registry.Users, registro, registroInaccesible);
                Console.WriteLine("Users done");
            }
            Console.WriteLine("Reporte finalizado!");
            Console.Read();
        }

        //TODO: algunos valores de registro los coge mal (multistring y byte)
        //      tratar IOException de caracteres no unicode
        private static void PrintKeys(RegistryKey rkey, StreamWriter registro, StreamWriter registroInaccesible)
        {
            String[] subkeys = rkey.GetSubKeyNames();
            String[] values = rkey.GetValueNames();

            foreach (String value in values)
            {
                registro.WriteLine(rkey.Name + "\\" + value + ": " /*+ rkey.GetValue(value)*/ + " (" + rkey.GetValueKind(value) + ")");
            }

            foreach (String s in subkeys)
            {
                try { 
                    RegistryKey subrkey = rkey.OpenSubKey(s);
                    PrintKeys(subrkey, registro, registroInaccesible);
                }
                catch (System.Security.SecurityException e)
                {
                    registroInaccesible.WriteLine(rkey.Name + "\\" + s);
                }
            }
        } 
    }
}
