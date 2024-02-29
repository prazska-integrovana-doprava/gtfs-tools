using System;
using GtfsModel.Enumerations;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Označení místa, kde se nastupuje (na nástupišti)
    /// </summary>
    public class BoardingArea : BaseStop
    {
        /// <summary>
        /// Název (označení)
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Zastávka (nástupiště), kterou boarding area reprezentuje
        /// </summary>
        public Stop ParentPlatform { get; set; }

        public override LocationType LocationType => LocationType.BoardingArea;

        public override GtfsStop ToGtfsStop()
        {
            throw new NotImplementedException();
        }
    }
}
