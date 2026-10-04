from pathlib import Path
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT = Path(r"C:\Users\USER\JuegoBoceto\Topo-v.2")
MEDIA = ROOT / ".docx_review" / "images_package" / "word" / "media"
OUT = ROOT / "Guia_visual_scripts_Golpea_al_Topo.docx"
SCRIPTS_CAPTURE = Path(r"C:\Users\USER\AppData\Local\Temp\codex-clipboard-2c6c598c-05ab-4e97-8e75-d0b7f7e45488.png")

doc = Document()
sec = doc.sections[0]
sec.top_margin = Inches(0.7); sec.bottom_margin = Inches(0.7)
sec.left_margin = Inches(0.75); sec.right_margin = Inches(0.75)

styles = doc.styles
styles['Normal'].font.name = 'Arial'; styles['Normal']._element.rPr.rFonts.set(qn('w:hAnsi'), 'Arial'); styles['Normal'].font.size = Pt(10)
for name, size in [('Title', 22), ('Heading 1', 15), ('Heading 2', 12)]:
    st=styles[name]; st.font.name='Arial'; st._element.rPr.rFonts.set(qn('w:hAnsi'),'Arial'); st.font.size=Pt(size); st.font.color.rgb=RGBColor(0,0,0)

def set_cell(cell, text, bold=False, size=9, fill=None):
    if fill:
        tcPr=cell._tc.get_or_add_tcPr(); shd=OxmlElement('w:shd'); shd.set(qn('w:fill'),fill); tcPr.append(shd)
    cell.text=''; p=cell.paragraphs[0]; p.paragraph_format.space_after=Pt(3); p.paragraph_format.line_spacing=1.05
    r=p.add_run(text); r.bold=bold; r.font.name='Arial'; r.font.size=Pt(size); r._element.rPr.rFonts.set(qn('w:hAnsi'),'Arial')
    cell.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER

def image(filename, caption, width=5.7):
    p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.paragraph_format.space_before=Pt(5); p.paragraph_format.space_after=Pt(2)
    p.add_run().add_picture(str(MEDIA/filename), width=Inches(width))
    p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.paragraph_format.space_after=Pt(8)
    r=p.add_run(caption); r.italic=True; r.font.size=Pt(8); r.font.name='Arial'

def image_path(path, caption, width=4.0):
    p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.paragraph_format.space_before=Pt(5); p.paragraph_format.space_after=Pt(2)
    p.add_run().add_picture(str(path), width=Inches(width))
    p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.paragraph_format.space_after=Pt(8)
    r=p.add_run(caption); r.italic=True; r.font.size=Pt(8); r.font.name='Arial'

def heading(text):
    p=doc.add_paragraph(text, style='Heading 1'); p.paragraph_format.space_before=Pt(12); p.paragraph_format.space_after=Pt(5)

def script_table(rows):
    t=doc.add_table(rows=1, cols=2); t.alignment=WD_TABLE_ALIGNMENT.CENTER; t.style='Table Grid'
    set_cell(t.cell(0,0),'Script',True,9,'1F4E78'); set_cell(t.cell(0,1),'Funcion dentro del juego',True,9,'1F4E78')
    for a,b in rows:
        cells=t.add_row().cells; set_cell(cells[0],a,True,9); set_cell(cells[1],b,False,9)
    for row in t.rows:
        for cell in row.cells:
            for p in cell.paragraphs:
                for r in p.runs:
                    if row == t.rows[0]: r.font.color.rgb=RGBColor(255,255,255)
    doc.add_paragraph().paragraph_format.space_after=Pt(2)

p=doc.add_paragraph(style='Title'); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Guia visual de los scripts de Golpea al Topo')
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER
r=p.add_run('Explicacion funcional del videojuego de realidad aumentada'); r.bold=True; r.font.size=Pt(11)
p=doc.add_paragraph('Este documento explica que hace cada grupo de scripts y como se relacionan durante una partida. Las imagenes son capturas del proyecto y sirven como evidencia visual de la configuracion y de las pantallas del juego.')
p.paragraph_format.space_after=Pt(10)

