@tool
extends CompositorEffect
class_name Colormapping_Post

enum PaletteType{
	HSL,
	OKLCh
}

enum QuantizationType{
	srgb_Y,
	oklab_L
}

@export
var enable_draw : bool = false
@export
var shader_path : String = "res://_BA_/Shaders/sh_glsl_posterization_color_mapping.glsl"
@export
var live_reload : bool = false
@export
var reload_interval_frames : int = 60
@export_tool_button("Reload Shader", "Redo") var reload_shader_action = _reinit_shader

@export_group("Uniforms")
@export_range(0, 128, 1) var steps : int = 8
@export var palette_type : PaletteType = PaletteType.HSL
@export var quantization_type : QuantizationType = QuantizationType.oklab_L
@export_range(0.0, 0.3, 0.00001) var dither_spread : float = 0.05

@export_group("Palettes")
@export_range(0.0, 360.0, 0.1) var hue_start : float = 0.0
@export_range(-360.0, 360.0, 0.1) var hue_range : float = 120.0
@export_range(0.0, 1.0, 0.01) var lightness_floor : float = 0.1
@export_range(0.0, 1.0, 0.01) var lightness_ceiling : float = 0.9
@export_range(0.0, 1.0, 0.01) var hsl_saturation : float = 0.5
@export_range(0.0, 0.3, 0.001) var oklch_chroma : float = 0.1
@export_range(0.0, 360.0, 0.1) var oklch_hue_offset : float = 0.0
@export var auto_create_palette : bool = false
@export_tool_button("Generate Palettes", "ColorTrackVu") var gen_palettes_action = _create_palettes

var image_hsl : Image
var image_oklch : Image

@export var gamma_correct_preview : bool = true
@export var palette_preview_hsl : Image
@export var palette_preview_oklch : Image
@export_tool_button("Save Palette", "Save") var save_palettes_action = _save_palettes
@export var save_path : String = "res://_BA_/Palettes"
@export var palette_preview_res : int = 128


var palette_hsl: RID
var palette_oklch: RID

var rd: RenderingDevice
var shader: RID
var pipeline: RID
var parameter_storage_buffer := RID()
var shader_is_valid = false
var is_palette_modified = false

var frame_counter : int = 0

func _init():
	rd = RenderingServer.get_rendering_device()
	RenderingServer.call_on_render_thread(_init_shader);
	_create_palettes()
	
	var data := PackedFloat32Array()
	data.resize(20)
	data.fill(0)
	var parameter_data := data.to_byte_array()
	parameter_storage_buffer = rd.storage_buffer_create(parameter_data.size(), parameter_data)




# System notifications, we want to react on the notification that
# alerts us we are about to be destroyed.
func _notification(what):
	if what == NOTIFICATION_PREDELETE:
		if shader.is_valid():
			# Freeing our shader will also free any dependents such as the pipeline!
			rd.free_rid(shader)


func _linear_srgb_to_oklab(c : Vector3) -> Vector3:
	var l = 0.4122214708 * c.x + 0.5363325363 * c.y + 0.0514459929 * c.z
	var m = 0.2119034982 * c.x + 0.6806995451 * c.y + 0.1073969566 * c.z;
	var s = 0.0883024619 * c.x + 0.2817188376 * c.y + 0.6299787005 * c.z;

	var l_ := pow(l, 1.0/3.0);
	var m_ := pow(m, 1.0/3.0);
	var s_ := pow(s, 1.0/3.0);

	return Vector3(
		0.2104542553*l_ + 0.7936177850*m_ - 0.0040720468*s_,
		1.9779984951*l_ - 2.4285922050*m_ + 0.4505937099*s_,
		0.0259040371*l_ + 0.7827717662*m_ - 0.8086757660*s_
	);

