# MPIDotNet

*Disclaimer*: I work for Microsoft, and I work on [ML.NET](https://github.com/dotnet/machinelearning), the machine learning library for .NET, but this project is neither supported nor promoted by Microsoft or the .NET Foundation. This is simply something that I found useful that I wanted to share.

A friend of mine recently asked me it if was possible to write MPI programs in .NET on Linux. I wrote this project to show that it **is** possible and quite easy. In fact, you can expect great performance from .NET with MPI.

While this repository is focused on MPI on Linux, it would be very easy to also get it to support Windows and Mac runtimes. You would simply need to build the C++ libraries on all three platforms, and reference them appropriately (perhaps with a runtime check) in .NET. See [this repository](https://github.com/rogancarr/DotNetCppExample) for an example of how to build cross-platform .NET code that relies on C++ libraries (this repository actually uses that repository as a base). In fact, I left the C++ project in a combined Makefile + VS state so that it would be easy to extend.

## Project Overview

This project consists of two parts:
1. A C++ library that wraps `mpi.h`
2. A C# library that uses the C++ wrapper to call MPI methods.

Let's look at these:

The C++ wrapper is necessary for two main reasons:
* We want an MPI wrapper that will work with the different flavors of MPI, be it OpenMPI, MPICH(2), Intel MPI, etc.
  
  By wrapping `mpi.h`, we can compile to any standard MPI flavor.
* Some of the MPI APIs are not compatible with .NET.

  See, for example, [AllReduce](https://www.open-mpi.org/doc/v4.0/man3/MPI_Allreduce.3.php), which has a signature like
  ```c++
  MPI_Allreduce(const void *sendbuf, void *recvbuf, int count, MPI_Datatype datatype, MPI_Op op, MPI_Comm comm)
  ```
  Typing an input as `void*` from .NET seems like a no-go.
  
Once we have our C library, we can then wrap it in C# and then use it from .NET.
  
## Requirements

- An MPI library: Follow the install directions of your MPI flavor. This has been tested with OpenMPI on Ubuntu 16.04 and 18.04.
- The .NET Core SDK: [Installation and documentation link](https://dotnet.microsoft.com/download)
  
## Building

### Building the C++ Library

To compile the library, you just need to `make all`. This assumes you have an MPI development library like OpenMPI already installed. From the project rood, simply do the following:

```bash
cd MpiLibrary/MpiLibrary/
make all
```

#### Test it out

First, add the path where `MpiLibrary.so` lives to your `LD_LIBRARY_PATH` like so:
```bash
export LD_LIBRARY_PATH=${LD_LIBRARY_PATH}:$(pwd)/x64/linux/
```

Next, run the executable
```bash
mpirun -np 4 x64/linux/main
```

Expected output:
```
1, 4
1
3
2, 4
1
3
3, 4
1
3
Sum 6
0, 4
1
3
Sum 6
Sum 6
Sum 6
```
