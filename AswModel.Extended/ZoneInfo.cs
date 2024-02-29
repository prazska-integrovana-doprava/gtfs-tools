namespace AswModel.Extended
{
    /// <summary>
    /// Tarifní informace k zastávce (tarifní systém + označení tarifního pásma/zóny v rámci systému)
    /// </summary>
    public class ZoneInfo
    {
        public const string PIDSystemName = "PID";

        /// <summary>
        /// Zkratka názvu tarifního systému (např. "PID")
        /// </summary>
        public string TariffSystemShortName { get; set; }

        /// <summary>
        /// Označení pásma/zóny
        /// </summary>
        public string ZoneId { get; set; }

        public override string ToString()
        {
            return $"{TariffSystemShortName} {ZoneId}";
        }
    }
}
