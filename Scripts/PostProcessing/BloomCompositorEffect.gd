@tool
extends CompositorEffect
class_name BloomPostProcessShader

class ShaderData : 
	var shader : RID
	var pipeline : RID
	
	func _init(shader_ : RID, pipeline_ : RID) -> void:
		shader = shader_
		pipeline = pipeline_
	


enum DrawMode{
	none,
	srgb,
	oklab,
	srgb_Y,
}



@export
var enable_draw : bool = false
@export
var shader_path_prepass : String = "res://Assets/Shaders/PostFX/sh_glsl_bloom_prepass.glsl"
@export
var shader_path_horizontal : String = "res://Assets/Shaders/PostFX/sh_glsl_bloom_horizontal.glsl"
@export
var shader_path_vertical : String = "res://Assets/Shaders/PostFX/sh_glsl_bloom_vertical.glsl"
@export
var shader_path_postpass : String = "res://Assets/Shaders/PostFX/sh_glsl_bloom_postpass.glsl"
@export
var live_reload : bool = false
@export
var reload_interval_frames : int = 60

@export_category("Uniforms")
@export_range(0.0, 10.0, 0.05)
var bloom_threshold : float = 1.0
@export_range(0.0, 1.0, 0.05)
var bloom_strength : float = 1.0
@export_range(0.0, 100.0, 0.05)
var bloom_weight : float = 0.5
@export_range(0, 50, 1)
var blurr_kernelsize : int = 1
@export_range(0.0, 50.0, 0.05)
var blurr_kernelspacing : float = 1.0
@export_range(0, 10, 1)
var blurr_passes : int = 1
@export
var draw_mode : DrawMode = DrawMode.none
@export
var half_size : bool = false

@export_tool_button("Reload Shader", "Redo") var reload_shader_action = _init_all_shaders


var rd: RenderingDevice
var parameter_storage_buffer := RID()
var texture: StringName = "texture"
var pong_texture: StringName = "pong_texture"
var context : StringName = "gaussian_bloom"

var shader_data_prepass : ShaderData
var shader_data_horizontal : ShaderData
var shader_data_vertical : ShaderData
var shader_data_postpass : ShaderData

var frame_counter : int = 0



func _init():
	rd = RenderingServer.get_rendering_device()
	_init_all_shaders()
	
	var data := PackedFloat32Array()
	data.resize(20)
	data.fill(0)
	var parameter_data := data.to_byte_array()
	parameter_storage_buffer = rd.storage_buffer_create(parameter_data.size(), parameter_data)



# System notifications, we want to react on the notification that
# alerts us we are about to be destroyed.
func _notification(what):
	if what == NOTIFICATION_PREDELETE:
		if shader_data_prepass.shader.is_valid():
			# Freeing our shader will also free any dependents such as the pipeline!
			rd.free_rid(shader_data_prepass.shader)
			
		if shader_data_horizontal.shader.is_valid():
			# Freeing our shader will also free any dependents such as the pipeline!
			rd.free_rid(shader_data_horizontal.shader)
			
		if shader_data_vertical.shader.is_valid():
			# Freeing our shader will also free any dependents such as the pipeline!
			rd.free_rid(shader_data_vertical.shader)
			
		if shader_data_postpass.shader.is_valid():
			# Freeing our shader will also free any dependents such as the pipeline!
			rd.free_rid(shader_data_postpass.shader)



func _init_all_shaders() -> void:
	RenderingServer.call_on_render_thread(_init_shader_prepass);
	RenderingServer.call_on_render_thread(_init_shader_horizontal);
	RenderingServer.call_on_render_thread(_init_shader_vertical);
	RenderingServer.call_on_render_thread(_init_shader_postpass);


func _init_shader_prepass() -> void:
	shader_data_prepass = _init_shader(shader_path_prepass)

func _init_shader_horizontal() -> void:
	shader_data_horizontal = _init_shader(shader_path_horizontal)
	
func _init_shader_vertical() -> void:
	shader_data_vertical = _init_shader(shader_path_vertical)
	
