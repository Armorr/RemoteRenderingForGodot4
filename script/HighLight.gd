extends Node3D

#var isHighlight = false
var mesh

func  _ready():
	mesh = get_node("MeshInstance3D")


func set_highlight(high : bool):
	if high and mesh is MeshInstance3D:
		mesh.mesh.material.next_pass = load("res://material/highlight.tres")
	else:
		mesh.mesh.material.next_pass = null
	
