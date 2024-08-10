using CommonLibrary;
using System.Collections.Generic;
using System.Linq;

namespace AswModel.Extended
{
    /// <summary>
    /// Jeden průjezd konkrétního spoje zastávkou
    /// </summary>
    public class StopTime
    {
        /// <summary>
        /// Trip, ke kterému zastavení patří
        /// </summary>
        public Trip Trip { get; set; }

        /// <summary>
        /// Čas příjezdu do zastávky. U výchozí zastávky obsahuje nedefinovanou hodnotu.
        /// </summary>
        public Time ArrivalTime { get; set; }

        /// <summary>
        /// Čas odjezdu ze zastávky. U cílové zastávky obsahuje nedefinovanou hodnotu.
        /// </summary>
        public Time DepartureTime { get; set; }
        
        /// <summary>
        /// Zastávka
        /// </summary>
        public Stop Stop { get; set; }

        /// <summary>
        /// Je zastavení pro veřejnost? Pozor na to, že může být veřejné zastavení v neveřejné zastávce, je třeba vždy kontrolovat i Stop.IsPublic!
        /// </summary>
        public bool IsForPublic { get; set; }

        /// <summary>
        /// Pouze pro nástup
        /// </summary>
        public bool BoardingOnly { get; set; }

        /// <summary>
        /// Pouze pro výstup
        /// </summary>
        public bool ExitOnly { get; set; }

        /// <summary>
        /// Kombinace <see cref="IsForPublic"/> a veřejnosti zastávky. Pokud je true, je zastavení veřejné, pokud je false, jde buď o neveřejnou zastávku, nebo neveřejné zastavení v zastávce.
        /// </summary>
        public bool IsPublic { get { return IsForPublic && Stop.IsPublic; } }
        
        /// <summary>
        /// True, pokud je zastavení jen na znamení
        /// </summary>
        public bool IsRequestStop { get; set; }

        /// <summary>
        /// Pouze na objednání
        /// </summary>
        public bool IsOnPhoneRequest { get; set; }

        /// <summary>
        /// Popis trasy do této zastávky (číslo závodu + předchozí zastávka + tato zastávka + číslo varianty trasy).
        /// Je vždy vyplněno, může ale odkazovat na neexistující trasu (např. vždy v první zastávce bude předchozí zastávka = null).
        /// Pokud trasa existuje, je vyplněno <see cref="TrackToThisStop"/>.
        /// </summary>
        public ShapeFragmentDescriptor TrackVariantDescriptor { get; set; }
        
        /// <summary>
        /// Trasa z předchozí zastávky do této zastávky (null, pokud neexistuje/nebyla nalezena, nebo pokud jde o první zastávku na trase)
        /// - odpovídá trase nalezené podle <see cref="TrackVariantDescriptor"/>
        /// </summary>
        public ShapeFragment TrackToThisStop { get; set; }

        /// <summary>
        /// Změna směru u polookružních linek (zastávky až do této mají cíl tuto zastávku,
        /// zbylé pak cílovou zastávku celého tripu).
        /// </summary>
        public bool DirectionChange { get; set; }

        /// <summary>
        /// True, pokud jde o deponovací místo (vozovna apod.)
        /// </summary>
        public bool IsDepot { get; set; }

        /// <summary>
        /// Poznámky odjezdu
        /// </summary>
        public Remark[] Remarks { get; set; }

        /// <summary>
        /// Typ výkonu na odjezdu ze zastávky
        /// </summary>
        public TripOperationType TripOperationType { get; set; }

        /// <summary>
        /// Návazné poznámky k odjezdu
        /// </summary>
        public IEnumerable<Remark> TimedTransferRemarks
        {
            get { return Remarks.Where(r => r.IsTimedTransfer); }
        }

        public override string ToString()
        {
            return $"{Trip} at {Stop} at {DepartureTime}";
        }
    }
}