func _oklab_to_linear_srgb(c : Vector3) -> Vector3:
	var l_ := c.x + 0.3963377774 * c.y + 0.2158037573 * c.z;
	var m_ := c.x - 0.1055613458 * c.y - 0.0638541728 * c.z;
	var s_ := c.x - 0.0894841775 * c.y - 1.2914855480 * c.z;

	var l := l_*l_*l_;
	var m := m_*m_*m_;
	var s := s_*s_*s_;

	return Vector3(
		+4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
		-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
		-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s
	);

#from https://github.com/flixel-gdx/flixel-gdx/blob/master/flixel-core/src/org/flixel/data/shaders/blend/luminosity.glsl
func _hue_to_rgb(f1 : float, f2 : float, hue : float) -> float:
	if (hue < 0.0):
		hue += 1.0;
	else : if (hue > 1.0):
		hue -= 1.0;
	var res : float;
	if ((6.0 * hue) < 1.0):
		res = f1 + (f2 - f1) * 6.0 * hue;
	else : if ((2.0 * hue) < 1.0):
		res = f2;
	else : if ((3.0 * hue) < 2.0):
		res = f1 + (f2 - f1) * ((2.0 / 3.0) - hue) * 6.0;
	else:
		res = f1;
	return res;

#from https://github.com/flixel-gdx/flixel-gdx/blob/master/flixel-core/src/org/flixel/data/shaders/blend/luminosity.glsl
func _hsl_to_rgb(hsl : Vector3) -> Vector3:
	var rgb : Vector3;
	
	if (hsl.y == 0.0):
		rgb = Vector3(hsl.z, hsl.z, hsl.z); # Luminance
	else:
		var f2 : float;
		
		if (hsl.z < 0.5):
			f2 = hsl.z * (1.0 + hsl.y);
		else:
			f2 = (hsl.z + hsl.y) - (hsl.y * hsl.z);
			
		var f1 := 2.0 * hsl.z - f2;
		
		rgb.x = _hue_to_rgb(f1, f2, hsl.x + (1.0/3.0));
		rgb.y = _hue_to_rgb(f1, f2, hsl.x);
		rgb.z = _hue_to_rgb(f1, f2, hsl.x - (1.0/3.0));
	return rgb;



func _create_palettes() -> void:
	
	image_hsl = Image.create_empty(steps, 1, false, Image.FORMAT_RGBA8)
	image_hsl.fill(Color(1, 0, 0)) # example
	palette_preview_hsl = Image.create_empty(steps, 1, false, Image.FORMAT_RGBA8)
	
	image_oklch = Image.create_empty(steps, 1, false, Image.FORMAT_RGBA8)
	image_oklch.fill(Color(0, 0, 1)) # example
	palette_preview_oklch = Image.create_empty(steps, 1, false, Image.FORMAT_RGBA8)
	
	# not really performant but avoids memory leaks and does not matter since its only for debugging
	if(rd.texture_is_valid(palette_hsl)):
		rd.free_rid(palette_hsl)
	if(rd.texture_is_valid(palette_oklch)):
		rd.free_rid(palette_oklch)
	
	for i in range(steps) :
		
		var col := Color.WHITE
		
		var H := ((float(i) / float(steps)) * (hue_range / 360.0) + (hue_start / 360.0))# needs fract()
		var S := hsl_saturation
		var L : float = lerp(lightness_floor, lightness_ceiling, float(i) / float(steps - 1))
		
		var hsl : Vector3
		hsl.x = H
		hsl.y = S
		hsl.z = L
		var c = _hsl_to_rgb(hsl)# dooes not output linear rgb!!!
		
		col.r = c.x
		col.g = c.y
		col.b = c.z
		
		col = col.srgb_to_linear()# convert to linear before sending to shader
		
		image_hsl.set_pixel(i, 0, col)
	
	for i in range(steps) :
		var col := Color.WHITE
		
		#var h := (((float(i) / float(steps)) * (hue_range / 360.0) + (hue_start / 360.0)) + (oklch_hue_offset / 360.0)) * 6.28318530718 #transform to radiants since oklab hue is -3.14...3.14
		#var C := oklch_chroma
		var L : float = lerp(lightness_floor, lightness_ceiling, float(i) / float(steps - 1))
		#
		#var lab : Vector3
		#lab.x = L
		#lab.y = C * cos(h)
		#lab.z = C * sin(h)
		#var c = _oklab_to_linear_srgb(lab)
		#
		#col.r = c.x
		#col.g = c.y
		#col.b = c.z
		
		var H := ((float(i) / float(steps)) * (hue_range / 360.0) + (hue_start / 360.0))# needs fract()
		var okhsl := Color.from_ok_hsl(H, hsl_saturation, L)
		okhsl = okhsl.srgb_to_linear()
		
		image_oklch.set_pixel(i, 0, okhsl)
	
	palette_preview_hsl.copy_from(image_hsl)
	palette_preview_oklch.copy_from(image_oklch)
	# gamma correct so the preview is correctly displayed from the editor and in the saved png
	if(gamma_correct_preview):
		palette_preview_hsl.linear_to_srgb()
		palette_preview_oklch.linear_to_srgb()
	
	var format := RDTextureFormat.new()
	format.width = image_hsl.get_width()
	format.height = image_hsl.get_height()
	format.format = RenderingDevice.DATA_FORMAT_R8G8B8A8_UNORM
	format.usage_bits = RenderingDevice.TEXTURE_USAGE_SAMPLING_BIT
	format.usage_bits = RenderingDevice.TEXTURE_USAGE_STORAGE_BIT
	palette_hsl = rd.texture_create(format, RDTextureView.new(), [image_hsl.get_data()])
	palette_oklch = rd.texture_create(format, RDTextureView.new(), [image_oklch.get_data()])



