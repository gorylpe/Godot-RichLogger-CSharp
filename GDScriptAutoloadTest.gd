extends Node

# Verifies the RichLogger autoload (LoggerAutoload.cs) registered by the plugin
# is reachable from GDScript and its C# logging methods are callable.
func _ready() -> void:
	var autoload := get_node_or_null(^"/root/RichLogger")
	if autoload == null:
		print("[GDScriptAutoload] FAIL: /root/RichLogger autoload not found")
		get_tree().quit(1)
		return

	# Call a C# method on the autoload singleton from GDScript.
	RichLogger.Info("GDScriptAutoloadMarker")

	print("[GDScriptAutoload] PASS")
	get_tree().quit(0)
