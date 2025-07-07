#version 430



layout(local_size_x = 32, local_size_y = 32) in;


layout(std430, binding = 0) buffer PixelCoordinates{
	int indices[];
};

layout(std430, binding = 1) buffer PixelValues{
	float values[];
};

layout(binding = 2, offset = 0) uniform atomic_uint counter;

uniform int width;
uniform int height;

uniform vec2 node1;
uniform vec2 node2;



void main(){
	
	uint column = gl_GlobalInvocationID.x;
	uint row = gl_GlobalInvocationID.y;

	if (row >= height) return;
	if (column >= width) return;

	uint pixelindex = row * width + column;

	float nodedist = length(node1 - node2);

	float dist = abs((node1.y - node2.y) * (column - node1.x) - (node1.x - node2.x) * (row - node1.y)) / nodedist;
	float value = min(1,1 * abs(dist) + 0.2);//(abs(dist)  - 1) / 10 + 1);
	
	if(value > 0.999) return; // Value is not part of line

	uint listindex = atomicCounterIncrement(counter);
	
	indices[listindex] = pixelindex;
	values[listindex] = value;
	
	
}