shader_type canvas_item;

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_nearest;
uniform float amount : hint_range(0.0, 1.0) = 1.0;
uniform float brightness : hint_range(0.0, 2.0) = 1.0;
uniform float contrast : hint_range(0.0, 3.0) = 1.0;

void fragment() {
    vec3 c = texture(screen_tex, SCREEN_UV).rgb;
    float gray = dot(c, vec3(0.2126, 0.7152, 0.0722));
    vec3 col = mix(c, vec3(gray), amount);

    col = (col - 0.5) * contrast + 0.5;  // pivot at mid-gray
    col *= brightness;

    COLOR = vec4(clamp(col, 0.0, 1.0), 1.0);
}