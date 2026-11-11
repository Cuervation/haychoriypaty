# Arte La Renga — procedencia (2026-11-10)

Arte original generado con la herramienta integrada ImageGen, no fotografías recortadas ni logos oficiales. Los PNG finales se copiaron sin recomposición programática. Esto documenta assets; **no es evidencia Game View**.

## Referencias autorizadas identificadas

- Collage de murales: `/var/folders/bf/q6_2cmvn6ls9pc5h5g7thj8r0000gn/T/codex-clipboard-1b4b178b-7a74-45c1-8f5c-2e2995a876e3.png`.
- Mural león/guitarrista: `/var/folders/bf/q6_2cmvn6ls9pc5h5g7thj8r0000gn/T/codex-clipboard-3ef40b13-e659-4a58-a97f-bdb02a9ecb50.png`.

Se inspeccionaron antes de generar. No se empaquetaron las fotografías en el juego. Las referencias temporales no son una dependencia runtime.

## Finales y fuentes

Raíz final: `/Users/celestino/HayChoriYPaty/Assets/Art/Street/Resources/`.
Raíz de fuentes generadas: `/Users/celestino/.codex/generated_images/01a1c6d0-72e9-7c20-bae1-2600ce45b4a2/`.

| PNG final | Dimensiones | Fuente generada |
|---|---|---|
| street-background-larenga-autodromo-v1.png | 2172×724 | exec-11c2bc48-2e32-4dd0-87bb-fb232577f23b.png |
| street-larenga-fans-walk-v1.png | 1536×1024 | exec-f48c886b-2474-4f9b-81fb-255a3e22d53d.png |
| street-larenga-fans-walk-down-v1.png | 1536×1024 | exec-ed75dab2-ad32-436e-9c20-19d96f75a37f.png |
| street-larenga-fans-front-v1.png | 1536×1024 | exec-f5fd78b4-fb53-44e1-8477-cebc2b449b50.png |
| street-larenga-crest-v1.png | 1254×1254 | exec-549da417-bf1e-4cd0-93c3-3f24ea830e3e.png |

## Especificaciones de prompts finales

Resumen de las instrucciones visuales utilizadas (no transcripción literal del historial de llamadas):

1. **Mural:** ilustración 2D panorámica 3:1 para pared callejera del juego; adaptar los motivos autorizados león y guitarrista, letras LA RENGA, azules/rojos/dorados. Integrar pista, tribunas, escenario y multitud del Autódromo de Buenos Aires. Sin fotografía pegada, sin HUD, trabajadores o vallas operativas dibujados. La primera versión portrait fue reemplazada por la panorámica.
2. **Lateral:** atlas RGBA transparente 1536×1024, seis columnas por cuatro filas, doce identidades en pares adyacentes; rockeros con remeras negras, jeans/bermudas, pelos/edades/géneros diversos y accesorios. Orientación derecha, reversa por flipX. Mantener identidad/escala/pies alineados. Edición final de columnas pares a pose de paso intermedio, alternando con zancada amplia, evitando dos frames idénticos.
3. **Descendente:** conservar exactamente las doce identidades y grilla del lateral, mirando al frente para bajar por conectores; dos pasos con piernas/brazos opuestos por identidad, fondo transparente, sin texto ni grilla visible.
4. **Frontal:** conservar identidades/grilla; primer frame de recepción con manos extendidas y segundo satisfecho con gesto de aprobación, transparente. La salida usa la animación compartida de desaparición/elevación; no introduce otro motor.
5. **Insignia:** emblema original transparente azul marino/dorado/rojo, león rugiente con guitarras cruzadas, LA RENGA y motivo a cuadros del autódromo; no copiar un logo oficial ajeno a las referencias.

Los atlas finales conservan alpha: diagnóstico directo del PNG (no import Unity) observó 53.04% de píxeles alpha cero lateral, 51.39% descendente y 52.51% frontal. Metas propios: atlas/mural TextureImporter sin sprites heredados; insignia Sprite única. La importación final, pivote efectivo y apariencia en Game View siguen pendientes por bloqueo MCP. Manifest SHA-256 en `asset-manifest-sha256.txt`.
