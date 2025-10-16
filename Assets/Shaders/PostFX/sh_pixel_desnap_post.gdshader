shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform sampler2D viewport_texture : repeat_disable, filter_nearest;
uniform sampler2D inventory_viewport_texture : repeat_disable, filter_nearest;

uniform float snapDeltaX;
uniform float snapDeltaY;
uniform float width;
uniform float height;

uniform float testMult;

void fragment() {
	vec2 uv = SCREEN_UV;

	// get the size of a viewport pixel in uv space;
	float texelSizeX = 1.0 / width;
	float texelSizeY = 1.0 / height;

	uv.x -= texelSizeX * snapDeltaX;
	uv.y += texelSizeY * snapDeltaY;

	//float n = 8.0;
	vec4 color;

	vec4 invColor = texture(inventory_viewport_texture, SCREEN_UV);
	if(length(vec3(invColor.xyz)) >= 0.0001){
		color = invColor;
	}else{
		color = texture(viewport_texture, uv);
	}

	//float n = 16.0;
	//color = floor(color * (n - 1.0) + 0.5) / (n - 1.0);
	COLOR = color;
}
