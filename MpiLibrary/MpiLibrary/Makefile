BUILD_DIR = x64/linux
CD_BUILD_DIR = cd ${BUILD_DIR}
Compiler = mpic++
#$(CXX)

main: MpiLibrary.so main.o
	${CD_BUILD_DIR}; $(Compiler) -o main  main.cpp -L. -lMpiLibrary

MpiLibrary.so: MpiLibrary.cpp
	${CD_BUILD_DIR}; $(Compiler) -fPIC -c MpiLibrary.cpp -o MpiLibrary.o; $(Compiler) -shared  -Wl,-soname,libMpiLibrary.so -o libMpiLibrary.so MpiLibrary.o

all: prep main clean_prep

prep:
	mkdir -p ${BUILD_DIR};
	@cp *.h *.cpp ${BUILD_DIR}
	${CD_BUILD_DIR}; dos2unix *.h *.cpp

clean_prep:
	${CD_BUILD_DIR} && rm -f *.cpp *.h;

clean:
	${CD_BUILD_DIR} && rm -f *.o *.so *.cpp *.h main;
	rmdir ${BUILD_DIR}

