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
        private bool errorHardware;
        private bool errorRegistro;
        private bool errorEventos;
        
        public ResultadoEvaluacion()
        {
            fecha = DateTime.Now;
            errorHardware = false;
            errorRegistro = false;
            errorEventos = false;
        } 

        public ResultadoEvaluacion(bool errorHardware, bool errorRegistro, bool errorEventos)
        {
            fecha = DateTime.Now;
            this.errorHardware = errorHardware;
            this.errorRegistro = errorRegistro;
            this.errorEventos = errorEventos;
        }

        //True si hay algun fallo, False en caso contrario
        public bool HayError()
        {
            return (errorHardware || errorRegistro || errorEventos);
        }
    }
}
