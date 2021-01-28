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

    [AutoEquality]
    partial class C
    {
        int Field1;
        string Field2;
        Exception Field3;
    }

    partial class D : INotifyPropertyChanged
    {
        [AutoNotify.AutoNotify]
        public int _value;

    }

}
