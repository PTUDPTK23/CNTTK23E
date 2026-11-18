"""Export BAO-CAO.md to a Word report. Requires python-docx and Pillow."""
from pathlib import Path
import math
import re
from io import BytesIO
from PIL import Image, ImageDraw, ImageFont
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT = Path(__file__).resolve().parent.parent


def diagram(kind):
    im = Image.new("RGB", (1600, 960), "white")
    draw = ImageDraw.Draw(im)
    font_path = Path("C:/Windows/Fonts/arial.ttf")
    font = ImageFont.truetype(str(font_path), 25) if font_path.exists() else ImageFont.load_default(size=25)
    small = ImageFont.truetype(str(font_path), 20) if font_path.exists() else ImageFont.load_default(size=20)

    def text(x, y, value, f=font):
        draw.multiline_text((x, y), value, font=f, fill="#19334a", anchor="mm", align="center")

    def arrow(points, label=""):
        draw.line(points, fill="#496b86", width=3)
        x, y = points[-1]
        px, py = points[-2]
        angle = math.atan2(y-py, x-px)
        draw.polygon([(x, y), (x-15*math.cos(angle-.45), y-15*math.sin(angle-.45)),
                      (x-15*math.cos(angle+.45), y-15*math.sin(angle+.45))], fill="#496b86")
        if label:
            text((points[0][0]+x)/2, (points[0][1]+y)/2-20, label, small)

    def box(x, y, label):
        draw.rounded_rectangle((x-135, y-45, x+135, y+45), radius=15, fill="#edf5fa", outline="#496b86", width=3)
        text(x, y, label)

    if kind == "flowchart":
        nodes = [(160, 200, "Patient Web"), (160, 440, "Staff Web"), (530, 320, "Appointment API"),
                 (900, 160, "PostgreSQL"), (900, 490, "Outbox"), (1310, 490, "RabbitMQ"),
                 (1310, 160, "Queue Worker"), (1310, 790, "Notification")]
        arrow([(295,200),(395,320)])
        arrow([(295,440),(395,320)])
        arrow([(665,300),(765,160)])
        arrow([(665,340),(765,490)])
        arrow([(1035,490),(1175,490)], "publish")
        arrow([(1310,445),(1310,205)], "number.requested")
        arrow([(1175,160),(1035,160)], "assign number")
        arrow([(1200,205),(1035,450)], "outbox event")
        arrow([(1310,535),(1310,745)], "queue.changed")
        arrow([(1175,790),(160,790),(160,485)])
        text(680,760,"SignalR")
        arrow([(160,745),(45,745),(45,200),(25,200)])
        for node in nodes:
            box(*node)
    elif kind == "erDiagram":
        for x, y, label in [(250,180,"doctors"),(800,180,"slots"),(1350,180,"appointments"),
                            (250,610,"doctor_queues"),(800,610,"patients"),(1350,610,"booking_requests")]:
            box(x,y,label)
        arrow([(385,180),(665,180)], "1 : N")
        arrow([(935,180),(1215,180)], "1 : N")
        arrow([(250,225),(250,565)], "1 : N")
        arrow([(935,610),(1350,225)], "1 : N")
        arrow([(1350,225),(1350,565)], "1 : 0..1")
        text(800,820,"Outbox và schema_migrations lưu riêng để phục vụ messaging và migration.",small)
    else:
        names=["Patient", "API", "PostgreSQL", "RabbitMQ", "Worker", "Notification"]
        xs=[120,390,660,930,1200,1470]
        for x, name in zip(xs,names):
            text(x,50,name)
            draw.line([(x,90),(x,900)], fill="#b7c9d8", width=2)
        steps=[(0,1,"POST + retry key"),(1,2,"Reserve + outbox; commit"),(1,0,"ID + token"),
               (1,3,"number.requested"),(3,4,"Deliver"),(4,2,"Assign + outbox; commit"),
               (4,3,"ACK / publish event"),(3,5,"queue.changed"),(5,0,"SignalR"),(0,1,"GET snapshot")]
        for i,(a,b,label) in enumerate(steps):
            y=150+i*72
            arrow([(xs[a],y),(xs[b],y)],label)
    result=BytesIO()
    im.save(result,format="PNG")
    result.seek(0)
    return result


def inline(paragraph, value):
    value=re.sub(r"\[([^\]]+)\]\(([^)]+)\)",r"\1 (\2)",value)
    for part in re.split(r"(\*\*.*?\*\*|`[^`]+`)",value):
        run=paragraph.add_run(part.strip("*`") if part.startswith(("**","`")) else part)
        if part.startswith("**"):
            run.bold=True
        elif part.startswith("`"):
            run.font.name="Consolas"
            run.font.size=Pt(10)


