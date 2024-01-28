extends Camera3D

var selected_obj

var _a = false
var _s =false
var _w = false
var _d= false
var _q = false
var _e = false

var _mouse_position= Vector2(0,0)
var sec =0.5

var _dir_move
var speed= 10.0

func _ready():

	pass
	
func _process(delta):
	_update_rotate()
	_update_move(delta)
	
	pass
	

func _update_rotate():
	if Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
		_mouse_position *= 0.5
		var offsetX = _mouse_position.x
		var offsetY = _mouse_position.y
		
		rotate_y(deg_to_rad(-offsetX))
		
		var r_x = get_rotation_degrees().x + - offsetY
		if r_x <= -89 or r_x >= 89:
			return
		rotate_object_local(Vector3(1,0,0),deg_to_rad(-offsetY))
		pass
	pass 

func _update_move(delta):
	_dir_move = Vector3((_d as float) - (_a as float),(_e as float) - (_q as float),(_s as float) - (_w as float))
	
	#_dir_move = self.transform.basis*_dir_move
	_dir_move = _dir_move.normalized() * speed * delta
	

	self.translate(_dir_move)
	pass
	

func get_selection():
	var worldspace = get_world_3d().direct_space_state
	
	var mouse_pos = get_viewport().get_mouse_position();
	var start = project_ray_origin(mouse_pos)
	var end = start + project_ray_normal(mouse_pos) * 2000
	var ray  =  PhysicsRayQueryParameters3D.create(start,end)

	
	var result = worldspace.intersect_ray(ray)
	print(result)
	if result and result.collider and result.collider.has_method("playAnimation"):
		result.collider.playAnimation()
		print(result.collider)


func _input(event):
	if event is InputEventScreenDrag:
		handle_screen(event)
		return true
	
	if event is InputEventScreenTouch:
		if $"../Control/Button2".get_global_rect().has_point(event.position):
			$"../Control/Button2".emit_signal("pressed")
			return true
		handle_screen(event)
		return true
	
	if event is InputEventMagnifyGesture:
		handle_gesture(event)
		return
	
	if event is InputEventMouseMotion:
		_mouse_position =  event.relative
	
	if event is InputEventKey:
		match event.keycode:
			KEY_W:
				_w = event.pressed
			KEY_S:
				_s = event.pressed
			KEY_A:
				_a = event.pressed
			KEY_D:
				_d = event.pressed
			KEY_Q:
				_q = event.pressed
			KEY_E:
				_e = event.pressed
			
	if Input.is_key_pressed(KEY_SHIFT):
		speed = 100
	else:
		speed = 5
					
	
	if event is InputEventMouseButton:
		match event.button_index:
			MOUSE_BUTTON_RIGHT:
				Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED  
				if event.is_pressed() else Input.MOUSE_MODE_VISIBLE)
			MOUSE_BUTTON_LEFT:
				print(event.position)

	#if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_RIGHT:
	#	get_selection()
	return true


func handle_screen(event):
	if event is InputEventScreenDrag:
		_mouse_position = event.relative
		var offsetX = _mouse_position.x
		var offsetY = _mouse_position.y
		
		rotate_y(deg_to_rad(-offsetX))
		
		var r_x = get_rotation_degrees().x + - offsetY
		if r_x <= -89 or r_x >= 89:
			return
		rotate_object_local(Vector3(1,0,0),deg_to_rad(-offsetY))
	else:
		print("--  ", event.position)
		select_obj(event.position)
	pass


func handle_gesture(event : InputEventMagnifyGesture):
	var delta = 0.1 if event.factor > 0 else -0.1
	if fov + delta > 120.0 or fov + delta < 40:
		return
	else:
		fov += delta
	pass
	
	
func select_obj(position):
	var worldspace = get_world_3d().direct_space_state
	
	var start = project_ray_origin(position)
	var end = start + project_ray_normal(position) * 2000
	print(start, self.position)
	
	var ray  =  PhysicsRayQueryParameters3D.create(start,end)

	var result = worldspace.intersect_ray(ray)
	if result.has("collider"):
		var obj = result["collider"]
		if obj.has_method("set_highlight"):
			print("High Light!")
			if selected_obj:
				selected_obj.set_highlight(false)
			obj.set_highlight(true)
			selected_obj = obj
	else:
		if selected_obj:
			selected_obj.set_highlight(false)
	pass
