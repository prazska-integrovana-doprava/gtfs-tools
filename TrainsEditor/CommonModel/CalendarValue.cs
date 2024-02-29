namespace TrainsEditor.CommonModel
{
    /// <summary>
    /// Určuje pro daný den vypravení vlaku (použito v <see cref="SingleTrainFile.BitmapEx"/>)
    /// </summary>
    public enum CalendarValue
    {
        /// <summary>
        /// Záznam pro tento den není aktivní (vlak nejede). V případě canel záznamů znamená, že vlak v tento den není rušen.
        /// </summary>
        Inactive = 0,

        /// <summary>
        /// Záznam je pro daný den aktivní (vlak jede). V případě cancel záznamů znamená, že vlak je v tento den zrušen.
        /// </summary>
        Active = 1,

        /// <summary>
        /// Záznam pro tento den byl aktivní, ovšem je přepsán jiným souborem ve skupině, který je aktivní místo tohoto (příp. zrušen).
        /// </summary>
        Overwritten = 10,
    }
}
