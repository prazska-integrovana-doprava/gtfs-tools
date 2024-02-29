namespace StopTimetableGen.StopTimetableModel
{
    /// <summary>
    /// Interpretuje se jako oddělovač (triviálně prázdná řádka)
    /// </summary>
    class SeparatorRemark : IRemark
    {
        public string Symbol { get { return ""; } }

        public string Text { get { return ""; } }
    }
}
