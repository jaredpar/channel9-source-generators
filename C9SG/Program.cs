using System;
using System.ComponentModel;

namespace C9SG
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    partial class E
    {
        public override bool Equals(object? obj)
        {
            return true;
        }
    }

    [AutoEquality]
    partial class C
    {
        int Field1;
        string Field2;
        Exception Field3;
    }

    partial class D
    {
        [AutoNotify]
        public int _value;

    }

}
