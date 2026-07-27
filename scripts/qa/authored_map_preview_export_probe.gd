extends Node

const ARTIFACT := "res://assets/maps/authored-map-preview.mapspec.json"

func _ready() -> void:
	if not FileAccess.file_exists(ARTIFACT):
		_fail("Export PCK omitted the committed authored preview artifact.")
		return
	var file := FileAccess.open(ARTIFACT, FileAccess.READ)
	if file == null:
		_fail("Export PCK artifact could not be opened.")
		return
	var length := file.get_length()
	file.close()
	if length != 1880:
		_fail("Export PCK artifact length differs from canonical parity.")
		return
	if FileAccess.file_exists("res://addons/map_authoring/plugin.cfg") \
			or FileAccess.file_exists("res://addons/map_authoring/samples/AuthoredMapPreview.tscn"):
		_fail("Export PCK leaked the editor plugin or typed source scene.")
		return
	print("Authored map export PCK probe PASSED: %d bytes sha256 %s." % [length, FileAccess.get_sha256(ARTIFACT)])
	get_tree().quit()

func _fail(message: String) -> void:
	push_error(message)
	get_tree().quit(1)
