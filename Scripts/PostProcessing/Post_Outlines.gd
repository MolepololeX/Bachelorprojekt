@tool
extends CompositorEffect
class_name PostProcessShader

@export
var shader_path : String = "res://Assets/Shaders/PostFX/Post_Outline_Shader.glsl"

@export_tool_button("Reload Shader", "Redo") var reload_shader_action = _reinit_shader

var rd: RenderingDevice
var shader: RID
var pipeline: RID

var shader_is_valid = false;

func _init():
	effect_callback_type = EFFECT_CALLBACK_TYPE_POST_TRANSPARENT
	rd = RenderingServer.get_rendering_device()
	RenderingServer.call_on_render_thread(_init_shader);

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
	if rd and p_effect_callback_type == EFFECT_CALLBACK_TYPE_POST_TRANSPARENT and shader_is_valid:
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

			# Push constant.
			var push_constant: PackedFloat32Array = PackedFloat32Array()
			push_constant.push_back(size.x)
			push_constant.push_back(size.y)
			push_constant.push_back(0.0)
			push_constant.push_back(0.0)

			# Loop through views just in case we're doing stereo rendering. No extra cost if this is mono.
			var view_count = render_scene_buffers.get_view_count()
			for view in range(view_count):
				# Get the RID for our color image, we will be reading from and writing to it.
				var input_image = render_scene_buffers.get_color_layer(view)

				# Create a uniform set.
				# This will be cached; the cache will be cleared if our viewport's configuration is changed.
				var uniform: RDUniform = RDUniform.new()
				uniform.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
				uniform.binding = 0
				uniform.add_id(input_image)
				var uniform_set = UniformSetCacheRD.get_cache(shader, 0, [ uniform ])

				# Run our compute shader.
				var compute_list:= rd.compute_list_begin()
				rd.compute_list_bind_compute_pipeline(compute_list, pipeline)
				rd.compute_list_bind_uniform_set(compute_list, uniform_set, 0)
				rd.compute_list_set_push_constant(compute_list, push_constant.to_byte_array(), push_constant.size() * 4)
				rd.compute_list_dispatch(compute_list, x_groups, y_groups, z_groups)
				rd.compute_list_end()
	
	_reinit_shader();
