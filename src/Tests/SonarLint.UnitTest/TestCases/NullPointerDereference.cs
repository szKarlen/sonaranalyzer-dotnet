﻿using System;
using System.Collections.Generic;

namespace Tests.Diagnostics
{
    class NullPointerDereference
    {
        void Test_1(bool condition)
        {
            object o = null;
            if (condition)
            {
                M1(o.ToString()); // Noncompliant {{"o" is null.}}
//                 ^
            }
            else
            {
                o = new object();
            }
            M2(o.ToString()); // Compliant
        }

        void Test_2(bool condition)
        {
            object o = new object();
            if (condition)
            {
                o = null;
            }
            else
            {
                o = new object();
            }
            M2(o.ToString()); // Non-compliant, can be null, but we are not reporting on it.
        }

        void Test_ExtensionMethodWithNull()
        {
            object o = null;
            o.MyExtension(); // Compliant
        }

        void Test_Out()
        {
            object o1;
            object o2;
            if (OutP(out o1) &&
                OutP(out o2) &&
                o2.Count != 0)
            {
            }
        }
        void OutP(out object o) { o = new object(); }

        void Test_Struct()
        {
            int? i = null;
            if(i.HasValue) // Compliant
            { }
        }

        void Test_Foreach()
        {
            IEnumerable<int> en = null;
            foreach (var item in en) // Noncompliant
            {

            }
        }

        async System.Threading.Tasks.Task Test_Await()
        {
            System.Threading.Tasks.Task t = null;
            await t; // Noncompliant
        }

        void Test_Exception()
        {
            Exception exc = null;
            throw exc; // Noncompliant
        }

        void Test_Exception_Ok()
        {
            Exception exc = new Exception();
            throw exc;
        }

        public NullPointerDereference()
        {
            object o = null;
            Console.WriteLine(o.ToString()); // Noncompliant

            var a = new Action(() =>
            {
                object o1 = null;
                Console.WriteLine(o1.ToString()); // Noncompliant
            });
        }

        public int MyProperty
        {
            get
            {
                object o1 = null;
                Console.WriteLine(o1.ToString()); // Noncompliant
                return 42;
            }
        }

        object myObject = null;

        void Test_ConditionEqualsNull(bool condition)
        {
            object o = myObject; // can be null
            if (o == null)
            {
                M1(o.ToString()); // Noncompliant, always null
            }
            else
            {
                o = new object();
            }
            M2(o.ToString()); // Compliant
        }

        void Test_ConditionNotEqualsNull(bool condition)
        {
            object o = myObject; // can be null
            if (null != o)
            {
                M1(o.ToString()); // Compliant
            }
            else
            {
                o = new object();
            }
            M2(o.ToString()); // Compliant
        }

        void Test_Foreach_Item(bool condition)
        {
            foreach (var item in new object[0])
            {
                if (item == null)
                {
                    Console.WriteLine(item.ToString()); // Noncompliant
                }
            }
        }

        void Test_Complex(bool condition)
        {
            var item = new object();
            if (item != null && item.ToString() == "")
            {
                Console.WriteLine(item.ToString());
            }
        }
    }

    static class Extensions
    {
        public static void MyExtension(this object o) { }
    }
}