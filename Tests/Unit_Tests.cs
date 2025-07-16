using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator.Tests
{
    internal class Unit_Tests
    {
        public static void AssertEquals(byte expected, byte actual, string message)
        {
            if (expected != actual) {
                Console.WriteLine($"[FAIL] {message}: Expected {expected}, Got Actual {actual}");
            }
            else {
                Console.WriteLine($"[PASS] {message}");
            }
        }

        public static void AssertEquals(ushort expected, ushort actual, string message)
        {
            if (expected != actual) {
                Console.WriteLine($"[FAIL] {message}: Expected {expected}, Got Actual {actual}");
            }
            else {
                Console.WriteLine($"[PASS] {message}");
            }
        }
        public static void AssertEquals(bool expected, bool actual, string message)
        {
            if (expected != actual)
            {
                Console.WriteLine($"[FAIL] {message}: Expected {expected}, Got Actual {actual}");
            }
            else
            {
                Console.WriteLine($"[PASS] {message}");
            }
        }
    }
}