heading('Que representa la carpeta Scripts')
doc.add_paragraph('La imagen muestra la carpeta Assets > WhackAMole > Scripts de Unity. No es una pantalla del juego: es la organizacion del codigo fuente. Cada archivo C# contiene una responsabilidad concreta. Separar las responsabilidades permite modificar una parte del juego sin alterar innecesariamente las demas.')
image_path(SCRIPTS_CAPTURE,'Figura 1. Carpeta Scripts: archivos C# que forman el comportamiento del videojuego.',3.8)
script_table([
('ARPlacementInput','Coloca la zona de juego al tocar una superficie real.'),
('ARPlaneBoundTracker','Mantiene las posiciones de juego dentro del plano AR.'),
('DynamicHole','Controla la aparicion y ocultamiento de un hoyo y su topo.'),
('DynamicMoleManager','Administra tiempo, puntaje, combo, objetivos y dificultad.'),
('GameFlowController','Coordina escaneo, partida y fin del juego.'),
('HammerSwing','Anima el martillo despues de un acierto.'),
('HammerTouchInput','Detecta si el toque del usuario golpea un topo.'),
('MenuMoleDisplay','Muestra el topo animado decorativo del menu.'),
('MoleTypes','Define caracteristicas y puntajes de los tipos de topo.'),
('PointerInput','Lee toques o clics y evita confundir botones con golpes.'),
('ProceduralModelFactory','Construye los modelos basicos y su zona de colision.'),
('ScorePopup','Muestra el puntaje flotante de cada acierto.'),
('UIPressScale','Anima visualmente los botones cuando se presionan.'),
('WhackUIController','Actualiza menu, puntos, combo, tiempo y pantalla final.')])

heading('Flujo general de la aplicacion')
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER
r=p.add_run('Escanear superficie  →  Fijar zona  →  Iniciar partida  →  Golpear topos  →  Mostrar resultado'); r.bold=True; r.font.size=Pt(11)
doc.add_paragraph('El jugador mueve el movil para que la aplicacion reconozca una mesa o el suelo. Cuando toca una superficie valida, se fija una zona circular. Dentro de esa zona aparecen los topos; cada toque valido registra puntos y actualiza la interfaz. Al terminar el tiempo se muestra el puntaje final y se permite reiniciar o cambiar la zona.')
image('image3.png','Figura 1. Jerarquia principal: sesion AR, XR Origin, gestor de juego, interfaz y objetos del escenario.',4.2)

heading('1 Deteccion de superficies y zona de juego')
script_table([
('ARPlacementInput','Recibe el toque inicial del usuario. Comprueba si el toque cae sobre un plano horizontal detectado y fija la zona de juego.'),
('ARPlaneBoundTracker','Guarda la superficie y el centro elegido. Calcula posiciones aleatorias para los hoyos sin salir del plano ni acercarse demasiado al borde.'),
('PointerInput','Lee toques en el movil o clics en el editor. Tambien distingue los botones de interfaz para que no se confundan con acciones del juego.')])
doc.add_paragraph('En palabras simples: estos scripts hacen que el juego no coloque objetos en cualquier parte. Primero verifican una superficie real, luego marcan un espacio de juego visible y finalmente entregan posiciones seguras dentro de ese espacio.')
image('image9.png','Figura 2. Inspector de ARPlacementInput: conexion con raycast, planos, camara, marcador y rastreador de zona.',4.9)
image('image2.png','Figura 3. Inspector de ARPlaneBoundTracker: radio de juego, margen de borde y numero de intentos para buscar posiciones validas.',4.8)
image('image14.png','Figura 4. Resultado para el usuario: circulo verde que indica que la zona fue fijada sobre una superficie.',4.4)