func _save_palettes() -> void:
	palette_preview_hsl.resize(palette_preview_res * steps, palette_preview_res, Image.INTERPOLATE_NEAREST)
	palette_preview_hsl.save_png(save_path + EditorInterface.get_edited_scene_root().name + "_HSL" + ".png")
	palette_preview_oklch.resize(palette_preview_res * steps, palette_preview_res, Image.INTERPOLATE_NEAREST)
	palette_preview_oklch.save_png(save_path + EditorInterface.get_edited_scene_root().name + "_OKLCh" + ".png")


func _reinit_shader() -> void:
	RenderingServer.call_on_render_thread(_init_shader);



# load and compile the shader
func _init_shader() -> void:
	
	if not rd:
		return
	
	# TODO: Highly illegal, fix later
	#var shader_file := ResourceLoader.load(shader_path, "" , ResourceLoader.CACHE_MODE_REPLACE)
	var file = FileAccess.open(shader_path, FileAccess.READ)
	
	if not file:
		push_warning("shaderfile was null - " + shader_path)
		return
	var c = file.get_as_text();
	
	var shader_source: RDShaderSource = RDShaderSource.new()
	shader_source.language = RenderingDevice.SHADER_LANGUAGE_GLSL
	shader_source.source_compute = c
	var shader_spirv: RDShaderSPIRV = rd.shader_compile_spirv_from_source(shader_source)
	
	#var shader_spirv: RDShaderSPIRV = file.get_spirv()
	file.close()

	if shader_spirv.compile_error_compute != "":
		push_error(shader_spirv.compile_error_compute)
		return

	shader = rd.shader_create_from_spirv(shader_spirv)
	if not shader.is_valid():
		return

	pipeline = rd.compute_pipeline_create(shader)
	shader_is_valid = true



	# Called by the rendering thread every frame.
