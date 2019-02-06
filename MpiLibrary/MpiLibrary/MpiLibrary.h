// MpiLibrary.h - Contains declarations of functions and delegates
#pragma once

#ifndef _WINDOWS
#define __declspec(dllexport)
#endif
#ifdef MpiLIBRARY_EXPORTS  
#define MpiLIBRARY_API __declspec(dllexport)   
#else  
#define MpiLIBRARY_API __declspec(dllimport)   
#endif  

// Initialize MPI
extern "C" MpiLIBRARY_API int initialize_mpi(int argc, char *argv[]);

extern "C" MpiLIBRARY_API int finalize_mpi();

// Get the World Size
extern "C" MpiLIBRARY_API int get_world_size();

// Get the World Rank
extern "C" MpiLIBRARY_API int get_world_rank();

extern "C" MpiLIBRARY_API int all_reduce_int(int i, int& j);

extern "C" MpiLIBRARY_API int all_reduce_floatarray(float source[], float dest[], int length);

// Get the number 1
extern "C" MpiLIBRARY_API int get_one();

// Add two numbers
extern "C" MpiLIBRARY_API int add_two_numbers(int i, int j);

// Call a function with two integers
typedef int(*TwoIntReduceDelegate)(int a, int b);
extern "C" MpiLIBRARY_API int external_call(TwoIntReduceDelegate addTwoNumbers, int i, int j);

// Call a function that reduces an array
typedef int(*ReduceIntArrayDelegate)(int v[], int v_size);
extern "C" MpiLIBRARY_API int external_reduce(ReduceIntArrayDelegate reduceFunc, int i[], int i_size);

// Call a function that reduces an array using a supplied C++ function
typedef int(*ReduceIntArrayWithFuncDelegate)(int v[], int v_size, int(*ReduceIntArray)(int v[], int v_size));
extern "C" MpiLIBRARY_API int external_reduce_with_callback(ReduceIntArrayWithFuncDelegate reduceHostFunc, int i[], int i_size);
