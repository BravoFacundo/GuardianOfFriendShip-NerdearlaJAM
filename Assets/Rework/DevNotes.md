# Guardian of Friend-Ship — Notas de desarrollo

Última actualización: 22/11/2026. Este archivo y la carpeta `Docs/` quedan **fuera de `Assets/`** a propósito: Unity no los importa y no entran en el build.

---

## 1. Estado actual

- **Versión publicada:** 1.0.1 (WebGL) en itch.io y embebida en el portfolio.
- **Motor:** Unity **2022.3.62f3 LTS** (abrir con la copia `Hub\Editor\2022.3.62f3-x86_64`, que tiene el módulo WebGL).
- **Plataforma:** WebGL con la plantilla propia `Assets/WebGLTemplates/Responsive` (el canvas llena el iframe y mantiene 16:9).
  - Player Settings: Gzip + Decompression Fallback, Code Optimization: Disk Size, Run In Background: off.
  - itch: viewport 1280×720, botón de pantalla completa, sin autostart, sin SharedArrayBuffer.
  - Zipear el **contenido** de la carpeta del build (`index.html` en la raíz del zip).
- **Flujo del juego:** tutorial de 7 pantallas solo en la primera partida (`tutorialCompleted` estático) → 4 rondas → ganar/perder → reintentar.
- **Música:** `MusicManager` con 3 capas sincronizadas (mismo largo, 45,71 s) y crossfade:
  - Calm = `ElEspacioMeRelaja` · Tense = `ElEspacioMeEstresa` (cartel de ronda o meteoritos vivos) · Outro = `ElEspacioMeEntretiene` (ganar/perder).
  - Las pistas se subieron +16 dB (la misma ganancia en las 3) porque estaban muy bajas.

## 2. v1.1 — código listo, falta armar la UI

El código ya está; solo falta construir los objetos en el Canvas y enchufar referencias.

| Script | Qué hace |
|---|---|
| `UIManager.cs` (va en el Canvas) | `SetRound`, `SetHull`, `SetPause`, `SetMuted`, `ShowRetry`. Solo UI, sin gameplay. |
| `GameManager.cs` | Esc/P pausa · M mute (PlayerPrefs) · R reintentar en game over. `uiManager` es **opcional** (si está vacío, el juego anda sin HUD). |
| `ShipPartBehaviour.cs` | Evento estático `HitPointsChanged(int)` cuando la nave recibe un golpe. |

Al perder: 3 s → cartel → botón Reintentar (sin créditos). Al ganar: cartel → créditos + Reintentar.

**Para armar en el Canvas:** texto de controles fijo (incluir "P pausa": en el navegador Esc saca la pantalla completa) · texto de ronda (*Legacy Text*, fuente Exo) · panel de pausa a pantalla completa con "Continuar" → `GameManager.TogglePause` · botón mute con 2 sprites → `GameManager.ToggleMute` · botón Reintentar en la raíz del Canvas → `GameManager.Retry`.

**No armar todavía** los 3 íconos de vida (`hullIcons`): con el diseño v2 la vida pasa a ser por parte de la nave.

## 3. Pendientes de limpieza

- [ ] Objeto **MusicManager**: borrar el **AudioSource viejo** (reproduce *Entretiene* encima de las capas).
- [ ] `Assets/ThirdParty/TextMesh Pro/` tiene una carpeta `Resources` que entra al build (~1 MB). Sacar las fuentes Exo a `ThirdParty/Fonts/` y borrar TMP (no se usa: la UI son imágenes).
- [ ] Carpeta `Prafabs` → `Prefabs` (renombrar desde Unity).
- [ ] Comprimir las pantallas del tutorial, los créditos y los carteles (2 MB cada una, 1920×1080).
- [ ] Borrar la copia de Unity sin usar: `C:\Program Files\Unity\Hub\Editor\2022.3.62f3` (sin `-x86_64`).
- [ ] Orden de código (skill `retomar-proyecto-juego`): el tutorial usa un contador "mágico" con `case`; `ShipPartBehaviour` busca el GameManager por nombre; código de escombros duplicado; variables en español (`perdio`, `gano`).

