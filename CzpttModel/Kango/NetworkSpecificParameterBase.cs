using System.Collections.Generic;
using System.Linq;

namespace CzpttModel.Kango
{
    /// <summary>
    /// Nadstavba pro <see cref="CZPTTCISMessage.NetworkSpecificParameter"/>, které obsahují <see cref="NameAndValuePair.Value"/> hodnotu rozdělenou pomocí
    /// svislítek '|'. Jde o bázovou třídu pro konkrétní instance v namespace <see cref="CzpttModel.Kango"/>.
    /// 
    /// Je postaveno nad instancí <see cref="CzpttModel.NameAndValuePair"/>, přímo z ní čte a rovnou do ní zapisuje případné změny.
    /// </summary>
    public abstract class NetworkSpecificParameterBase
    {
        /// <summary>
        /// Dvojice jméno-název tak jak je zapsána v XML souboru (odkaz)
        /// </summary>
        public NameAndValuePair NameAndValuePair { get; set; }

        private List<string> _valueSplit;

        protected NetworkSpecificParameterBase(NameAndValuePair nameAndValuePair)
        {
            NameAndValuePair = nameAndValuePair;
            _valueSplit = NameAndValuePair.Value.Split('|').ToList();
        }
        
        protected string Get(int index)
        {
            if (index >= _valueSplit.Count)
            {
                return "";
            }

            return _valueSplit[index];
        }

        protected int GetAsInt(int index)
        {
            return int.Parse(Get(index));
        }

        protected int GetAsIntOrDefault(int index, int defaultValueIfEmpty = 0)
        {
            if (string.IsNullOrWhiteSpace(Get(index)))
            {
                return defaultValueIfEmpty;
            }
            else
            {
                return GetAsInt(index);
            }
        }

        protected void Set(int index, string value)
        {
            while (_valueSplit.Count <= index)
            {
                _valueSplit.Add("");
            }

            _valueSplit[index] = value;
            NameAndValuePair.Value = string.Join("|", _valueSplit);
        }

        protected void Set(int index, int value, int? defaultValueNotToBeStored = null)
        {
            if (defaultValueNotToBeStored.HasValue && value == defaultValueNotToBeStored.Value)
            {
                Set(index, "");
            }
            else
            {
                Set(index, value.ToString());
            }
        }
    }
}
