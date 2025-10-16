shader_type spatial;

render_mode cull_disabled, blend_mix, depth_draw_opaque, diffuse_toon, specular_toon;

uniform vec3 color : source_color;
uniform vec3 color2 : source_color;
uniform sampler2D grassTexture : filter_nearest, repeat_disable;
uniform sampler2D noise;
uniform float noiseScale = 20.0;
uniform float timeScale = 20.0;
uniform float maxAngle = 1.0;
uniform float directionBias = 0.3;

varying vec3 vertexWorldPos;
varying vec3 lightVertexPos;

void vertex() {
	// used in fragment to set the position of the LIGHT_VERTEX to the model origin
	lightVertexPos = MODELVIEW_MATRIX[3].xyz;



	// bilboard code from the standard material
	// !!! removed setting MODEL_VIEW_NORMAL_MATRIX since normals look weird when combined with the next thing
	// Billboard Mode: Enabled
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
			MAIN_CAM_INV_VIEW_MATRIX[0],
			MAIN_CAM_INV_VIEW_MATRIX[1],
			MAIN_CAM_INV_VIEW_MATRIX[2],
			MODEL_MATRIX[3]);
	// Billboard Keep Scale: Enabled
	MODELVIEW_MATRIX = MODELVIEW_MATRIX * mat4(
			vec4(length(MODEL_MATRIX[0].xyz), 0.0, 0.0, 0.0),
			vec4(0.0, length(MODEL_MATRIX[1].xyz), 0.0, 0.0),
			vec4(0.0, 0.0, length(MODEL_MATRIX[2].xyz), 0.0),
			vec4(0.0, 0.0, 0.0, 1.0));
	//MODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);



	// the next thing in question
	// setting the normals of each vertex to point up for consistent lighting
	vec3 up = vec3(0.0, 1.0, 0.0);
	NORMAL = up;



	// move the leaf in the wind, temp ripped from grass shader
	vertexWorldPos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec3 instancePos = MODEL_MATRIX[3].xyz;
	VERTEX = VERTEX + vec3((texture(noise, (instancePos.xz + (TIME * timeScale)) / noiseScale).r - (0.5 - directionBias) ) * maxAngle, 0.0, 0.0) * (1.0 - UV.y);
}

void fragment() {
	vec3 noiseLevel = texture(noise, (vertexWorldPos.xz + (TIME * timeScale)) / noiseScale).rgb;
	vec3 col2 = mix(color, color2, smoothstep(0.6, 0.8, noiseLevel.r));
	ALBEDO = mix(col2, color2, clamp(UV.y, 0.0, 1.0));
	ALBEDO = color2;
	ALPHA = texture(grassTexture, UV.xy).a;
	ALPHA_SCISSOR_THRESHOLD = 0.5;

	LIGHT_VERTEX = lightVertexPos;
}