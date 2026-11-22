import sys
from PIL import Image, ImageDraw, ImageFont

def render_terminal(title, lines, output_path, width=800):
    bg_color = (30, 30, 30)
    title_bar_color = (45, 45, 45)
    text_color = (204, 204, 204)
    cmd_color = (255, 255, 255)
    green_color = (78, 201, 176)
    yellow_color = (220, 220, 170)
    blue_color = (86, 156, 214)
    red_color = (244, 71, 71)

    # Calculate height
    line_height = 20
    padding = 16
    title_height = 36
    total_height = title_height + padding * 2 + len(lines) * line_height

    img = Image.new('RGB', (width, max(total_height, 120)), bg_color)
    draw = ImageDraw.Draw(img)

    # Title bar
    draw.rectangle([0, 0, width, title_height], fill=title_bar_color)
    
    # Window buttons
    draw.ellipse([14, 12, 24, 22], fill=(255, 95, 86))
    draw.ellipse([32, 12, 42, 22], fill=(255, 189, 46))
    draw.ellipse([50, 12, 60, 22], fill=(39, 201, 63))

    # Font
    try:
        font = ImageFont.truetype("consola.ttf", 14)
        title_font = ImageFont.truetype("segoeui.ttf", 12)
    except:
        font = ImageFont.load_default()
        title_font = font

    # Draw title
    draw.text((width // 2 - len(title) * 3, 10), title, fill=(180, 180, 180), font=title_font)

    # Draw lines
    y = title_height + padding
    for line in lines:
        color = text_color
        if line.startswith("PS ") or line.startswith("$ "):
            color = yellow_color
        elif "Error" in line or "FAILED" in line or "Exception" in line or "refused" in line or "404" in line:
            color = red_color
        elif "succeeded" in line or "graded" in line or "201 Created" in line or "200" in line or "[Echo]" in line:
            color = green_color
        elif line.startswith("[") or line.startswith("info:"):
            color = blue_color
        
        draw.text((padding, y), line, fill=color, font=font)
        y += line_height

    img.save(output_path)
    print(f"Saved terminal screenshot to {output_path}")

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python render_screenshot.py <output_path> <title> <lines...>")
        sys.exit(1)
    out_path = sys.argv[1]
    title_text = sys.argv[2]
    content_lines = sys.argv[3:]
    render_terminal(title_text, content_lines, out_path)
