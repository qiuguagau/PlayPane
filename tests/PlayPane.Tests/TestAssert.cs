using System;

namespace PlayPane.Tests
{
    internal static class TestAssert
    {
        public static void True(bool value, string message)
        {
            if (!value)
            {
                throw new InvalidOperationException(message);
            }
        }

        public static void False(bool value, string message)
        {
            True(!value, message);
        }

        public static void Equal<T>(T expected, T actual, string message)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(message + " Expected: " + expected + ", Actual: " + actual);
            }
        }

        public static void Near(double expected, double actual, double tolerance, string message)
        {
            if (Math.Abs(expected - actual) > tolerance)
            {
                throw new InvalidOperationException(message + " Expected: " + expected + ", Actual: " + actual);
            }
        }
    }
}
