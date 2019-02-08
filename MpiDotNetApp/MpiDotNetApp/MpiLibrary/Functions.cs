using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MpiLibrary
{
    /// <summary>
    /// A library of static functions for interop with a C library
    /// </summary>
    public static class Functions
    {

        // Define a delegate that corresponds to the managed function.
        public delegate int AddTwoIntsDelegate(int i, int j);

        /// <summary>
        /// Sum two ints
        /// </summary>
        /// <param name="i">an integer</param>
        /// <param name="j">an integer</param>
        /// <returns>The sum of i and j</returns>
        public static int AddTwoInts(int i, int j)
        {
            return i + j;
        }

        // Delegate for a sum reducer of an int array
        public delegate int ReduceIntArrayDelegate(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]int[] array, int arraySize);

        /// <summary>
        /// Reduce an int array by summation
        /// </summary>
        /// <param name="array">An integer C array</param>
        /// <param name="arraySize">The size of the C array</param>
        /// <returns>The sum of the integers in the array</returns>
        public static int ReduceIntArray(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] array, int arraySize)
        {
            return array.Sum();
        }

        // A delegate to a function that reduces an int array using a pointer to a callback function
        public delegate int ReduceNativeIntArrayWithFuncDelegate(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]int[] array, int arraySize,
            IntPtr functionPointer);

        /// <summary>
        /// Reduces an int array using a custom reduce function
        /// </summary>
        /// <param name="array">An integer C array</param>
        /// <param name="arraySize">The size of the C array</param>
        /// <param name="functionPointer">A pointer to a reducer function that reduces the input array to an int</param>
        /// <returns>The integer result of the reducer function</returns>
        public static int ReduceNativeIntArrayWithNativeFunc(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] array, int arraySize,
            IntPtr functionPointer)
        {
            var function = Marshal.GetDelegateForFunctionPointer<ReduceIntArrayDelegate>(functionPointer);

            return function(array, arraySize);
        }
    }
}
