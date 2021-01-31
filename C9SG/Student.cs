using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace C9SG
{
    public partial class Student
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public Student(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }
    }
}
