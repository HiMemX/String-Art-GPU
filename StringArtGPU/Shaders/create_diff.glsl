#version 430

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;


layout(std430, binding = 0) buffer TargetImage{
	float target[];
};

layout(std430, binding = 1) buffer ReproducedImage{
	float reproduced[];
};

layout(std430, binding = 2) buffer DifferenceImage{
	float difference[];
};

layout(std430, binding = 3) buffer NodePositions{
	vec2 nodes[];
};

layout(std430, binding = 4) buffer IndexSortBuffer{
	int sortbuffer[];
};

uniform int width;
uniform int height;
uniform int imagecount;

uniform int nodeindex;

void main(){
	
	uint column = gl_GlobalInvocationID.x;
	uint row = gl_GlobalInvocationID.y;
	uint image = gl_GlobalInvocationID.z;

	if (row >= height) return;
	if (column >= width) return;
	if (image >= imagecount) return;

	vec2 node1 = nodes[nodeindex];
	vec2 node2;

	if (image >= nodeindex){
		node2 = nodes[image + 1]; 
		sortbuffer[image] = int(image) + 1;
	}
	else{
		node2 = nodes[image];
		sortbuffer[image] = int(image);
	}

	uint pixelindex = row * width + column;

	float nodedist = length(node1 - node2);

	float dist = abs((node1.y - node2.y) * (column - node1.x) - (node1.x - node2.x) * (row - node1.y)) / nodedist;

	float value = min(1,dist  / 16 + 0.875);
	
	float o = 2;
	float blurvalue = 1 - 1 / sqrt(2 * 3.141592 * o*o) * exp(-pow(dist / o, 2) / 2);
	//difference[pixelindex] *= value;


	difference[image * width * height + pixelindex] = pow(reproduced[pixelindex] * blurvalue - target[pixelindex], 2);

}