## 4. Diseño v2 — reglas acordadas

**La nave**
- 5 partes en total: fragmento inicial + cabina (adelante), bahía/depósito (atrás) y dos propulsores (costados).
- Cada parte llega con un amigo y se anexa **al tocar la nave** (si queda muy deforme: ajustar al punto de grilla más cercano). Una vez pegada, queda fija (por ahora no se puede reacomodar).
- **La nave es donde estás vos:** el grupo de partes conectadas que contiene el cuerpo que habitás. Su centro es el promedio de sus partes y su radio, un aro que pasa por el punto más lejano.
- Si se rompe una parte y deja otras desconectadas, esos fragmentos quedan **a la deriva**. Siguen siendo accesibles: te podés pasar a uno (si está en rango) y quedarte ahí, abandonando el resto.

**Los cuerpos**
- Un cuerpo sin nave alrededor se pierde para siempre (incluido tu cuerpo original). Todo es descartable mientras no se cumpla una condición de derrota.
- **Último aliento:** al romperse una parte, su amigo tiene **3–5 s** de uso (reorientar o empujar fragmentos, pasarse a otro amigo). Al terminar:
  - si seguís adentro, volvés al amigo anterior (si no existe, perdés);
  - el amigo queda congelado o con el casco roto, flota a la deriva y no es interactuable.
- **Derrota:** te quedás sin cuerpo habitable o se rompen todas las partes.

**Victoria y fases**
- **Fase 1 (reparar):** ganar = **5 partes conectadas + 5 amigos vivos al mismo tiempo**. Ciclo: oleada → calma → llega un fragmento con amigo. Curva fácil (≈ 3–5 min).
- Al llegar a 5/5, decisión **única**: *Volver a la Tierra* (final + **foto de tu nave** + créditos) o *Seguir viajando*.
- **Fase 2 (infinito):** siguen llegando partes; la dificultad escala hasta ser imposible (≈ ronda 15). Termina siempre perdiendo, con la foto de lo que quedó + rondas + amigos + récord.
- **Foto de la nave:** segunda cámara que solo ve la capa de la nave, encuadrando su radio, renderizada a una RenderTexture y mostrada en la pantalla final (funciona en WebGL).

## 5. Plan técnico v2

**Orden:** 1) `ShipManager` mínimo (centro, radio, partes) → 2) `MeteorSpawner` → 3) anexión y fragmentos a la deriva → 4) daño por parte y último aliento → 5) fases, decisión, foto, récord.

**Problemas de los meteoritos hoy** (`GameManager.RoundOfAsteroids`): ángulo al azar en 360°, todos tardan lo mismo en llegar (llegan juntos desde lados opuestos), aparecen alrededor del (0,0) del mundo y apuntan al cuerpo del jugador al aparecer.

**`MeteorSpawner` (decidido):**
- Oleadas **por sector** (arco), con llegada **en fila** (pensado como *tiempo hasta el impacto*, no como distancia).
- Distancia de aparición = radio de la nave + velocidad × tiempo de llegada (siempre el mismo tiempo de reacción, aunque la nave crezca).
- **Arcos excluidos:** la dirección de las partes que vienen llegando y la de los fragmentos a la deriva. Opcional: no repetir el último sector.
- Apuntan a la nave donde está el jugador. Los meteoritos desviados y los fragmentos que se alejan, salen de cámara y no pueden volver, se eliminan.
- Aviso de la dirección de la oleada (cartel o radar).
- Radio: alcanza con recorrer los colliders de las partes y tomar el punto más lejano al centro (si se usan raycasts, lanzarlos de afuera hacia adentro).

**Oleadas en el Inspector** — campos: `Meteor Count`, `Sectors`, `Sector Width`, `First Arrival`, `Arrival Gap`, `Speed`.

| Ronda (fase 1) | Meteoritos | Sectores | Ancho | Separación |
|---|---|---|---|---|
| 1 | 2 | 1 | 60° | 3 s |
| 2 | 3 | 1 | 70° | 2,5 s |
| 3 | 3 | 1 | 90° | 2 s |
| 4 | 4 | 1 | 90° | 2 s |

