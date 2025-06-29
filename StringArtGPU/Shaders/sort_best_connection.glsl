#version 430

layout(local_size_x = 128) in;


layout(std430, binding = 0) buffer DataBuffer{
	float data[];
};

layout(std430, binding = 1) buffer IndexSortBuffer{
	int sortbuffer[];
};

uniform int blocksize; // In this case it relates to setcount, not to datasize
uniform int datasize;
uniform int setcount;


void main(){
	
	uint setindex = gl_GlobalInvocationID.x;
	uint setindex2 = blocksize + setindex;

	if (setindex >= blocksize) return;
	if (setindex2 >= setcount) return;

	uint dataindex = setindex * datasize;
	uint dataindex2 = setindex2 * datasize;

	float data1 = data[dataindex];
	float data2 = data[dataindex2];

	if (data2 < data1){
		data[dataindex] = data[dataindex2];
		sortbuffer[setindex] = sortbuffer[setindex2];
	}




}