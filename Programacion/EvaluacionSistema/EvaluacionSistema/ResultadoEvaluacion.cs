using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvaluacionSistema
{
    class ResultadoEvaluacion
    {
        private DateTime fecha;
        private int erroresHardware;
        private int erroresRegistro;
        private int erroresEventos;
        
        public ResultadoEvaluacion()
        {
            fecha = DateTime.Now;
            erroresHardware = 0;
            erroresRegistro = 0;
            erroresEventos = 0;
        } 

        public ResultadoEvaluacion(int erroresHardware, int erroresRegistro, int erroresEventos)
        {
            fecha = DateTime.Now;
            this.erroresHardware = erroresHardware;
            this.erroresRegistro = erroresRegistro;
            this.erroresEventos = erroresEventos;
        }

        public int GetErroresHardware()
        {
            return erroresHardware;
        }

        public int GetErroresRegistro()
        {
            return erroresRegistro;
        }

        public int GetErroresEventos()
        {
            return erroresEventos;
        }

        //True si hay algun fallo, False en caso contrario
        public bool HayError()
        {
            return (erroresHardware > 0|| erroresRegistro > 0 || erroresEventos > 0);
        }
    }
}
