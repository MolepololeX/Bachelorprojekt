shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float zoom : hint_range(0.0, 10.0) = 1.0;
uniform bool staticZoom = true;
uniform float offsetX : hint_range(-1.0, 1.0) = 0.0;
uniform float offsetY : hint_range(-1.0, 1.0) = 0.0;

uniform float bgCutX0 : hint_range(0.0, 1.0) = 1.0;
uniform float bgCutX1 : hint_range(0.0, 1.0) = 1.0;
uniform float bgCutY0 : hint_range(0.0, 1.0) = 1.0;
uniform float bgCutY1 : hint_range(0.0, 1.0) = 1.0;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	// transform offset to -1.0, 1.0, digga wirklich keine ahnung einfach so lassen
	float oX = offsetX * SCREEN_PIXEL_SIZE.x;
	float oY = offsetY * SCREEN_PIXEL_SIZE.y;
	float viewportZoomWeirdness = 1.0;
	vec2 uvO = vec2(oX, oY) * viewportZoomWeirdness - 0.5; //TODO: FIX DIGGA WARUM - 0.5 und nich 1.0 bidde mal fixen dange
	
	vec2 uvS;
	vec2 uv;
	
	//apply offset to normal SCREEN_UV before transformation to -1.0, 1.0
	if(staticZoom){
		uvS = ((SCREEN_UV * 2.0 - 1.0) * zoom) * 0.5 + 0.5;
		uv = (((uvS + uvO) * 2.0 - 1.0));
	}
	else{
		uvS = SCREEN_UV;
		uv = (((uvS + uvO) * 2.0 - 1.0));
		uv = zoom * uv;
	}

	
	
	if(uv.x >= bgCutX0 || uv.y >= bgCutY0 || uv.x <=-bgCutX1 || uv.y <=-bgCutY1){
		COLOR = vec4(vec3(0.0), 1.0);
	}
	else{
		COLOR = texture(screen_texture, uv * 0.5 + 0.5);
	}
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