heading('2 Control del flujo de la partida')
script_table([
('GameFlowController','Es el coordinador. Cambia entre escaneo, partida y fin de juego; activa solo la entrada que corresponde a cada momento.'),
('DynamicMoleManager','Administra el temporizador, puntaje, combo, tipos de topo, dificultad y cantidad maxima de hoyos simultaneos.'),
('DynamicHole','Controla un hoyo individual: aparece, muestra el topo, espera un golpe o una fuga, se oculta y solicita otra posicion.')])
doc.add_paragraph('El GameFlowController evita errores de uso: mientras se elige la zona no se puede golpear; cuando inicia la partida se detiene la colocacion; al finalizar se desactiva la entrada de golpes. El DynamicMoleManager lleva las reglas globales y cada DynamicHole se ocupa solamente de su propio ciclo de aparicion.')
image('image7.png','Figura 5. Inspector de GameFlowController: relacion entre plano AR, colocacion, zona, gestor, martillo e interfaz.',5.2)
image('image1.png','Figura 6. Inspector de DynamicMoleManager: referencias a la zona, camara y raiz donde se crean los hoyos.',5.1)

heading('3 Golpe, colision y puntuacion')
script_table([
('HammerTouchInput','Durante la partida proyecta un rayo desde la camara hacia el punto tocado. Si encuentra un topo, registra el acierto.'),
('HammerSwing','Crea la animacion visual del martillo sobre el punto de impacto y la elimina al terminar.'),
('ScorePopup','Muestra un texto flotante, por ejemplo +30, para confirmar visualmente el puntaje obtenido.'),
('MoleTypes','Define los tipos de topo, sus puntos, probabilidad, velocidad, color, escala y accesorio.')])
doc.add_paragraph('El golpe no se produce solo por tocar la pantalla. El sistema comprueba que el toque alcance un topo activo. Asi se evita sumar puntos cuando el usuario toca un espacio vacio o un boton. Cada tipo de topo modifica la recompensa y el reto: el comun vale 10 puntos, el amarillo 30 y el rojo 50.')
image('image8.png','Figura 7. Inspector de HammerTouchInput: camara AR, capa Mole, distancia del raycast y materiales del martillo.',5.2)
image('image10.png','Figura 8. Configuracion de tipos de topo: puntajes, probabilidades, tiempo visible, escala y accesorios.',5.0)
image('image13.png','Figura 9. Vista del juego: hoyos y topos aparecen distribuidos dentro de la zona seleccionada.',4.0)

heading('4 Modelos e interfaz de usuario')
script_table([
('ProceduralModelFactory','Construye por codigo el hoyo, topo y martillo cuando no se asignan modelos externos. Tambien asegura el collider para detectar golpes.'),
('WhackUIController','Maneja las pantallas de inicio, HUD y fin. Actualiza puntos, combo, tiempo y botones.'),
('MenuMoleDisplay','Muestra un topo animado en el menu mediante una camara y una textura propia; es una presentacion visual, no parte de la partida.'),
('UIPressScale','Da retroalimentacion a los botones: se reducen levemente al pulsarlos y pueden tener una animacion de pulso.')])
doc.add_paragraph('La interfaz comunica siempre el estado actual. Antes de jugar informa que se debe fijar una zona; durante la partida muestra puntos, combo y tiempo; al final presenta la puntuacion y permite elegir entre reiniciar o cambiar de zona. El objetivo es que el usuario comprenda que debe hacer sin necesitar instrucciones largas.')
image('image5.png','Figura 10. Carpeta de scripts del proyecto: los componentes estan separados por entrada, juego, interfaz, modelos y efectos.',4.0)
image('image12.png','Figura 11. Pantalla inicial: titulo y boton Jugar antes de que la partida este activa.',3.7)
image('image15.png','Figura 12. HUD durante la partida: puntos, combo y temporizador.',4.4)
image('image16.png','Figura 13. Pantalla final: puntuacion, mejor combo, reinicio o cambio de zona.',4.5)

heading('Como explicarlo en una exposicion')
doc.add_paragraph('“Primero, ARPlacementInput y ARPlaneBoundTracker detectan una superficie real y delimitan la zona donde se desarrollara el juego. Despues, GameFlowController inicia la partida y DynamicMoleManager genera los objetivos dentro de esa zona. Cuando el usuario toca un topo, HammerTouchInput verifica el impacto, se registra el puntaje y la interfaz se actualiza. Finalmente, al terminar el tiempo, el sistema desactiva los golpes y muestra el resultado.”')

doc.core_properties.title='Guia visual de scripts de Golpea al Topo'
doc.core_properties.author=''
doc.save(OUT)
print(OUT)