def main():
    doc=Document()
    section=doc.sections[0]
    section.page_width=Cm(21);section.page_height=Cm(29.7)
    section.left_margin=Cm(2.5);section.right_margin=Cm(2)
    section.top_margin=Cm(2);section.bottom_margin=Cm(2)
    normal=doc.styles["Normal"]
    normal.font.name="Times New Roman";normal.font.size=Pt(13)
    normal.paragraph_format.line_spacing=1.15
    normal.paragraph_format.space_after=Pt(7)
    for name in ["Title","Heading 1","Heading 2"]:
        doc.styles[name].font.name="Times New Roman"
        doc.styles[name].font.color.rgb=RGBColor.from_string("19334A")
    doc.core_properties.author="Viet0810"
    doc.core_properties.title="Đề tài 09 — Hệ thống đặt lịch khám và quản lý hàng đợi"
    for value in ["BÀI TẬP LỚN", "PHÁT TRIỂN ỨNG DỤNG PHÂN TÁN"]:
        p=doc.add_paragraph();p.alignment=1;p.add_run(value).bold=True
    p=doc.add_paragraph("ĐỀ TÀI 09\nHỆ THỐNG ĐẶT LỊCH KHÁM\nVÀ QUẢN LÝ HÀNG ĐỢI","Title");p.alignment=1
    p=doc.add_paragraph("Sinh viên: Đàm Văn Việt\nTài khoản GitHub: Viet0810\nEmail: dtc245200682@ictu.edu.vn\nBản bàn giao: 17/11/2026");p.alignment=1
    doc.add_paragraph("C# · .NET 10 · PostgreSQL · RabbitMQ · SignalR").alignment=1
    doc.add_page_break()
    lines=(ROOT/"BAO-CAO.md").read_text(encoding="utf-8").splitlines()
    doc.add_heading("Nội dung",1)
    for line in lines:
        if line.startswith("## "):
            doc.add_paragraph(line[3:])
    doc.add_page_break()
    i=0
    while i<len(lines):
        line=lines[i];i+=1
        if not line.strip():
            continue
        if line.startswith("```"):
            lang=line[3:];block=[]
            while i<len(lines) and not lines[i].startswith("```"):
                block.append(lines[i]);i+=1
            i+=1
            if lang=="mermaid":
                kind=block[0].strip().split()[0]
                doc.add_picture(diagram(kind),width=Cm(16))
                doc.add_paragraph({"flowchart":"Sơ đồ kiến trúc hệ thống", "erDiagram":"Quan hệ dữ liệu chính", "sequenceDiagram":"Luồng đặt lịch và cấp số bất đồng bộ"}[kind],"Caption")
            else:
                p=doc.add_paragraph();run=p.add_run("\n".join(block));run.font.name="Consolas";run.font.size=Pt(10)
        elif line.startswith("|"):
            rows=[line]
            while i<len(lines) and lines[i].startswith("|"):
                rows.append(lines[i]);i+=1
            rows=[r for r in rows if not re.fullmatch(r"[\s|:\-]+",r)]
            table=doc.add_table(rows=0,cols=len(rows[0].strip("|").split("|")));table.style="Table Grid"
            for index,row in enumerate(rows):
                cells=table.add_row().cells
                for cell,value in zip(cells,row.strip("|").split("|")):
                    inline(cell.paragraphs[0],value.strip())
                    for run in cell.paragraphs[0].runs:
                        run.font.size=Pt(10.5)
                        if index==0:run.bold=True
        elif line.startswith("# "):
            doc.add_heading(line[2:],0)
        elif line.startswith("## "):
            doc.add_heading(line[3:],1)
        else:
            style="Normal"
            if line.startswith("- "):
                line=line[2:];style="List Bullet"
            elif re.match(r"^\d+\. ",line):
                line=re.sub(r"^\d+\. ","",line);style="List Number"
            inline(doc.add_paragraph(style=style),line)
    footer=section.footer.paragraphs[0];footer.alignment=2
    footer.add_run("Đề tài 09 · ")
    field=OxmlElement("w:fldSimple");field.set(qn("w:instr"),"PAGE");footer._p.append(field)
    output=ROOT/"BAO-CAO.docx"
    doc.save(output)
    print(f"Exported {output.name}; {len(doc.paragraphs)} paragraphs, {len(doc.tables)} tables, {len(doc.inline_shapes)} diagrams")


if __name__=="__main__":
    main()
