#include <iostream>
#include "MpiLibrary.h"

//using namespace std;

int main(int argc, char *argv[])
{
	initialize_mpi(argc, argv);
	int world_size = get_world_size();
	int world_rank = get_world_rank();
	std::cout << world_rank << ", " << world_size << std::endl;
	
	std::cout << get_one() << std::endl;
	std::cout << add_two_numbers(1,2) << std::endl;

	int sum;
	all_reduce_int(world_rank, sum);
	std::cout << "Sum " << sum << std::endl;

	finalize_mpi();
}

