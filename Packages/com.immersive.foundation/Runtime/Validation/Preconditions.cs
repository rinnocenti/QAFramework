using System;

namespace Immersive.Foundation.Validation
{
    public static class Preconditions
    {
        public static T NotNull<T>(T value, string parameterName) where T : class
        {
            return value ?? throw new ArgumentNullException(parameterName);
        }

        public static string NotNullOrWhiteSpace(string value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be empty or whitespace.", parameterName) : value;

        }

        public static void Check(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        public static void CheckArgument(bool condition, string parameterName, string message)
        {
            if (!condition)
            {
                throw new ArgumentException(message, parameterName);
            }
        }

        public static int CheckInRange(int value, int minInclusive, int maxInclusive, string parameterName)
        {
            if (value < minInclusive || value > maxInclusive)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, $"Value must be between {minInclusive} and {maxInclusive}.");
            }

            return value;
        }

        public static int CheckNotNegative(int value, string parameterName)
        {
            return value < 0 ? throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.") : value;

        }
    }
}
