shader_type spatial;

render_mode cull_disabled, blend_mix, depth_draw_opaque, diffuse_toon, specular_toon;

uniform vec3 color : source_color;
uniform vec3 color2 : source_color;
uniform sampler2D grassTexture : filter_nearest, repeat_disable;
uniform sampler2D noise : filter_nearest;
uniform float timeScale = 0.01;
uniform float maxAngle = 1.0;
uniform float directionBias = 0.0;

uniform sampler2D highLightNoise : filter_linear;
uniform vec2 highlightScale = vec2(1.0, 1.0);
uniform float highlightThreshold = 0.8;
uniform float highlightTimeScale = 1.0;
uniform float highlightStep = 0.5;

///from the toon shader
uniform int cuts : hint_range(1, 8) = 3;
uniform float wrap : hint_range(-2.0f, 2.0f) = 0.0f;
uniform float steepness : hint_range(1.0f, 8.0f) = 1.0f;
uniform bool clamp_diffuse_to_max = false;
uniform bool use_attenuation = true;



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
	//vertexWorldPos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	//VERTEX = VERTEX + vec3((texture(noise, (instancePos.xz + (TIME * timeScale)) / noiseScale).r - (0.5 - directionBias) ) * maxAngle, 0.0, 0.0) * (1.0 - UV.y);

	vec3 instancePos = MODEL_MATRIX[3].xyz;
	float angle = texture(noise, (instancePos.xz + (TIME * timeScale))).r - (0.5 - directionBias) * maxAngle;
	mat4 rotation = mat4(
		vec4(cos(angle), -sin(angle), 0.0, 0.0),
		vec4(sin(angle), cos(angle), 0.0, 0.0),
		vec4(0.0, 0.0, 1.0, 0.0),
		vec4(0.0, 0.0, 0.0, 1.0)
	);
	VERTEX = (rotation * vec4(VERTEX, 1.0)).xyz;
	
	
	
	vertexWorldPos = NODE_POSITION_WORLD;
}

void fragment() {
	//vec3 noiseLevel = texture(noise, (vertexWorldPos.xz + (TIME * timeScale)) / noiseScale).rgb;
	//vec3 col2 = mix(color, color2, smoothstep(0.6, 0.8, noiseLevel.r));
	//ALBEDO = mix(col2, color2, clamp(UV.y, 0.0, 1.0));
	
	float n = texture(highLightNoise, (vec2(vertexWorldPos.x, -vertexWorldPos.z) - highlightTimeScale) * highlightScale * 0.01).r;
	
	float s = step(highlightThreshold, n);
	vec3 highlight = mix(color, color2, step(highlightStep, UV.y));
	ALBEDO = (color2 * s) + (highlight * (1.0 - s));
	ALPHA = texture(grassTexture, UV.xy).a;
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