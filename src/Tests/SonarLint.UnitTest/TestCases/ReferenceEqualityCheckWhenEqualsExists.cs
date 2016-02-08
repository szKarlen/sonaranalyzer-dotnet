﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Tests.Diagnostics
{
    interface IMyInterface { }
    interface IMyInterfaceWithoutImplementations { }

    class Base
    {
        public override bool Equals(object obj)
        {
            return new Base() == obj; // Compliant, we are inside teh Equals.
        }
    }

    class MyClass : Base, IMyInterface
    {

    }

    internal class MyClass2 : IMyInterface
    {
        public static bool operator ==(MyClass2 a, MyClass2 b)
        {
            return false;
        }
        public static bool operator !=(MyClass2 a, MyClass2 b)
        {
            return false;
        }
    }

    class ReferenceEqualityCheckWhenEqualsExists
    {
        static void Main(IMyInterface x, IMyInterface y)
        {
            var b = x == y; // Noncompliant
            b = x != y; // Noncompliant
            b = x != null;
            b = x == new object(); // Noncompliant
            b = new Base() == new object(); // Noncompliant
            b = new MyClass() == new object(); // Noncompliant
            b = new MyClass2() == new object(); // CS0253
            b = new object() == new object();

            // The following is compliant
            // mscorlib defines Type.operator==, but System.Runtime doesn't
            // we can't test it here though, because in the test we have the mscorlib's Type
            b = typeof(object) == typeof(object);
        }

        private static T1 CompareExchange<T1>(ref T1 reference, T1 expectedValue, T1 newValue) where T1 : class
        {
            var r = CompareExchange(ref reference, newValue, expectedValue) == expectedValue;
            return r ? newValue : null;
        }

        private static T2 CompareExchange2<T2>(ref T2 reference, T2 expectedValue, T2 newValue) where T2 : Base
        {
            var r = CompareExchange2(ref reference, newValue, expectedValue) == expectedValue; // Noncompliant
            return r ? newValue : null;
        }
    }
}