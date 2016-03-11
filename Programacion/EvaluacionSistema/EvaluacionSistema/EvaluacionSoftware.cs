using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace EvaluacionSistema
{
    class EvaluacionSoftware
    {
        //PerformanceCounter y otros (registros, WMI, ...)
        
        public static void GetCategorias()
        {
            PerformanceCounterCategory[] categorias = PerformanceCounterCategory.GetCategories();
            foreach (PerformanceCounterCategory categoria in categorias)
            {
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine(categoria.MachineName);
                Console.WriteLine(categoria.CategoryName + " - " + categoria.CategoryType);
                Console.WriteLine(categoria.CategoryHelp);
                //categoria.ReadCategory(); categoria.GetCounters(); categoria.GetInstanceNames();
            }
            /*
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Public\PerformanceCounters.txt"))
            {
                for (int i = 0; i < categorias.Length; i++)
                {
                    file.WriteLine("------------------------------------------------------");
                    file.WriteLine(categorias[i].CategoryName.ToString());
                    file.WriteLine("------------------------------------------------------");
                    getContadores(categorias[i].CategoryName.ToString(), file);
                }
            }
            */
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
            RegistryKey users = Registry.Users;
            RegistryKey performance = Registry.PerformanceData;
            RegistryKey local = Registry.LocalMachine;
            PrintKeys(users);
            PrintKeys(performance);
            PrintKeys(local);
        }

        private static void PrintKeys(RegistryKey rkey)
        {
            // Retrieve all the subkeys for the specified key.
            String[] names = rkey.GetSubKeyNames();

            int icount = 0;

            Console.WriteLine("Subkeys of " + rkey.Name);
            Console.WriteLine("-----------------------------------------------");

            // Print the contents of the array to the console.
            foreach (String s in names)
            {
                Console.WriteLine(s);

                // The following code puts a limit on the number
                // of keys displayed.  Comment it out to print the
                // complete list.
                //icount++;
                //if (icount >= 10)
                //    break;
            }
            Console.Read();
        } 
    }
}
