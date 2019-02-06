// MpiLibrary.cpp : Defines the exported functions for the DLL.
#include "stdafx.h"
#include "MpiLibrary.h"
#include <mpi.h>
#include <iostream>

// Initialize MPI
int initialize_mpi(int argc, char *argv[]) {
	return MPI_Init(&argc, &argv);
}

int finalize_mpi()
{
	return MPI_Finalize();
}

// Get the World Size
int get_world_size()
{
	int size;
	MPI_Comm_size(MPI_COMM_WORLD, &size);
	return size;
}

//Get the World Rank
int get_world_rank()
{
	int rank;
	MPI_Comm_rank(MPI_COMM_WORLD, &rank);
	return rank;
}

int all_reduce_int(int i, int& j)
{
	return MPI_Allreduce(&i, &j, 1, MPI_INT, MPI_SUM, MPI_COMM_WORLD);
}

int all_reduce_floatarray(float source[], float dest[], int length)
{
	return MPI_Allreduce(source, dest, length, MPI_FLOAT, MPI_SUM, MPI_COMM_WORLD);
}

// Return 1
int get_one()
{
	return 1;
}

// Add two numbers
int add_two_numbers(int i, int j)
{
	return i + j;
}

// Call a function pointer with two ints, and return the result
int external_call(TwoIntReduceDelegate addTwoNumbers, int i, int j)
{
	return addTwoNumbers(i, j);
}

// Call a reduce function pointer with an int array and return the result
int external_reduce(ReduceIntArrayDelegate reduceFunc, int i[], int i_size)
{
	return reduceFunc(i, i_size);
}

// Reduce an int array by summation
int reduce_int_array(int v[], int v_size)
{
	int sum = 0;
	for (int i = 0; i < v_size; i++)
	{
		sum += v[i];
	}
	return sum;
}

// Call a function pointer with an array of integers and a custom reducing function and return the results
int external_reduce_with_callback(ReduceIntArrayWithFuncDelegate reduceHostFunc, int i[], int i_size)
{
	return reduceHostFunc(i, i_size, *reduce_int_array);
}
