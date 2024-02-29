namespace TrainsEditor.CommonLogic
{
    /// <summary>
    /// Číselná řada čísla linky (Ex, R, S, V apod.)
    /// </summary>
    enum TrainLineType
    {
        // Ex1 až Ex7
        Express,

        // R8 až R30
        FastTrain,

        // S1 až S99 PID
        Pid,

        // R40 až R49 PID
        PidFastTrain,

        // U1 až U99
        Duk,

        // L1 až L99
        Idol,

        // V1 až V99
        Iredo,

        // S1 až S99 ODIS
        Odis,

        // Ostatní
        Unknown,

        // Není definována žádná linka
        Undefined,
    }
}
