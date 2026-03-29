@tool
extends CompositorEffect
class_name PostProcessShader

enum TonemapperMode{
	linear,
	srgb,
	oklab
}

enum DrawMode{
	none,		# 0
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

@export
var enable_draw : bool = false
@export
var shader_path : String = "res://Assets/Shaders/PostFX/sh_glsl_tonemap_testing.glsl"
@export
var live_reload : bool = false
@export
var reload_interval_frames : int = 60

@export_category("Uniforms")
@export_range(0.0, 10.0, 0.05)
var base_exposure : float = 1.0
@export
var tonemapper_mode : TonemapperMode = TonemapperMode.linear
@export_range(0.0, 10.0, 0.05)
var tonemapper_exposure : float = 1.5
@export
var draw_mode : DrawMode = DrawMode.none

@export_tool_button("Reload Shader", "Redo") var reload_shader_action = _reinit_shader


var rd: RenderingDevice
var shader: RID
var pipeline: RID
var parameter_storage_buffer := RID()
var shader_is_valid = false

var frame_counter : int = 0

func _init():
	rd = RenderingServer.get_rendering_device()
	RenderingServer.call_on_render_thread(_init_shader);
	
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

				var parameters := PackedFloat32Array([base_exposure, tonemapper_mode, tonemapper_exposure, draw_mode])
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
					#uniform_depth, 
					#uniform_normal
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
