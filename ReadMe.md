# MPIDotNet: A demo of MPI with .NET Core on Linux

*Disclaimer*: I work for Microsoft, and I work on [ML.NET](https://github.com/dotnet/machinelearning), the machine learning library for .NET, but this project is neither supported nor promoted by Microsoft or the .NET Foundation. This is simply something that I found useful that I wanted to share.

A friend of mine recently asked me it if was possible to write MPI programs in .NET on Linux. I wrote this project in an afternoon to show that it **is** possible and quite easy. In fact, you can expect great performance from .NET with MPI.

This isn't a complete wrapping of MPI in .NET. Only a handful of functions have been wrapped so far. That said, it's enough to show that any function could be wrapped, and the rest of the API surface could most likely be scripted.

While this repository is focused on MPI on Linux, it would be very easy to also get it to support Windows and Mac runtimes. You would simply need to build the C++ libraries on all three platforms, and reference them appropriately (perhaps with a runtime check) in .NET. See [this repository](https://github.com/rogancarr/DotNetCppExample) for an example of how to build cross-platform .NET code that relies on C++ libraries (this repository actually uses that repository as a base). In fact, I left the C++ project in a combined Makefile + VS state so that it would be easy to extend.

## Project Overview

This project consists of two parts:
1. A C++ library that wraps functions from `mpi.h`
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
  Typing an input as `void*` from .NET seems like a no-go. Instead, we write explicit methods for each possible `Type` we want to pass over the wire.
  
The C# library is both a wrapper for the C++ library, and a demo program showing how to use the MPI bindings. The library implements the interface for MPI, with some helpful tricks and a workaround for `DllImport` in .NET Core. The example program shows off what we can now do. It does some simple counting with MPI and finishes with some machine learning: Each node builds a linear model over a resampled dataset and then uses `Allreduce` to ensemble the models. This is a simple example, but you can imagine all the amazing directions you can go from here.
  
## Requirements

- An MPI library: Follow the install directions of your MPI flavor. This project has been tested with OpenMPI on stand-alone Ubuntu 16.04 and 18.04 VMs, and on an Ubuntu16.04 Azure Batch AI cluster. It has also been tested on Ubuntu 16.04 running in the [Windows Subsystem for Linux](https://docs.microsoft.com/en-us/windows/wsl/install-win10). (Did you know about that? It's awesome.)
- The .NET Core SDK: [Installation and documentation link](https://dotnet.microsoft.com/download)
- A c++ compiler, like gcc.
- dos2unix: The code has been written in Windows and in Linux, so the line-endings aren't necessarily unix-style. Before compiling the C++ code, we'll need to run dos2unix just in case.
  
## Building

### Building the C++ Library

To compile the library, you just need to `make all`. This assumes you have an MPI development library like OpenMPI already installed. From the project root, simply do the following:

```bash
cd MpiLibrary/MpiLibrary/
make all
```

In addition to the library `make all` also creates a helper program called `main` that you can use to test that the library works.

#### Test it out

First, add the path where `libMpiLibrary.so` lives to your `LD_LIBRARY_PATH` like so:
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

## Building

### Building the .NET (C#) Library

To compile the .NET (C#) library, we simply use the `dotnet` application.

From the project root, simply do the following:

```bash
cd MpiDotNetApp/MpiDotNetApp/
dotnet build -c Release -r ubuntu.16.04-x64 # Put your target version here 
```

This will build a `dll` in the `bin/Release/netcoreapp2.1/ubuntu.16.04-x64` directory. If you targeted a different system, then replace the `ubuntu...` bit with your target version.

Note that at this time, I cannot get a stand-alone .NET executable to interop with MPI correctly (i.e. with `dotnet publish ...`). I think it's related to the `DllImport`/`dlopen` issue discussed in the C# code. So for now, we build a runtime-dependent application (i.e. that you run with `dotnet myProgram` instead of `./myProgram`).

#### Test it out

This sample script uses the "UCI housing" dataset. You can grab a copy [here](https://raw.githubusercontent.com/dotnet/machinelearning/024bd4452e1d3660214c757237a19d6123f951ca/test/data/housing.txt).

Assuming that you put `housing.txt` into the root directory of the project, you can run the program from the root directory by doing the following:

```bash
export LD_LIBRARY_PATH=${LD_LIBRARY_PATH}:$(pwd)/MpiLibrary/MpiLibrary/x64/linux/
mpirun -np 4 dotnet MpiDotNetApp/MpiDotNetApp/bin/Release/netcoreapp2.1/ubuntu.16.04-x64/MpiDotNetApp.dll housing.txt
```

The first line sets the library path as to where to find `libMpiLibrary.so`, and the second runs our dotnet program using MPI.

The output should be as follows:
```bash
Starting up an MPI program!
Starting up an MPI program!
Starting up an MPI program!
Starting up an MPI program!
Rank: 1, Size: 4
Rank: 3, Size: 4
Rank: 0, Size: 4
AllReduce Rank-Sum: 6
Training a linear model....
Rank: 2, Size: 4
AllReduce Rank-Sum: 6
Training a linear model....
AllReduce Rank-Sum: 6
Training a linear model....
AllReduce Rank-Sum: 6
Training a linear model....
Rank-3: bias=0.02340505 weight[0]=-0.07871736 | bias=0.02261201 weight[0]=-0.07457273
Rank-2: bias=0.02257155 weight[0]=-0.07575243 | bias=0.02261201 weight[0]=-0.07457273
Rank-1: bias=0.02159619 weight[0]=-0.0743866 | bias=0.02261201 weight[0]=-0.07457273
Rank-0: bias=0.02287525 weight[0]=-0.06943453 | bias=0.02261201 weight[0]=-0.07457273
```
