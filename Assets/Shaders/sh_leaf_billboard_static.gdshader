shader_type spatial;

render_mode cull_disabled, blend_mix, depth_draw_opaque, diffuse_toon, specular_toon;

uniform vec3 color : source_color;
uniform sampler2D leaveAlpha : filter_nearest, repeat_disable;

varying vec3 lightVertexPos;


void vertex() {
	// used in fragment to set the position of the LIGHT_VERTEX to the model origin
	lightVertexPos = MODELVIEW_MATRIX[3].xyz;



	// bilboard code from the standard material
	// !!! removed setting MODEL_VIEW_NORMAL_MATRIX since normals look weird when combined with the next thing
	// Billboard Mode: Particles
	mat4 mat_world = mat4(
			normalize(INV_VIEW_MATRIX[0]),
			normalize(INV_VIEW_MATRIX[1]),
			normalize(INV_VIEW_MATRIX[2]),
			MODEL_MATRIX[3]);
	mat_world = mat_world * mat4(
			vec4(cos(INSTANCE_CUSTOM.x), -sin(INSTANCE_CUSTOM.x), 0.0, 0.0),
			vec4(sin(INSTANCE_CUSTOM.x), cos(INSTANCE_CUSTOM.x), 0.0, 0.0),
			vec4(0.0, 0.0, 1.0, 0.0),
			vec4(0.0, 0.0, 0.0, 1.0));
	MODELVIEW_MATRIX = VIEW_MATRIX * mat_world;

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
}

void fragment() {
	ALBEDO = color;
	ALPHA = 1.0 - texture(leaveAlpha, UV).r;
	ALPHA_SCISSOR_THRESHOLD = 0.5;

	LIGHT_VERTEX = lightVertexPos;
}