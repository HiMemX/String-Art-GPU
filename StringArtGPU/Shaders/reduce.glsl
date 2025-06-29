#version 430

layout(local_size_x = 64, local_size_y = 8) in;


layout(std430, binding = 0) buffer DataBuffer{
	float data[];
};

uniform int blocksize;
uniform int datasize;
uniform int setcount;


void main(){
	
	uint index = gl_GlobalInvocationID.x;
	uint setindex = gl_GlobalInvocationID.y;

	uint index2 = index + blocksize;

	if (index >= blocksize) return;
	if (index2 >= datasize) return;
	if (setindex >= setcount) return;

	data[index + setindex * datasize] += data[index2 + setindex * datasize];
	data[index + setindex * datasize] /= 2; // Build average error


}