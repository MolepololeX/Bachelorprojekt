shader_type spatial;

render_mode cull_disabled, blend_mix, depth_draw_opaque, diffuse_toon, specular_toon;

uniform vec3 color : source_color;
uniform sampler2D leaveAlpha : filter_nearest, repeat_disable;
uniform sampler2D noise;
uniform vec3 targetPos;

varying vec3 lightVertexPos;
varying vec3 testPos;

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



	// rotate the leaves to face the center of the tree which is hopefull the meshinstance3D
 	//vec3 v1 = NORMAL;
	
	// chatgpt gekocht
	
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
	//testPos = vec3(nodePos, 0.0);

	//// 4. Convert to pixel coordinates
	//vec2 screen_pos = screen_uv * vec2(SCREEN_SIZE);
	
	
	//worldPos = INV_PROJECTION_MATRIX * worldPos;
	//vec3 object_pos_view = (MODELVIEW_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	//objectPosView = object_pos_view;
	//nodePosView = NODE_POSITION_VIEW.xyz;
	//nodePosView = MODEL_MATRIX[3].xyz;
	//vec3 v2 = normalize(vec3(0.0, 0.0, 0.0) - MODEL_MATRIX[3].xyz);
	//if(isnan(v2).x) v2 = -NORMAL;
	//vec3 v2 = normalize(vec3(0.0, 1.0, 1.0));
	float angle = acos(dot(v1, v2) / (length(v1) * length(v2)));
	if((v1.x * v2.y - v1.y * v2.x) <= 0.0) angle = -angle;
	//vec3 x = normalize(cross(v1, v2));
	//vec3 y = vec3(dot(v1, v2));
	//float angle = 0.5;
	mat4 rotation = mat4(
		vec4(cos(angle), -sin(angle), 0.0, 0.0),
		vec4(sin(angle), cos(angle), 0.0, 0.0),
		vec4(0.0, 0.0, 1.0, 0.0),
		vec4(0.0, 0.0, 0.0, 1.0)
	);
	VERTEX = (rotation * vec4(VERTEX, 1.0)).xyz;

	
	// applying rotation; from: https://www.youtube.com/watch?v=g1vH3HeePco&t=8s
	//vec3 instancePos = MODEL_MATRIX[3].xyz;
	//float seed = texture(noise, instancePos.xz).r * PI * 2.0;
	//float sway_speed = 0.3;
	//float sway_angle = 10.0;
	//float sway = sin((TIME * sway_speed * 2.0 * PI) + seed) * radians(sway_angle) + seed * .05;
	//rotation = mat4(
		//vec4(cos(sway), -sin(sway), 0.0, 0.0),
		//vec4(sin(sway), cos(sway), 0.0, 0.0),
		//vec4(0.0, 0.0, 1.0, 0.0),
		//vec4(0.0, 0.0, 0.0, 1.0)
	//);
	//vec3 pivot = normalize(NODE_POSITION_WORLD);
	//// move vertex so pivot is at origin, rotate, then move back
	//vec3 localPos = VERTEX - pivot;
	//localPos = (rotation * vec4(localPos, 1.0)).xyz;
	//VERTEX = localPos + pivot;
	//VERTEX = (rotation * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	// mirroring uvs for the texture, temp until real texture is there
	//vec2 uvMirror = UV;
	//if (uvMirror.y > 0.5)
    	//uvMirror.y = 1.0 - uvMirror.y;

	ALBEDO = color;
	ALPHA = 1.0 - texture(leaveAlpha, UV).r;
	ALPHA_SCISSOR_THRESHOLD = 0.5;

	LIGHT_VERTEX = lightVertexPos;
	//ALBEDO = testPos;
	// Called for every pixel the material is visible on.
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
