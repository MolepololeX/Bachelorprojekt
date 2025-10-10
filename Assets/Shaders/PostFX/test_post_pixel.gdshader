shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float zoom : hint_range(0.0, 10.0) = 1.0;
uniform float offsetX : hint_range(-1.0, 1.0) = 0.0;
uniform float offsetY : hint_range(-1.0, 1.0) = 0.0;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	//float offsetXT = 320.04684;
	//float offsetYT = 187.52776;
	float offsetXT = offsetX;
	float offsetYT = offsetY;

	//TODO: zoom korrent einbauen mit optional wie vorher
	//TODO: viewport groesse beruecksichtigen

	float uvOX = offsetXT / 1221.0 * 2.0;
	float uvOY = offsetYT / 687.0 * 2.0;
	vec2 uvO = vec2(uvOX, uvOY);

	vec2 uvNDC = (((SCREEN_UV + uvO) * 2.0 - 1.0)) - vec2(1.0, 1.0);

	vec2 uv = (uvNDC);
	if(uv.x >= 1.0 || uv.y >= 1.0 || uv.x <=-1.0 || uv.y <=-1.0){
		COLOR = vec4(1.0);
	}
	else{
		COLOR = texture(screen_texture, uv * 0.5 + 0.5);
	}
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
