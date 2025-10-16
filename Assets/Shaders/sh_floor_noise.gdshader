shader_type spatial;

render_mode diffuse_toon, specular_toon;

uniform vec3 color : source_color;
uniform vec3 color2 : source_color;
uniform float threshold : hint_range(0.0, 1.0, 0.01) = 0.5;
uniform sampler2D noise;
uniform float noiseScale = 20.0;
uniform float timeScale = 20.0;

varying vec3 vertexWorldPos;

void vertex() {
	vertexWorldPos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	float noiseLevel = texture(noise, (vertexWorldPos.xz + (TIME * timeScale)) / noiseScale).r;
	float s = step(noiseLevel, threshold);

	ALBEDO = color * s + color2 * (1.0 - s);
}