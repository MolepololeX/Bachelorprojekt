shader_type spatial;
uniform vec3 color : source_color;
uniform sampler2D dither;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec2 scaledUV = 1.0 - vec2(SCREEN_UV.x * VIEWPORT_SIZE.x / VIEWPORT_SIZE.y, SCREEN_UV.y);
	scaledUV *= 2.0;
	ALBEDO = color;
	ALPHA = step(0.1, texture(dither, scaledUV).r);
}

void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
}
