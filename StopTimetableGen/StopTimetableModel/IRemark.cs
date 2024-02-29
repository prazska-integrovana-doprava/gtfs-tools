namespace StopTimetableGen.StopTimetableModel
{
    /// <summary>
    /// Předpis pro poznámku ke spoji nebo zastávce
    /// </summary>
    public interface IRemark
    {
        /// <summary>
        /// Symbol, který poznámku reprezentuje u odjezdu nebo v seznamu zastávek
        /// </summary>
        string Symbol { get; }

        /// <summary>
        /// Textová vysvětlivka k symbolu.
        /// Podporuje i __xy__ pro podtržení textu
        /// </summary>
        string Text { get; }
    }
}