func _init_shader_postpass() -> void:
	shader_data_postpass = _init_shader(shader_path_postpass)
	
# load and compile the shader
func _init_shader(shader_path) -> ShaderData:
	
	var shader : RID
	var pipeline : RID
	
	if not rd:
		return null
	
	if shader_path == null:
		push_warning("shaderfile was null - " + shader_path)
		return null
		
	# TODO: Highly illegal, fix later
	var file = FileAccess.open(shader_path, FileAccess.READ)
	if not file:
		push_warning("shaderfile was null - " + shader_path)
		return null
	var c = file.get_as_text();
	
	var shader_source: RDShaderSource = RDShaderSource.new()
	shader_source.language = RenderingDevice.SHADER_LANGUAGE_GLSL
	shader_source.source_compute = c
	var shader_spirv: RDShaderSPIRV = rd.shader_compile_spirv_from_source(shader_source)
	
	#var shader_spirv: RDShaderSPIRV = file.get_spirv()
	file.close()

	if shader_spirv.compile_error_compute != "":
		push_error(shader_spirv.compile_error_compute)
		return null

	shader = rd.shader_create_from_spirv(shader_spirv)
	if not shader.is_valid():
		return null

	pipeline = rd.compute_pipeline_create(shader)
	
	var sd = ShaderData.new(shader, pipeline)
	return sd



# Called by the rendering thread every frame., partially from Godot Documentation
func _render_callback(p_effect_callback_type, p_render_data):
	
	if not enable_draw:
		return
	#if rd and p_effect_callback_type == EFFECT_CALLBACK_TYPE_POST_TRANSPARENT and shader_is_valid:
	
	
	if rd  and shader_data_prepass != null and shader_data_horizontal != null and shader_data_vertical != null and shader_path_postpass != null:
		
		# Get our render scene buffers object, this gives us access to our render buffers.
		# Note that implementation differs per renderer hence the need for the cast.
		var render_scene_buffers: RenderSceneBuffersRD = p_render_data.get_render_scene_buffers()
		if render_scene_buffers:
			# Get our render size, this is the 3D render resolution!
			var size = render_scene_buffers.get_internal_size()
			if size.x == 0 and size.y == 0:
				return

			var x_groups = (size.x - 1) / 8 + 1
			var y_groups = (size.y - 1) / 8 + 1
			var z_groups = 1
			
			var render_size : Vector2 = render_scene_buffers.get_internal_size()
			var effect_size : Vector2 = render_size
			if effect_size.x == 0.0 and effect_size.y == 0.0:
				return

			# Render our intermediate at half size
			if half_size:
				effect_size *= 0.5;
			
			# If we have buffers for this viewport, check if they are the right size
			if render_scene_buffers.has_texture(context, texture):
				var tf : RDTextureFormat = render_scene_buffers.get_texture_format(context, texture)
				if tf.width != effect_size.x or tf.height != effect_size.y:
					# This will clear all textures for this viewport under this context
					render_scene_buffers.clear_context(context)

			if !render_scene_buffers.has_texture(context, texture):
				var usage_bits : int = RenderingDevice.TEXTURE_USAGE_SAMPLING_BIT | RenderingDevice.TEXTURE_USAGE_STORAGE_BIT
				render_scene_buffers.create_texture(context, texture, RenderingDevice.DATA_FORMAT_R16G16B16A16_SFLOAT, usage_bits, RenderingDevice.TEXTURE_SAMPLES_1, effect_size, 1, 1, true, false)
				render_scene_buffers.create_texture(context, pong_texture, RenderingDevice.DATA_FORMAT_R16G16B16A16_SFLOAT, usage_bits, RenderingDevice.TEXTURE_SAMPLES_1, effect_size, 1, 1, true, false)
			
			

			# Loop through views just in case we're doing stereo rendering. No extra cost if this is mono.
			var view_count = render_scene_buffers.get_view_count()
			for view in range(view_count):
				
