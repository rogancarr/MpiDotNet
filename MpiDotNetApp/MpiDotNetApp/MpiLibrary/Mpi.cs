using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MpiLibrary
{
    // Inherit from an abstract class
    public sealed class Mpi : IDisposable
    {
        // The name of the MPI Library to load
        private const string MPI_LIBRARY = "libMpiLibrary.so";

        // The command-line invocation of dotnet
        private const string DOTNET_COMMAND = "dotnet";

        // There is a bug in .NET Core, where if you DLLImport a library that uses dlopen, the imported
        // library won't be able to get the correct version of the underlying library. (See https://github.com/dotnet/coreclr/issues/18599)
        // The workaround we use (from that issue) is to import dl into .NET, and dlopen any required libraries
        // into the .NET runtime here.
        private const int RTLD_LAZY = 0x00001; //Only resolve symbols as needed
        private const int RTLD_GLOBAL = 0x00100; //Make symbols available to libraries loaded later
        [DllImport("dl")]
        private static extern IntPtr dlopen(string file, int mode);

        // Native implementation of initialize_mpi() for Linux
        [DllImport(MPI_LIBRARY)]
        private static extern int initialize_mpi(int argc, string[] argv);

        // Native implementation of finalize_mpi() for Linux
        [DllImport(MPI_LIBRARY)]
        private static extern int finalize_mpi();

        // Native implementation of get_world_size() for Linux
        [DllImport(MPI_LIBRARY)]
        private static extern int get_world_size();

        // Native implementation of get_world_rank() for Linux
        [DllImport(MPI_LIBRARY)]
        private static extern int get_world_rank();

        // Native implementation of AllReduce(int, int, ...) for Linux
        [DllImport(MPI_LIBRARY)]
        private static extern int all_reduce_int(int i, ref int j);

        // Native implementation of AllReduce(float[], float[], ...) for Linux
        [DllImport(MPI_LIBRARY)]
        private static extern int all_reduce_floatarray(float[] source, float[] dest, int length);

        // Native implementation of GetOne() for Linux
        [DllImport(MPI_LIBRARY)]
        private static extern int get_one();

        // Native implementation of AddTwoNumbers() for Linux
        [DllImport(MPI_LIBRARY)]
        private static extern int add_two_numbers(int i, int j);

        // Native implementation of ExternalCall() for Linux
        [DllImport(MPI_LIBRARY)]
        private static extern int external_call(Functions.AddTwoIntsDelegate func, int i, int j);

        // Native implementation of ExternalReduce() for Linux
        [DllImport(MPI_LIBRARY)]
        private static extern int external_reduce(Functions.ReduceIntArrayDelegate reduceFunc, int[] array, int arraySize);

        // Native implementation of ExternalReduceWithCallback() for Linux
        [DllImport(MPI_LIBRARY)]
        private static extern int external_reduce_with_callback(
            Functions.ReduceNativeIntArrayWithFuncDelegate reduceFunc, int[] array, int arraySize);

        /// <summary>
        /// Initialize the MPI object.
        /// </summary>
        /// <param name="args">The arguments array passed to the program.</param>
        /// <returns>The result of MPI_Init().</returns>
        public Mpi(string[] args)
        {
            // Pre-load dependencies of the wrapping module; necessary to grab the right _version_
            // libmpi.so.12 is for OpenMPI on Ubuntu16.04; libmpi.so.20 for Ubuntu18.04.
            dlopen("libmpi.so.12", RTLD_LAZY | RTLD_GLOBAL);
            InitializeMpi(args);
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        /// <remarks>
        /// Necessary so that we can open and close the MPI connection with a using statement.
        /// </remarks>
        public void Dispose()
        {
            FinalizeMpi();
        }

        private int InitializeMpi(string[] args)
        {
            // Here we recreate the args that a C program would give to MPI; probably not necessary.
            var cStyleArgs = args.ToList();
            cStyleArgs.Insert(0, DOTNET_COMMAND);
            var fileName = Environment.GetCommandLineArgs()[0];
            cStyleArgs.Insert(1, fileName);
            return initialize_mpi(cStyleArgs.Count, cStyleArgs.ToArray());
        }

        /// <summary>
        /// Finalize MPI.
        /// </summary>
        /// <returns>The result of MPI_Finalize().</returns>
        private int FinalizeMpi()
        {
            return finalize_mpi();
        }

        /// <summary>
        /// Get the MPI world size.
        /// </summary>
        /// <returns>The world size.</returns>
        public int GetWorldSize()
        {
            return get_world_size();
        }

        /// <summary>
        /// Get the MPI world rank.
        /// </summary>
        /// <returns>The world rank.</returns>
        public int GetWorldRank()
        {
            return get_world_rank();
        }

        /// <summary>
        /// AllReduce an integer to an integer.
        /// </summary>
        /// <param name="i">The value in the local process.</param>
        /// <param name="j">The sum over all the workers.</param>
        /// <returns>The result of the MPI operation.</returns>
        public int AllReduce(int i, ref int j)
        {
            return all_reduce_int(i, ref j);
        }

        /// <summary>
        /// AllReduce a flaot array to a float array.
        /// </summary>
        /// <param name="i">The value in the local process.</param>
        /// <param name="j">The sum over all the workers.</param>
        /// <returns>The result of the MPI operation.</returns>
        public int AllReduce(float[] source, float[] dest)
        {
            if (source == null || dest == null)
                throw new ArgumentException("Arrays must not be null.");
            if (source.Length != dest.Length)
                throw new ArgumentException("Arrays not of equal length.");

            return all_reduce_floatarray(source, dest, source.Length);
        }

        /// <summary>
        /// Return the number 1
        /// </summary>
        /// <returns>The number 1</returns>
        public int GetOne()
        {
            return get_one();
        }

        /// <summary>
        /// Sum two numbers
        /// </summary>
        /// <param name="i">An integer</param>
        /// <param name="j">An integer</param>
        /// <returns>The sum of <paramref name="i"/> and <paramref name="j"/></returns>
        public int AddTwoNumbers(int i, int j)
        {
            return add_two_numbers(i, j);
        }

        /// <summary>
        /// Evaluate a function with integer inputs
        /// </summary>
        /// <param name="func">A delegate to a function taking two ints as input and returning an int</param>
        /// <param name="i">An integer</param>
        /// <param name="j">An integer</param>
        /// <returns>The result of the function</returns>
        public int ExternalCall(Functions.AddTwoIntsDelegate func, int i, int j)
        {
            return external_call(func, i, j);
        }

        /// <summary>
        /// Evaluate a reduce function
        /// </summary>
        /// <param name="reduceFunc">A delegate to a reduce function taking an integer array</param>
        /// <param name="array">The integer array</param>
        /// <param name="arraySize">The size of the integer array</param>
        /// <returns>The result of the reduce function</returns>
        public int ExternalReduce(Functions.ReduceIntArrayDelegate reduceFunc, int[] array, int arraySize)
        {
            return external_reduce(reduceFunc, array, arraySize);
        }

        /// <summary>
        /// Evaluate a reduce function taking a callback to a custom reducer
        /// </summary>
        /// <param name="reduceFunc">A delegate to a reducer function taking an integer array, the integer array size, and a pointer to a reducing function over the integer array</param>
        /// <param name="array">The integer array</param>
        /// <param name="arraySize">The size of the integer array</param>
        public int ExternalReduceWithCallback(Functions.ReduceNativeIntArrayWithFuncDelegate reduceFunc, int[] array, int arraySize)
        {
            return external_reduce_with_callback(reduceFunc, array, arraySize);
        }
    }
}
