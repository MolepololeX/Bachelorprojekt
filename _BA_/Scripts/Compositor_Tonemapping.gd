# partially copied from godot documentation
@tool
extends CompositorEffect
class_name Tonemapping_Post

enum TonemapperMode{
	linear,
	srgb,
	oklab
}

enum DrawMode{
	visualize_clipping, #0
	colorImage,	# 1
	oklab_d_h,	# 2
	oklab_d_C,	# 3
	oklab_d_L,  # 4
	oklab_d_E,	# 5
	cie_d_H,	# 6
	cie_d_C,	# 7
	cie_d_L,    # 8
	cie_d_E		# 9
}

@export var enable_draw : bool = false
@export var shader_path : String = "res://_BA_/Shaders/sh_glsl_tonemap_testing.glsl"
@export var live_reload : bool = false
@export var reload_interval_frames : int = 60
@export_tool_button("Reload Shader", "Redo") var reload_shader_action = _reinit_shader

@export_group("Uniforms")
@export_range(0.0, 10.0, 0.05) var base_exposure : float = 1.0
@export var tonemapper_mode : TonemapperMode = TonemapperMode.linear
@export_range(0.0, 10.0, 0.05) var tonemapper_exposure : float = 1.5
@export_range(0.0, 10.0, 0.05) var tonemapper_saturation : float = 3
@export var draw_mode : DrawMode = DrawMode.colorImage

@export_group("Post Measurements")
@export_tool_button("Generate Post Image", "ColorRect") var generate_post_image_action = _generate_post_image
@export_tool_button("Generate All delta masks", "InputEventMagnifyGesture") var generate_all_delta_image_action = _generate_delta_images
@export var auto_capture_at_game_launch : bool = false
@export var capture_frame_delay : int = 1000 
@export var raw_texture_data_path : String
@export var raw_texture : Texture2D

var enable_capture_pre = false
var rd: RenderingDevice
var shader: RID
var pipeline: RID
var parameter_storage_buffer := RID()
var shader_is_valid = false

var frame_counter : int = 0

var texture : StringName = "texture_before"
var context : StringName = "capture_textures"



func _generate_delta_images()->void:
	return#TODO


func _generate_post_image()->void:
	###----------------------TODO: alles generalisieren---------------------------------------------
	###----------------------compile shader---------------------------------------------------------
	var local_shader : RID
	var local_pipeline : RID
	# Create RenderingDevice
	var local_rd := RenderingServer.create_local_rendering_device()
	# need to create another instance of the shader
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
	var shader_spirv: RDShaderSPIRV = local_rd.shader_compile_spirv_from_source(shader_source)
	#var shader_spirv: RDShaderSPIRV = file.get_spirv()
	file.close()
	if shader_spirv.compile_error_compute != "":
		push_error(shader_spirv.compile_error_compute)
		return
	local_shader = local_rd.shader_create_from_spirv(shader_spirv)
	if not local_shader.is_valid():
		return
	local_pipeline = local_rd.compute_pipeline_create(local_shader)
	
	
	###----------------------reconstruct image from raw data----------------------------------------
	var raw_image = raw_texture.get_image()
	var size = raw_image.get_size()
	var data_file = FileAccess.open(raw_texture_data_path, FileAccess.READ)
	if data_file == null:
		push_error("Failed to open file")
		return
	raw_image.set_data(
		size.x,
		size.y,
		false,
		Image.FORMAT_RGBAH,
		data_file.get_buffer(data_file.get_length()))
	data_file.close()
	
	
	###----------------------create gpu resources---------------------------------------------------
	var format := RDTextureFormat.new()
	format.width = size.x
	format.height = size.y
	format.format = RenderingDevice.DATA_FORMAT_R16G16B16A16_SFLOAT
	format.usage_bits = RenderingDevice.TEXTURE_USAGE_STORAGE_BIT | RenderingDevice.TEXTURE_USAGE_CAN_COPY_FROM_BIT
	var tex_in : RID = local_rd.texture_create(format, RDTextureView.new(), [raw_image.get_data()])
	var dummy_tex : RID =  local_rd.texture_create(format, RDTextureView.new(), [raw_image.get_data()])
	
	var parameters := PackedFloat32Array([base_exposure, tonemapper_mode, tonemapper_exposure, draw_mode])
	var parameter_data := parameters.to_byte_array()
	var param_storage_buffer = local_rd.storage_buffer_create(parameter_data.size(), parameter_data)
	var uniform_parameter := RDUniform.new()
	uniform_parameter.uniform_type = RenderingDevice.UNIFORM_TYPE_STORAGE_BUFFER
	uniform_parameter.binding = 0
	uniform_parameter.add_id(param_storage_buffer)
	
	var uniform_color := RDUniform.new()
	uniform_color.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
	uniform_color.binding = 1
	uniform_color.add_id(tex_in)
	
	var uniform_before := RDUniform.new()
	uniform_before.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
	uniform_before.binding = 2
	uniform_before.add_id(dummy_tex)## dummy tex is needed for now since we use the same shader for both compositor and this, doesnt matter since its only for testing anyway
	
	var uniform_set := local_rd.uniform_set_create([
		uniform_parameter, 
		uniform_color, 
		uniform_before
		],local_shader, 0
		)
	
	
	###----------------------dispatch compute-------------------------------------------------------
	var x_groups = (size.x - 1) / 8 + 1
	var y_groups = (size.y - 1) / 8 + 1
	var z_groups = 1
	var compute_list:= local_rd.compute_list_begin()
	local_rd.compute_list_bind_compute_pipeline(compute_list, local_pipeline)
	local_rd.compute_list_bind_uniform_set(compute_list, uniform_set, 0)
	local_rd.compute_list_dispatch(compute_list, x_groups, y_groups, z_groups)
	local_rd.compute_list_end()
	
	local_rd.submit()
	local_rd.sync()
	
	
	###----------------------readback texture-------------------------------------------------------
	var texData = local_rd.texture_get_data(tex_in, 0)
	var img = Image.create_from_data(
		size.x,
		size.y,
		false,
		Image.FORMAT_RGBAH,
		texData 
		)
	img.save_png("res://_after_lin.png")
	img.convert(Image.FORMAT_RGB8)
	img.resize(size.x*2, size.y*2, Image.INTERPOLATE_NEAREST)
	img.linear_to_srgb()
	img.save_png("res://_after_srgb.png")	
	
	print("processed image")



