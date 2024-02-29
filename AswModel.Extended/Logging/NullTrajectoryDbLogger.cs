using CommonLibrary;

namespace AswModel.Extended.Logging
{
    class NullTrajectoryDbLogger : ITrajectoryDbLogger
    {
        public void LogDuplicate(ShapeFragmentDescriptor descriptor, ServiceDaysBitmap serviceAsBits)
        {
        }

        public void LogMissing(ShapeFragmentDescriptor descriptor, Trip referingTrip)
        {
        }

        public void LogPartiallyMissing(ShapeFragmentDescriptor descriptor, Trip referingTrip)
        {
        }

        public void Close()
        {
        }
    }
}
