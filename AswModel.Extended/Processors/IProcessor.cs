namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Zpracovává konkrétní XML tagy
    /// </summary>
    interface IProcessor<T>
    {
        void Process(T item);
    }
}
