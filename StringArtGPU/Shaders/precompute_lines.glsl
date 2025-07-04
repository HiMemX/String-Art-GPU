#version 430

layout(local_size_x = 128) in;

layout(std430, binding = 0) buffer StartIndices{
	int indices[];
}

layout(std430, binding = 1) buffer PixelCoordinates{
	int coordinates[];
};

layout(std430, binding = 2) buffer PixelValues{
	float values[];
};