using System;

namespace CsvSerializer.Attributes
{
    /// <summary>
    /// Udává operaci, která by měla být provedena nad daty v daném sloupečku
    /// </summary>
    public enum CsvFieldPostProcess
    {
        /// <summary>
        /// Žádná operace
        /// </summary>
        None,

        /// <summary>
        /// Přidat uvozovky
        /// </summary>
        Quote
    }

    /// <summary>
    /// Označuje member field, který se má ukládat do CSV
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class CsvFieldAttribute : Attribute
    {
        /// <summary>
        /// Název sloupečku v CSV
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Pořadí, podle kterého mají být sloupečky do souboru vypisovány.
        /// Není nutné, aby pořadí všech fieldů tvořilo souvislou řadu, nicméně hodnota Order musí být unikátní.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Operace, která se má provést nad hodnotami v daném sloupečku
        /// </summary>
        public CsvFieldPostProcess PostProcess { get; set; }

        /// <summary>
        /// Pokud je hodnota uložená v políčku rovna této, uloží se prázdný řetězec. Naopak při načítání se prázdný řetězec interpretuje jako tato hodnota
        /// </summary>
        public object DefaultValue { get; set; }

        public CsvFieldAttribute(string name, int order, CsvFieldPostProcess postProcess = CsvFieldPostProcess.None, object defaultValue = null)
        {
            Name = name;
            Order = order;
            PostProcess = postProcess;
            DefaultValue = defaultValue;
        }
    }
}