func _init():
	rd = RenderingServer.get_rendering_device()
	RenderingServer.call_on_render_thread(_init_shader);
	
	# idk how necessary this is
	var data := PackedFloat32Array()
	data.resize(20)
	data.fill(0)
	var parameter_data := data.to_byte_array()
	parameter_storage_buffer = rd.storage_buffer_create(parameter_data.size(), parameter_data)



func _notification(what):
	if what == NOTIFICATION_PREDELETE:
		if shader.is_valid():
			# Freeing our shader will also free any dependents such as the pipeline!
			rd.free_rid(shader)



func _reinit_shader() -> void:
	RenderingServer.call_on_render_thread(_init_shader);



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



func _render_callback(_p_effect_callback_type, p_render_data):
	
	if not enable_draw:
		return
	#if rd and p_effect_callback_type == EFFECT_CALLBACK_TYPE_POST_TRANSPARENT and shader_is_valid:
	if not (rd and shader_is_valid):
		return
	
	# Get our render scene buffers object, this gives us access to our render buffers.
	# Note that implementation differs per renderer hence the need for the cast.
	var render_scene_buffers: RenderSceneBuffersRD = p_render_data.get_render_scene_buffers()
	if not render_scene_buffers:
		return
	
	# Get our render size, this is the 3D render resolution!
	var size = render_scene_buffers.get_internal_size()
	if size.x == 0 and size.y == 0:
		return
	
	var x_groups = (size.x - 1) / 8 + 1
	var y_groups = (size.y - 1) / 8 + 1
	var z_groups = 1
	
	# ---------------------------update texture if the scene size changes---------------------------
	# If we have buffers for this viewport, check if they are the right size
	if render_scene_buffers.has_texture(context, texture):
		var tf : RDTextureFormat = render_scene_buffers.get_texture_format(context, texture)
		if tf.width != size.x or tf.height != size.y:
			# This will clear all textures for this viewport under this context
			print("clearing textures")
			render_scene_buffers.clear_context(context)
	if !render_scene_buffers.has_texture(context, texture):
		var usage_bits : int = RenderingDevice.TEXTURE_USAGE_SAMPLING_BIT | RenderingDevice.TEXTURE_USAGE_STORAGE_BIT | RenderingDevice.TEXTURE_USAGE_CPU_READ_BIT | RenderingDevice.TEXTURE_USAGE_CAN_COPY_FROM_BIT
		render_scene_buffers.create_texture(context, texture, RenderingDevice.DATA_FORMAT_R16G16B16A16_SFLOAT, usage_bits, RenderingDevice.TEXTURE_SAMPLES_1, size, 1, 1, true, false)

	# Loop through views just in case we're doing stereo rendering. No extra cost if this is mono.
	var view_count = render_scene_buffers.get_view_count()
	for view in range(view_count):
		
		# scuffed way of capturing the raw texture data of the current viewport, needed for accurate graphing of delta values
		if(enable_capture_pre):
			print("capturing raw image")
			enable_capture_pre = false
			var texture_before = render_scene_buffers.get_texture_slice(context, texture, view, 0, 1, 1)
			var texData = rd.texture_get_data(texture_before, 0)
			var img = Image.create_from_data(
				size.x,
				size.y,
				false,
				Image.FORMAT_RGBAH,
				texData 
				)
			var file = FileAccess.open("res://_before_data", FileAccess.WRITE)
			if file == null:
				push_error("Failed to open file")
				return
			file.store_buffer(texData)
			file.close()
			img.save_png("res://_before_lin.png")
			img.convert(Image.FORMAT_RGB8)
			img.resize(size.x*2, size.y*2, Image.INTERPOLATE_NEAREST)
			img.linear_to_srgb()
			img.save_png("res://_before_srgb.png")
		
		# Get the RID for our color image, we will be reading from and writing to it.
		var input_image : RID = render_scene_buffers.get_color_layer(view)
		
		var parameters := PackedFloat32Array([base_exposure, tonemapper_mode, tonemapper_exposure, tonemapper_saturation, draw_mode])
		var parameter_data := parameters.to_byte_array()
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
		
		var texture_image = render_scene_buffers.get_texture_slice(context, texture, view, 0, 1, 1)
		var uniform_before := RDUniform.new()
		uniform_before.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
		uniform_before.binding = 2
		uniform_before.add_id(texture_image)
		
		var uniform_set := UniformSetCacheRD.get_cache(shader, 0, [
			uniform_parameter, 
			uniform_color, 
			uniform_before,
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
	if (frame_counter == capture_frame_delay):
		enable_capture_pre = true
