using System;

namespace Generators
{
    internal sealed class IndentUtil
    {
        public class Marker : IDisposable
        {
            private readonly IndentUtil _util;
            private int _count;

            public Marker(IndentUtil indentUtil, int count)
            {
                _util = indentUtil;
                _count = count;
            }

            public void Revert()
            {
                Dispose();
                _count = 0;
            }

            public void Dispose() => _util.Decrease(_count);
        }

        public int Depth { get; private set; }
        public string UnitValue { get; } = new string(' ', 4);
        public string Value { get; private set; } = "";
        public string Value2 { get; private set; } = "";
        public string Value3 { get; private set; } = "";
        public string Value4 { get; private set; } = "";
        public string Value5 { get; private set; } = "";
        public string Value6 { get; private set; } = "";

        public IndentUtil() => Update();

        public Marker Increase(int count = 1)
        {
            IncreaseSimple(count);
            return new Marker(this, count);
        }

        public void IncreaseSimple(int count = 1)
        {
            Depth += count;
            Update();
        }

        public void Decrease(int count = 1)
        {
            Depth -= count;
            Update();
        }

        private void Update()
        {
            Value = new string(' ', Depth * 4);
            Value2 = new string(' ', (Depth + 1) * 4);
            Value3 = new string(' ', (Depth + 2) * 4);
            Value4 = new string(' ', (Depth + 3) * 4);
            Value5 = new string(' ', (Depth + 4) * 4);
            Value6 = new string(' ', (Depth + 5) * 4);
        }
    }
}
