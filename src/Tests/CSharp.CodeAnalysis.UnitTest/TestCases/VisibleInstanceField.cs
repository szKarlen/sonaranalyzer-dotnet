﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.TestCases
{
    public class VisibleInstanceField
    {
        public static double Pi = 3.14;  
        public const double Pi2 = 3.14;  
        public readonly double Pi2 = 3.14; // Noncompliant
        public double Pi3 = 3.14; // Noncompliant
        private double Pi4 = 3.14; 
    }
}
