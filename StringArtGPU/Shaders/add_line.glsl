#version 430

layout(local_size_x = 32, local_size_y = 32) in;

layout(std430, binding = 0) buffer ReproducedImage{
	float reproduced[];
};

layout(std430, binding = 1) buffer BlurReproducedImage{
	float blurreproduced[];
};


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
	
	float o = 2;
	float blurvalue = 1 - 1 / sqrt(2 * 3.141592 * o*o) * exp(-pow(dist / o, 2) / 2);

	reproduced[pixelindex] *= value;
	blurreproduced[pixelindex] *= blurvalue;
	//reproduced[pixelindex] = pow(reproduced[pixelindex] * value - target[pixelindex], 2);

}