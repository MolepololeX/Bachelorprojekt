// NOTE: Shader automatically converted from Godot Engine 4.5.stable.mono's StandardMaterial3D.

shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx, unshaded;

uniform vec4 tint : source_color;
uniform sampler2D viewport_texture : source_color, filter_nearest, repeat_enable;
uniform float alpha : hint_range(0.0, 1.0, 0.001) = 1.0;

void vertex() {
}

void fragment() {
	vec2 base_uv = vec2(UV.x, -UV.y);

	vec4 albedo_tex = texture(viewport_texture, base_uv);
	ALBEDO = tint.rgb * albedo_tex.rgb;
	ALPHA = alpha;
}