# parameter unifroms
				var parameters := PackedFloat32Array([bloom_threshold, bloom_strength, bloom_weight, blurr_kernelsize, blurr_kernelspacing, draw_mode])
				var parameter_data := parameters.to_byte_array()
				rd.buffer_update(parameter_storage_buffer, 0, parameter_data.size(), parameter_data)
				var uniform_parameter := RDUniform.new()
				uniform_parameter.uniform_type = RenderingDevice.UNIFORM_TYPE_STORAGE_BUFFER
				uniform_parameter.binding = 0
				uniform_parameter.add_id(parameter_storage_buffer)

# get current color layer
				var color_image : RID = render_scene_buffers.get_color_layer(view)
				var uniform_color := RDUniform.new()
				uniform_color.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
				uniform_color.binding = 1
				uniform_color.add_id(color_image)
				
				var texture_image = render_scene_buffers.get_texture_slice(context, texture, view, 0, 1, 1)
				var uniform_texture := RDUniform.new()
				uniform_texture.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
				uniform_texture.binding = 2
				uniform_texture.add_id(texture_image)
				
				var pong_texture_image = render_scene_buffers.get_texture_slice(context, pong_texture, view, 0, 1, 1)
				var uniform_pong_texture := RDUniform.new()
				uniform_pong_texture.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
				uniform_pong_texture.binding = 3
				uniform_pong_texture.add_id(pong_texture_image)

				var uniform_set_prepass := UniformSetCacheRD.get_cache(shader_data_prepass.shader, 0, [
					uniform_parameter, 
					uniform_color, 
					uniform_texture,
					uniform_pong_texture
					])
				var uniform_set_horizontal := UniformSetCacheRD.get_cache(shader_data_horizontal.shader, 0, [
					uniform_parameter, 
					uniform_color, 
					uniform_texture,
					uniform_pong_texture
					])
				var uniform_set_vertical := UniformSetCacheRD.get_cache(shader_data_vertical.shader, 0, [
					uniform_parameter, 
					uniform_color, 
					uniform_texture,
					uniform_pong_texture
					])
				var uniform_set_postpass := UniformSetCacheRD.get_cache(shader_data_postpass.shader, 0, [
					uniform_parameter, 
					uniform_color, 
					uniform_texture,
					uniform_pong_texture
					])

# Run compute shader.
				var compute_list
				
				compute_list = rd.compute_list_begin()
				rd.compute_list_bind_compute_pipeline(compute_list, shader_data_prepass.pipeline)
				rd.compute_list_bind_uniform_set(compute_list, uniform_set_prepass, 0)
				rd.compute_list_dispatch(compute_list, x_groups, y_groups, z_groups)
				rd.compute_list_end()
				
				
				for i in range(0, blurr_kernelspacing):
					
					compute_list = rd.compute_list_begin()
					rd.compute_list_bind_compute_pipeline(compute_list, shader_data_horizontal.pipeline)
					rd.compute_list_bind_uniform_set(compute_list, uniform_set_horizontal, 0)
					rd.compute_list_dispatch(compute_list, x_groups, y_groups, z_groups)
					rd.compute_list_end()
					
					compute_list = rd.compute_list_begin()
					rd.compute_list_bind_compute_pipeline(compute_list, shader_data_vertical.pipeline)
					rd.compute_list_bind_uniform_set(compute_list, uniform_set_vertical, 0)
					rd.compute_list_dispatch(compute_list, x_groups, y_groups, z_groups)
					rd.compute_list_end()
				
				compute_list = rd.compute_list_begin()
				rd.compute_list_bind_compute_pipeline(compute_list, shader_data_postpass.pipeline)
				rd.compute_list_bind_uniform_set(compute_list, uniform_set_postpass, 0)
				rd.compute_list_dispatch(compute_list, x_groups, y_groups, z_groups)
				rd.compute_list_end()
	
	
	frame_counter += 1
	if frame_counter > reload_interval_frames and live_reload:
		frame_counter = 0
		_init_all_shaders();
		return
