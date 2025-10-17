shader_type spatial;

render_mode unshaded;

uniform sampler2D noise;
uniform float noiseScale = 1.0;
uniform float scaleFalloff = 2.0;
uniform float timeScale = 1.0;
uniform float threshold : hint_range(0.0, 1.0, 0.01) = 0.5;

varying vec3 vertexWorldPos;

void vertex() {
	vertexWorldPos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	float noiseLevel = texture(noise, (vertexWorldPos.xz + (TIME * timeScale)) * noiseScale).r;
	float noiseLevel2 = texture(noise, (vertexWorldPos.xz + (TIME * scaleFalloff * timeScale)) * noiseScale * scaleFalloff).r;
	float s = noiseLevel + noiseLevel2;
	s = step(s, threshold);

	ALBEDO = vec3 (0.38, 0.537, 0.929);
	
	ALPHA = s;
	ALPHA_SCISSOR_THRESHOLD = .5;
}