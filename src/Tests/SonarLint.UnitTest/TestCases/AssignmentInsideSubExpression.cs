﻿using System;
using System.Collections.Generic;

namespace Tests.Diagnostics
{
    public class AssignmentInsideSubExpression
    {
        void foo(int a)
        {
        }

        void foo(bool a)
        {
        }

        int foo(Func<int, int> f)
        {
            throw new Exception();
        }

        private class MyClass
        {
            public int MyField;
        }

        void Foo()
        {
            int i = 0;

            foo(i = 42); // Noncompliant
            foo(i += 42); // Noncompliant
            foo(i -= 42); // Noncompliant
            foo(i *= 42); // Noncompliant
            foo(i /= 42); // Noncompliant
            foo(i %= 42); // Noncompliant
            foo(i &= 1); // Noncompliant
            foo(i ^= 1); // Noncompliant
            foo(i |= 1); // Noncompliant
            foo(i <<= 1); // Noncompliant
            foo(i >>= 1); // Noncompliant

            i = 42;
            foo(i == 42);

            foo(
                (int x) =>
                {
                    int a;
                    a = 42;
                    return a;
                });

            var b = true;

            if (b = false) { } // Noncompliant
            if ((b = false)) { } // Noncompliant
            for (int j = 0; b &= false; j++) { } // Noncompliant
            for (int j = 0; b == false; j++) { }

            while (b &= false) { } // Noncompliant

            while ((i = 1) == 1) { } // Compliant

            if (i == 0) i = 2;
            if ((i = 1) <= 1) // Compliant
            {
            }
            b = (i = 1) <= 1; // Noncompliant

            var y = (b &= false) ? i : i * 2; // Noncompliant

            string result = "";
            if (!string.IsNullOrEmpty(result)) result = result + " ";
            var v1 = new Action(delegate { });
            var v2 = new Action(delegate { var foo = 42; });
            var v3 = new Func<object, object>((x) => x = 42);
            var v4 = new Action<object>((x) => { x = 42; });
            var v5 = new { MyField = 42 };
            var v6 = new MyClass { MyField = 42 };
            var v7 = new MyClass() { MyField = 42 };
            var v8 = foo(x => { x = 42; return 0; });

            var index = 0;
            new int[] { 0, 1, 2 }[index = 2] = 10; // Noncompliant
            new int[] { 0, 1, 2 }[(index = 2)] = 10; // Noncompliant

            var o = new object();
            var oo = new object();

            if (false && (oo = o) != null) // Compliant
            { }

            oo = (o) ?? (o = new object()); // Compliant
            oo = (o) ?? (object)(o = new object()); // Compliant

            oo = oo ?? (o = new object()); // Noncompliant

            if ((a = b = 0) != 0) { }  // Noncompliant
            int x = (a = b) + 5; // Noncompliant
        }
        public void TestMethod1()
        {
            var j = 5;
            var k = 5;
            var i = j = // Noncompliant
                k = 10; // Noncompliant
            i = j =     // Noncompliant
                k = 10; // Noncompliant
        }
    }
}