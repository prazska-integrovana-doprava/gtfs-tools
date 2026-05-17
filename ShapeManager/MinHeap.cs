using System.Collections.Generic;

namespace ShapeManager
{
    public class MinHeap<T>
    {
        private List<(T Item, double Priority)> _data = new List<(T Item, double Priority)>();

        public int Count => _data.Count;

        public void Enqueue(T item, double priority)
        {
            _data.Add((item, priority));
            HeapifyUp(_data.Count - 1);
        }

        public T Dequeue()
        {
            var root = _data[0].Item;

            _data[0] = _data[_data.Count - 1];
            _data.RemoveAt(_data.Count - 1);

            if (_data.Count > 0)
                HeapifyDown(0);

            return root;
        }

        public bool Any() => _data.Count > 0;

        private void HeapifyUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) / 2;

                if (_data[parent].Priority <= _data[i].Priority)
                    break;

                (_data[parent], _data[i]) = (_data[i], _data[parent]);
                i = parent;
            }
        }

        private void HeapifyDown(int i)
        {
            while (true)
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                int smallest = i;

                if (left < _data.Count && _data[left].Priority < _data[smallest].Priority)
                    smallest = left;

                if (right < _data.Count && _data[right].Priority < _data[smallest].Priority)
                    smallest = right;

                if (smallest == i)
                    break;

                (_data[i], _data[smallest]) = (_data[smallest], _data[i]);
                i = smallest;
            }
        }
    }
}
