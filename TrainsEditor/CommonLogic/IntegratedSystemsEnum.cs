using System;

namespace TrainsEditor.CommonLogic
{
    /// <summary>
    /// Známé integrované systémy
    /// </summary>
    [Flags]
    enum IntegratedSystemsEnum
    {
        None = 0,

        PID = 1,

        ODIS = 2,
    }
}
