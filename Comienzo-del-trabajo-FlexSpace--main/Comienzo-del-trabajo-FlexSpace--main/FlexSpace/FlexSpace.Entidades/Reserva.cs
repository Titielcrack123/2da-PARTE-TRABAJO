using System;
using System.Collections.Generic;
using System.Text;

namespace FlexSpace.Entidades
{
    public enum EstadoReserva
    {
        Confirmada,
        Cancelada,
        Finalizada
    }
    public class Reserva
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }

        public int PuestoId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public EstadoReserva EstadoReserva { get; set; }
        public decimal CostoFinal { get; set; }
    }
}
