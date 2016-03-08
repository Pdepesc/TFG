using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pruebas
{
    class Contadores
    {
        public static void getCategorias()
        {
            PerformanceCounterCategory[] categorias;
            // Retrieve the categories.
            categorias = PerformanceCounterCategory.GetCategories();
            // Add the retrieved categories to the list. 
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
        }

        public static void getContadores(String categoria, System.IO.StreamWriter file)
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
