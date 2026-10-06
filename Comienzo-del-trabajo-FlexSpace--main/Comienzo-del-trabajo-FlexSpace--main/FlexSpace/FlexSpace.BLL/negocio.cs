using System;
using System.Collections.Generic;
using FlexSpace.Entidades; 
using FlexSpace.DAL;
namespace FlexSpace.BLL

{
    public class ClienteSancionadoException : Exception
    {
        public ClienteSancionadoException(string mensaje) : base(mensaje) { }
    }

    public class ReservaBLL
    {
        private ClienteDAL _clienteDAL = new ClienteDAL();
        private PuestoDAL _puestoDAL = new PuestoDAL();
        private ReservaDAL _reservaDAL = new ReservaDAL();

        // 1. REGISTRAR NUEVA RESERVA
        public Reserva RegistrarReserva(int clienteId, int puestoId, DateTime fechaInicio, DateTime fechaFin)
        {
            if (fechaFin <= fechaInicio)
                throw new Exception("La fecha de fin debe ser posterior a la de inicio.");

            if (fechaInicio < DateTime.Now)
                throw new Exception("No se pueden realizar reservas para fechas pasadas.");

            // Buscar Cliente
            Cliente cliente = _clienteDAL.ObtenerPorId(clienteId);
            if (cliente == null)
                throw new Exception("El cliente especificado no existe.");

            // C. Regla de Bloqueo de Cliente
            if (cliente.SancionesActivas >= 3)
                throw new ClienteSancionadoException($"El cliente {cliente.Nombre} está bloqueado por acumular {cliente.SancionesActivas} sanciones.");

            // Buscar Puesto
            Puesto puesto = _puestoDAL.ObtenerPorId(puestoId);
            if (puesto == null)
                throw new Exception("El puesto especificado no existe.");

            // A. Validación de Disponibilidad (Sin Solapamiento)
            bool estaOcupado = _reservaDAL.ExisteSolapamiento(puestoId, fechaInicio, fechaFin);
            if (estaOcupado)
                throw new Exception("El puesto ya tiene una reserva confirmada en ese rango horario.");

            // B. Cálculo de la Tarifa
            decimal costoTotal = CalcularTarifa(cliente, puesto, fechaInicio, fechaFin);

            // Crear y guardar reserva
            Reserva nuevaReserva = new Reserva
            {
                ClienteId = clienteId,
                PuestoId = puestoId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Estado = "Confirmada",
                CostoTotal = costoTotal
            };

            _reservaDAL.Guardar(nuevaReserva);
            return nuevaReserva;
        }

        // B. CÁLCULO DINÁMICO DE LA TARIFA (Método Privado)
        public decimal CalcularTarifa(Cliente cliente, Puesto puesto, DateTime fechaInicio, DateTime fechaFin)
        {
            double totalHoras = (fechaFin - fechaInicio).TotalHours;

            // 1. Subtotal Base
            decimal subtotalBase = (decimal)totalHoras * puesto.TarifaBasePorHora;

            // 5. Penalización por Sanciones
            // Si el cliente tiene sanciones, pierde todos los descuentos y suma 20% extra sobre la tarifa base.
            if (cliente.SancionesActivas > 0)
            {
                return Math.Round(subtotalBase * 1.20m, 2);
            }

            decimal total = subtotalBase;

            // 2. Recargo por Fin de Semana (+15% sobre subtotal)
            if (EsFinDeSemana(fechaInicio, fechaFin))
            {
                total += subtotalBase * 0.15m;
            }

            // 3. Descuento por Volumen (-10% si dura 5 hs o más)
            if (totalHoras >= 5)
            {
                total -= total * 0.10m;
            }

            // 4. Beneficio VIP (-5% extra final)
            if (cliente.TipoCliente != null && cliente.TipoCliente.ToUpper() == "VIP")
            {
                total -= total * 0.05m;
            }

            return Math.Round(total, 2);
        }

        // 2. CANCELAR RESERVA
        public void CancelarReserva(int reservaId)
        {
            Reserva reserva = _reservaDAL.ObtenerPorId(reservaId);
            if (reserva == null)
                throw new Exception("No se encontró la reserva indicada.");

            if (reserva.Estado == "Cancelada")
                throw new Exception("La reserva ya está cancelada.");

            // Si falta menos de 2 horas para el inicio, suma +1 sanción activa
            TimeSpan tiempoRestante = reserva.FechaInicio - DateTime.Now;
            if (tiempoRestante.TotalHours < 2)
            {
                _clienteDAL.IncrementarSancion(reserva.ClienteId);
            }

            _reservaDAL.ActualizarEstado(reservaId, "Cancelada");
        }

        // 3. CONSULTAR RESERVAS ACTIVAS POR PUESTO
        public List<Reserva> ConsultarReservasActivasPorPuesto(string codigoPuesto)
        {
            Puesto puesto = _puestoDAL.ObtenerPorCodigo(codigoPuesto);
            if (puesto == null)
                throw new Exception("No existe un puesto con el código ingresado.");

            return _reservaDAL.ObtenerActivasPorPuesto(puesto.Id);
        }

        // 4. LISTAR CLIENTES SANCIONADOS
        public List<Cliente> ListarClientesSancionados()
        {
            return _clienteDAL.ObtenerClientesSancionados();
        }

        // AUXILIAR: Revisa si el rango abarca un sábado o domingo
        private bool EsFinDeSemana(DateTime inicio, DateTime fin)
        {
            for (DateTime dia = inicio.Date; dia <= fin.Date; dia = dia.AddDays(1))
            {
                if (dia.DayOfWeek == DayOfWeek.Saturday || dia.DayOfWeek == DayOfWeek.Sunday)
                    return true;
            }
            return false;
        }
    }
}
public class negocio
    {

        public abstract class CalculadorDescuentoCliente
        {
            public abstract decimal AplicarDescuento(decimal montoActual);
        }

        public class CalculadorEstandar : CalculadorDescuentoCliente
        {
            public override decimal AplicarDescuento(decimal montoActual)
            {
                return montoActual; // no tiene descuento VIP
            }
        }

        public class CalculadorVIP : CalculadorDescuentoCliente
        {
            public override decimal AplicarDescuento(decimal montoActual)
            {
                // Beneficio VIP
                return montoActual * 0.95m;
            }
        }
    }
}