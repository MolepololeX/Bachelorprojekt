shader_type canvas_item;
render_mode blend_mix;

uniform sampler2D viewport_texture : repeat_disable, filter_nearest;

void fragment() {
	vec2 uv = SCREEN_UV;
	vec4 color = texture(viewport_texture, uv);
	if(length(vec3(color.rgb)) <= 1.0){
		COLOR = vec4(0.0);
	}
	COLOR = color;
}
