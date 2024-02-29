namespace AswModel.Extended
{
    /// <summary>
    /// Odkaz na zastávku v databázi - používá se v poznámkách <see cref="Remark"/> pro určení směru příjezdu návazného spoje
    /// (tam se to nedá převést na konkrétní sloupek, protože pravidla umožňují, aby bylo shodné jen číslo uzlu).
    /// </summary>
    public class StopRef
    {
        /// <summary>
        /// Číslo uzlu. Musí být zadáno.
        /// </summary>
        public int NodeId { get; private set; }

        /// <summary>
        /// Číslo zastávky. Může být 0, pokud není určeno nebo na něm nezáleží.
        /// </summary>
        public int StopId { get; private set; }

        public StopRef(int nodeId, int stopId)
        {
            NodeId = nodeId;
            StopId = stopId;
        }

        public override bool Equals(object obj)
        {
            var other = obj as StopRef;
            if (other == null)
                return false;

            return NodeId == other.NodeId && StopId == other.StopId;
        }

        public override int GetHashCode()
        {
            return NodeId.GetHashCode() * 1751 + StopId.GetHashCode();
        }

        public override string ToString()
        {
            if (StopId != 0)
                return $"{NodeId}/{StopId}";
            else
                return $"{NodeId}/?";
        }
    }
}
