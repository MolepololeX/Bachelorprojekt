shader_type spatial;

render_mode cull_disabled, blend_mix, depth_draw_opaque;

uniform vec3 color : source_color;
uniform vec3 color2 : source_color;
uniform sampler2D leaveAlpha : filter_nearest, repeat_disable;
uniform sampler2D noise;

void vertex() {
	// Billboard Mode: Y-Billboard
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
			vec4(normalize(cross(vec3(0.0, 1.0, 0.0), MAIN_CAM_INV_VIEW_MATRIX[2].xyz)), 0.0),
			vec4(0.0, 1.0, 0.0, 0.0),
			vec4(normalize(cross(MAIN_CAM_INV_VIEW_MATRIX[0].xyz, vec3(0.0, 1.0, 0.0))), 0.0),
			MODEL_MATRIX[3]);

	// Billboard Keep Scale: Enabled
	MODELVIEW_MATRIX = MODELVIEW_MATRIX * mat4(
			vec4(length(MODEL_MATRIX[0].xyz), 0.0, 0.0, 0.0),
			vec4(0.0, length(MODEL_MATRIX[1].xyz), 0.0, 0.0),
			vec4(0.0, 0.0, length(MODEL_MATRIX[2].xyz), 0.0),
			vec4(0.0, 0.0, 0.0, 1.0));
	MODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);
	 //Called for every vertex the material is visible on.
	
	vec3 instancePos = MODEL_MATRIX[3].xyz;

	float seed = texture(noise, instancePos.xz).r * PI * 2.0;
	float sway_speed = 0.3;
	float sway_angle = 10.0;
	
	float sway = sin((TIME * sway_speed * 2.0 * PI) + seed) * radians(sway_angle) + seed * .05;
	mat4 rotation = mat4(
		vec4(cos(sway), -sin(sway), 0.0, 0.0),
		vec4(sin(sway), cos(sway), 0.0, 0.0),
		vec4(0.0, 0.0, 1.0, 0.0),
		vec4(0.0, 0.0, 1.0, 1.0)
	);
	
	VERTEX = (rotation * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 uvMirror = UV;

	if (uvMirror.y > 0.5)
    	uvMirror.y = 1.0 - uvMirror.y;

	ALBEDO = mix(color, color2, UV.y);
	ALPHA = 1.0 - texture(leaveAlpha, uvMirror).r;
	ALPHA_SCISSOR_THRESHOLD = 0.5;
	// Called for every pixel the material is visible on.
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