func _render_callback(p_effect_callback_type, p_render_data):
	if(auto_create_palette):
		_create_palettes()
	
	if not enable_draw:
		return
	#if rd and p_effect_callback_type == EFFECT_CALLBACK_TYPE_POST_TRANSPARENT and shader_is_valid:
	if rd  and shader_is_valid:
		
		# Get our render scene buffers object, this gives us access to our render buffers.
		# Note that implementation differs per renderer hence the need for the cast.
		var render_scene_buffers: RenderSceneBuffersRD = p_render_data.get_render_scene_buffers()
		if render_scene_buffers:
			# Get our render size, this is the 3D render resolution!
			var size = render_scene_buffers.get_internal_size()
			if size.x == 0 and size.y == 0:
				return

			# We can use a compute shader here.
			var x_groups = (size.x - 1) / 8 + 1
			var y_groups = (size.y - 1) / 8 + 1
			var z_groups = 1
			

			# Loop through views just in case we're doing stereo rendering. No extra cost if this is mono.
			var view_count = render_scene_buffers.get_view_count()
			for view in range(view_count):
				
				# Get the RID for our color image, we will be reading from and writing to it.
				var input_image : RID = render_scene_buffers.get_color_layer(view)
				#var input_depth : RID = render_scene_buffers.get_depth_layer(view)
				#var input_normal : RID = render_scene_buffers.get_texture("forward_clustered", "normal_roughness")

				#var texture_sampler = RDSamplerState.new()
				#texture_sampler.mip_filter = RenderingDevice.SAMPLER_FILTER_NEAREST
				#texture_sampler = rd.sampler_create(texture_sampler)

				var parameters := PackedFloat32Array([steps, palette_type, quantization_type, dither_spread])
				#var inv_proj_mat = p_render_data.get_render_scene_data().get_cam_projection().inverse()
				#var inv_proj_mat_array := PackedVector4Array([inv_proj_mat.x, inv_proj_mat.y, inv_proj_mat.z, inv_proj_mat.w])

				var parameter_data := parameters.to_byte_array()
				#parameter_data.append_array(inv_proj_mat_array.to_byte_array())
				rd.buffer_update(parameter_storage_buffer, 0, parameter_data.size(), parameter_data)

				# Create a uniform set, this will be cached, the cache will be cleared if our viewports configuration is changed.
				var uniform_parameter := RDUniform.new()
				uniform_parameter.uniform_type = RenderingDevice.UNIFORM_TYPE_STORAGE_BUFFER
				uniform_parameter.binding = 0
				uniform_parameter.add_id(parameter_storage_buffer)

				var uniform_color := RDUniform.new()
				uniform_color.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
				uniform_color.binding = 1
				uniform_color.add_id(input_image)
				
				var uniform_palette_hsl := RDUniform.new()
				uniform_palette_hsl.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
				uniform_palette_hsl.binding = 2
				uniform_palette_hsl.add_id(palette_hsl)
				
				var uniform_palette_oklch := RDUniform.new()
				uniform_palette_oklch.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
				uniform_palette_oklch.binding = 3
				uniform_palette_oklch.add_id(palette_oklch)
				#
				#var uniform_depth := RDUniform.new()
				#uniform_depth.uniform_type = RenderingDevice.UNIFORM_TYPE_SAMPLER_WITH_TEXTURE
				#uniform_depth.binding = 2
				#uniform_depth.add_id(texture_sampler)
				#uniform_depth.add_id(input_depth)
#
				#var uniform_normal := RDUniform.new()
				#uniform_normal.uniform_type = RenderingDevice.UNIFORM_TYPE_SAMPLER_WITH_TEXTURE
				#uniform_normal.binding = 3
				#uniform_normal.add_id(texture_sampler)
				#uniform_normal.add_id(input_normal)

				var uniform_set := UniformSetCacheRD.get_cache(shader, 0, [
					uniform_parameter, 
					uniform_color, 
					uniform_palette_hsl,
					uniform_palette_oklch
					])

				# Run our compute shader.
				var compute_list:= rd.compute_list_begin()
				rd.compute_list_bind_compute_pipeline(compute_list, pipeline)
				rd.compute_list_bind_uniform_set(compute_list, uniform_set, 0)
				rd.compute_list_dispatch(compute_list, x_groups, y_groups, z_groups)
				rd.compute_list_end()
	
	
	frame_counter += 1
	if frame_counter > reload_interval_frames and live_reload:
		frame_counter = 0
		_reinit_shader();
		return
