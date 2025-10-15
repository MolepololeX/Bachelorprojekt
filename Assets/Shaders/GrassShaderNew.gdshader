shader_type spatial;
render_mode cull_disabled, blend_mix, depth_draw_always;

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
	lightVertexPos = MODELVIEW_MATRIX[3].xyz;
	//// Billboard Mode: Y-Billboard
	//MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
			//vec4(normalize(cross(vec3(0.0, 1.0, 0.0), MAIN_CAM_INV_VIEW_MATRIX[2].xyz)), 0.0),
			//vec4(0.0, 1.0, 0.0, 0.0),
			//vec4(normalize(cross(MAIN_CAM_INV_VIEW_MATRIX[0].xyz, vec3(0.0, 1.0, 0.0))), 0.0),
			//MODEL_MATRIX[3]);
	//// Billboard Keep Scale: Enabled
	//MODELVIEW_MATRIX = MODELVIEW_MATRIX * mat4(
			//vec4(length(MODEL_MATRIX[0].xyz), 0.0, 0.0, 0.0),
			//vec4(0.0, length(MODEL_MATRIX[1].xyz), 0.0, 0.0),
			//vec4(0.0, 0.0, length(MODEL_MATRIX[2].xyz), 0.0),
			//vec4(0.0, 0.0, 0.0, 1.0));
	//MODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);

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

	vec3 up = vec3(0.0, 1.0, 0.0);
	//NORMAL = (MODELVIEW_NORMAL_MATRIX * up).xyz;
	NORMAL = up;

	vertexWorldPos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec3 instancePos = MODEL_MATRIX[3].xyz;
	VERTEX = VERTEX + vec3((texture(noise, (instancePos.xz + (TIME * timeScale)) / noiseScale).r - (0.5 - directionBias) ) * maxAngle, 0.0, 0.0) * (1.0 - UV.y);
}

void fragment() {
	vec3 noiseLevel = texture(noise, (vertexWorldPos.xz + (TIME * timeScale)) / noiseScale).rgb;
	ALBEDO = mix(color, color2, clamp(UV.y, 0.0, 1.0));// * mix(color, color2, noiseLevel.r); //TODO: fix and use UV by fixing up the grass meshes UV mapping
	ALPHA = texture(grassTexture, UV.xy).a;
	ALPHA_SCISSOR_THRESHOLD = 0.5;

	LIGHT_VERTEX = lightVertexPos;



	//TODO: REIMPLEMENT ROLLING GRASS COLOR

	//ALBEDO = texture(noise, (vertexWorldPos.xz + (TIME * 1.0))/ noiseScale).rgb;
	//if (!FRONT_FACING){
		//NORMAL = -NORMAL;
	//}
}