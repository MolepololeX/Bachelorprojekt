shader_type spatial;

render_mode cull_disabled, blend_mix, depth_draw_opaque, diffuse_toon, specular_toon;

uniform vec3 color : source_color;
uniform sampler2D leaveAlpha : filter_nearest, repeat_disable;
uniform sampler2D noise : filter_nearest;
uniform vec3 targetPos;
uniform float timeScale = 0.01;
uniform float maxAngle = 1.0;
uniform float directionBias = 0.0;

///from the toon shader
uniform int cuts : hint_range(1, 8) = 3;
uniform float wrap : hint_range(-2.0f, 2.0f) = 0.0f;
uniform float steepness : hint_range(1.0f, 8.0f) = 1.0f;
uniform bool clamp_diffuse_to_max = false;
uniform bool use_attenuation = true;


varying vec3 lightVertexPos;
varying vec3 testPos;

void vertex() {
	// used in fragment to set the position of the LIGHT_VERTEX to the model origin
	lightVertexPos = MODELVIEW_MATRIX[3].xyz;



	// bilboard code from the standard material
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




	MODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);
	// setting the normals of each vertex to point towards the center of the tree for consistent lighting
	// NOTE: mutiplying my the ViewMatrix since we billboarded the mesh
	vec3 up = vec3(0.0, 1.0, 0.0);
	NORMAL = (VIEW_MATRIX * vec4(-normalize(targetPos - NODE_POSITION_WORLD.xyz), 0.0)).xyz;



	// rotate the leaves to face the center of the tree
	// Center Position Screen Space
	// 1. Transform to clip space
	vec4 targetClip = PROJECTION_MATRIX * VIEW_MATRIX * vec4(targetPos, 1.0);
	// 2. Perspective divide (to normalized device coordinates)
	vec3 targetNDC = targetClip.xyz / targetClip.w;  // range: -1 to +1
	vec2 targetScreen = vec2(targetNDC.x, targetNDC.y);

	// Leaf Position Screen Space
	vec3 leafPos = NODE_POSITION_WORLD.xyz;
	// 1. Transform to clip space
	vec4 leafClip = PROJECTION_MATRIX * VIEW_MATRIX * vec4(leafPos, 1.0);
	// 2. Perspective divide (to normalized device coordinates)
	vec3 leafNDC = leafClip.xyz / leafClip.w;  // range: -1 to +1
	vec2 leafScreen = vec2(leafNDC.x, leafNDC.y);


	vec2 v1 = normalize(vec2(0.0, 1.0));
	vec2 v2 = normalize(targetScreen - leafScreen);

	float angle = acos(dot(v1, v2) / (length(v1) * length(v2)));
	if((v1.x * v2.y - v1.y * v2.x) <= 0.0) angle = -angle;
	mat4 rotation = mat4(
		vec4(cos(angle), -sin(angle), 0.0, 0.0),
		vec4(sin(angle), cos(angle), 0.0, 0.0),
		vec4(0.0, 0.0, 1.0, 0.0),
		vec4(0.0, 0.0, 0.0, 1.0)
	);
	VERTEX = (rotation * vec4(VERTEX, 1.0)).xyz;



	// move the leaf in the wind, temp ripped from grass shader
	//vec3 instancePos = MODEL_MATRIX[3].xyz;
	//VERTEX = VERTEX + vec3((texture(noise, (instancePos.xz + (TIME * timeScale)) / noiseScale).r - (0.5 - directionBias) ) * maxAngle, 0.0, 0.0) * (1.0 - UV.y);
	vec3 instancePos = MODEL_MATRIX[3].xyz;
	angle = texture(noise, (instancePos.xz + (TIME * timeScale))).r - (0.5 - directionBias) * maxAngle;
	rotation = mat4(
		vec4(cos(angle), -sin(angle), 0.0, 0.0),
		vec4(sin(angle), cos(angle), 0.0, 0.0),
		vec4(0.0, 0.0, 1.0, 0.0),
		vec4(0.0, 0.0, 0.0, 1.0)
	);
	VERTEX = (rotation * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	ALBEDO = color;
	ALPHA = 1.0 - texture(leaveAlpha, UV).r;
	ALPHA_SCISSOR_THRESHOLD = 0.5;

	LIGHT_VERTEX = lightVertexPos;
}


void light() {
	//// Attenuation.
	float attenuation = 1.0f;
	if (use_attenuation) {
		attenuation = ATTENUATION;
	}

	// Diffuse lighting.
	float NdotL = dot(NORMAL, LIGHT);
	float diffuse_amount = NdotL + (attenuation - 1.0) + wrap;
	//float diffuse_amount = NdotL * attenuation + wrap;
	diffuse_amount *= steepness;
	float cuts_inv = 1.0f / float(cuts);
	float diffuse_stepped = clamp(diffuse_amount + mod(1.0f - diffuse_amount, cuts_inv), 0.0f, 1.0f);

	// Calculate borders.
	float border = 0.0f;

	// Apply diffuse result to different styles.
	vec3 diffuse = ALBEDO.rgb * LIGHT_COLOR / PI;
	diffuse *= diffuse_stepped * (1.0f - border);


	if (clamp_diffuse_to_max) {
		// Clamp diffuse to max for multiple light sources.
		DIFFUSE_LIGHT = max(DIFFUSE_LIGHT, diffuse);
	} else {
		DIFFUSE_LIGHT += diffuse;
	}
}