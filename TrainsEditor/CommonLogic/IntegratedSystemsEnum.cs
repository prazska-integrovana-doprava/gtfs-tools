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

        IDSJMK = 4,

        IDZK = 8,

        IDESKA = 16,

        IDPK = 32,
    }
}