Si hacen falta más rondas en la fase 1, se repite la última. **Escalado del infinito:** +1 meteorito cada 2 rondas · separación ×0,9 (mínimo 0,3 s) · 2 sectores desde la ronda 3 del infinito · velocidad ×1,05.

**Responsabilidades:** `ShipManager` (partes, centro, radio, conexiones, fragmentos que llegan) · `MeteorSpawner` (dónde y cuándo) · `GameManager` (flujo de fases y rondas) · `UIManager` (solo UI).

## 6. Dirección de arte (en pausa, revertida)

Se probó un estilo nuevo y se **revirtió**: el proyecto volvió a los sprites originales (pack de plataformas de Kenney, CC0). Referencias en `Docs/Art/`:
- `CurrentSprites_Reference.png` — los sprites en uso hoy, con etiquetas.
- `NewStyle_SpriteSheet_v2.png` — la hoja del estilo nuevo (fondo transparente), con las partes separadas para animar.

**Mapeo personaje → sprite actual → color nuevo:**

| Objeto | Sprite actual | Estilo nuevo |
|---|---|---|
| Friend0 (jugador) | `p1_jump` | verde, 2 ojos + nariz |
| Friend1 | `alienBeige` | violeta claro, ceño fruncido |
| Friend2 | `alienPink` (+ `alienPink_walk2`) | rojo, cíclope sonriente |
| Friend3 | `alienYellow` | amarillo, ojos dormidos y sonrisa |
| Friend4 | `alienBlue` (+ `alienBlue_swim1`) | azul, 3 ojos y sonrisa |
| Cuerpo astral | `ghost_normal` | fantasma blanco |
| Brazo astral | `snakeSlime_hit` | brazo blanco elástico |

Tiles en uso: `metalCenter` (piso), `metalPlatform` (pared), `beamDiagonalLeft` (pared diagonal), `tile_20` / `tile_21` (esquinas del piso), `metalCenterWarning`, `metalCenterSticker`, `metalPlatformWire` (pared con cable). Originales de 70×70 px a 100 PPU (0,7 unidades).

**Lo que aprendimos al implementarlo:**
- Método sin tocar la escena: pisar el PNG manteniendo su `.meta` (mismo GUID) y ajustar los *Pixels Per Unit* para conservar el tamaño en el mundo (amigos: 290 PPU; tiles: lienzo de 140 px a 200 PPU).
- El fondo es azul-violeta casi negro (`#12022B`): los colores tienen que ser vivos y claros, y conviene un **halo claro tipo sticker** alrededor de cada pieza.
- **Tiles del piso:** necesitan versión **sin borde** para encajar (recortar ~18 px de contorno y halo funcionó bien).
- **Paredes:** las tiras con puntas redondeadas no se tocan entre sí. Al recortarles las puntas no convencieron → hay que **redibujarlas como tiras continuas y tileables** (sin borde en los extremos, con borde solo del lado que da al espacio).
- Las 3 filas de extremidades que generó la IA se nombraron `Arm`, `Leg`, `Foot`; revisar si corresponde.

**Prompt base usado:**
```
Redraw every sprite in this sheet in a new art style, keeping the same layout, the same silhouettes, proportions and poses, and the same canvas size for each item (ship tiles must stay perfectly square and tileable at 70x70 px).
Style: hand-drawn 2D cartoon, like a cozy indie space game. Thick, slightly wobbly dark outlines with uneven line weight. Flat colors with one simple cel-shaded shadow tone and a small soft highlight. Chunky, rounded, squishy shapes. Slightly imperfect, as if inked with a marker and colored by hand. No gradients, no vector-perfect curves, no 3D rendering.
Transparent background. No text, no labels.
```
Segunda vuelta: partes separadas para rig (cabeza, torso, brazos, piernas con solapamiento en las articulaciones), colores vivos azul/verde/amarillo/rojo/violeta claro que contrasten con `#12022B`, y halo claro exterior.
