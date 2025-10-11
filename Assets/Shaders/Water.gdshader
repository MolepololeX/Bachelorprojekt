shader_type spatial;

uniform vec3 color : source_color;
uniform vec3 color2 : source_color;
uniform vec3 color3 : source_color;

uniform float threshold : hint_range(0.0, 1.0, 0.01) = 0.5;
uniform float threshold2 : hint_range(0.0, 1.0, 0.01) = 0.5;
uniform sampler2D noise;
uniform sampler2D noise2;
uniform float waterStretch : hint_range(0.0, 10.0, 0.01) = 1.0;

uniform float noiseScale = 20.0;
uniform float noiseScale2 = 20.0;
uniform float timeScale = 20.0;
uniform float timeScale2 = 20.0;


varying vec3 vertexWorldPos;

void vertex() {
	vertexWorldPos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 uvO = vec2(vertexWorldPos.x, vertexWorldPos.z * waterStretch);
	float noiseLevel = texture(noise2, (uvO + (TIME * timeScale)) / noiseScale).r;
	 noiseLevel += texture(noise2, (uvO + (TIME * timeScale) * 2.0) / noiseScale).r;
	float s = step(noiseLevel, threshold);
	
	noiseLevel = texture(noise2, (uvO + (TIME * timeScale2)) / noiseScale2).r;
	noiseLevel += texture(noise2, (uvO + (TIME * timeScale2) * 2.0) / noiseScale2).r;
	float c = step(threshold2, noiseLevel);
	
	vec3 col = mix(color2, color, s);
	col = mix(col, color3, c);
	ALBEDO = col;
	
	//ALPHA = s * .8;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
