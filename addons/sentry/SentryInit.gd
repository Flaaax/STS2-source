extends Node




const RELEASE_INFO_FILENAME: = "release_info.json"

var _is_headless: bool
var _should_sample_event: Callable
var _platform_branch_environment: String = ""

func _ready() -> void :

    var is_autoslay: = "--autoslay" in OS.get_cmdline_args()
    var force_sentry: = "--force-sentry" in OS.get_cmdline_args()
    _is_headless = DisplayServer.get_name().to_lower() == "headless"
    if OS.has_feature("editor") and not is_autoslay and not force_sentry:
        print("[Sentry.GDExtension] Disabled in editor")
        return

    var release_info: = _load_release_info()
    var has_release_info: = not release_info.is_empty()
    var version: String = release_info.get("version", "dev")
    var environment: = "playtesters" if has_release_info else "development"


    SentrySDK.init( func(options: SentryOptions) -> void :
        options.environment = environment
        options.release = version
        options.dsn = ProjectSettings.get_setting("sentry/config/dsn", "")
        options.before_send = _filter_event
        options.debug = force_sentry
        options.diagnostic_level = 0
        options.experimental.enable_metrics = false
    )

    if not SentrySDK.is_enabled():
        print("[Sentry.GDExtension] SDK not enabled (no DSN configured)")
        return


    if has_release_info:
        SentrySDK.set_context("build", {
            "version": version, 
            "branch": release_info.get("branch", "unknown"), 
            "commit": release_info.get("commit", "unknown"), 
            "date": release_info.get("date", "unknown"), 
            "main_hash": release_info.get("main_assembly_hash", "unknown"), 
        })

        SentrySDK.set_tag("branch", release_info.get("branch", "unknown"))
    else:
        SentrySDK.set_context("build", {"type": "development"})


    SentrySDK.set_tag("os", OS.get_name())
    SentrySDK.set_tag("godot_version", Engine.get_version_info().string)


    if is_autoslay:
        SentrySDK.set_tag("autoslay", "true")
        for arg in OS.get_cmdline_args():
            if arg.begins_with("--seed="):
                var seed_value: = arg.substr(len("--seed="))
                SentrySDK.set_tag("autoslay.seed", seed_value)
                break

    print("[Sentry.GDExtension] Initialized: env=%s, release=%s" % [environment, version])

func _filter_event(event: SentryEvent) -> SentryEvent:

    if not _should_sample_event.is_valid():

        if randf() >= 0.1:
            return null
    elif not _should_sample_event.call():
        return null

    if _platform_branch_environment != "":
        event.environment = _platform_branch_environment

    var message: = event.get_exception_value(0)
    if message.is_empty():
        return event


    if "Exception:" in message:
        return null


    if "/build/modules/mono/glue/" in message:
        return null


    if "res://.godot/mono/temp/" in message:
        return null



    if _is_headless and "fmod" in message.to_lower():
        return null



    if _is_headless and "Failed to set animation mix" in message:
        return null



    if _is_headless and "custom_samplers.has" in message:
        return null



    if _is_headless:
        if "Parameter \"mem\" is null" in message:
            return null
        if "Attempting to initialize the wrong RID" in message:
            return null
        if "Initializing already initialized RID" in message:
            return null

    return event

func _load_release_info() -> Dictionary:
    var paths: = _get_possible_release_info_paths()

    for path in paths:
        if not FileAccess.file_exists(path):
            continue

        var file: = FileAccess.open(path, FileAccess.READ)
        if file == null:
            continue

        var json_string: = file.get_as_text()
        file.close()

        var json: = JSON.new()
        var error: = json.parse(json_string)
        if error != OK:
            push_warning("Failed to parse release_info.json: " + json.get_error_message())
            continue

        var data = json.get_data()
        if data is Dictionary:
            return data

    return {}


func _get_possible_release_info_paths() -> PackedStringArray:
    var executable_path: = OS.get_executable_path()
    var executable_dir: = executable_path.get_base_dir()


    var paths: = PackedStringArray(["user://" + RELEASE_INFO_FILENAME])

    if OS.get_name() == "macOS":

        var resources_path: = executable_dir.path_join("..").path_join("Resources")
        paths.append(resources_path.path_join(RELEASE_INFO_FILENAME))
        paths.append(executable_dir.path_join(RELEASE_INFO_FILENAME))
    else:

        paths.append(executable_dir.path_join(RELEASE_INFO_FILENAME))

    return paths





func set_csharp_context(session_id: String, main_assembly_hash: int):
    SentrySDK.set_tag("session_id", session_id)
    SentrySDK.set_context("assembly", {"main_hash": main_assembly_hash})

func set_should_sample_event(should_sample_event: Callable):
    _should_sample_event = should_sample_event
    print("[Sentry.GDExtension] should_sample callable set")

func set_platform_branch(branch: String):
    SentrySDK.set_tag("platform.branch", branch)
    _platform_branch_environment = branch
    print("[Sentry.GDExtension] Platform branch set to " + branch)

func shutdown():
    print("[Sentry.GDExtension] Shutting down")
    SentrySDK.close()

func generate_mock_event():
    var event: = SentrySDK.create_event()
    event.message = "test event"
    SentrySDK.capture_event(event)
    print("Emitted mock event")
