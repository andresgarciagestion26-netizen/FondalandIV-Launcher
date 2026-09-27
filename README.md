# Fondaland IV Launcher

Launcher gráfico para Windows orientado a Minecraft 1.20.1 + Forge.

## Estado del desarrollo
- Interfaz WPF lista como base visual.
- Fondo personalizado de Fondaland IV incluido.
- Comprobación de manifest JSON por HTTPS.
- Verificación SHA-256 de mods locales.
- Detección de mods faltantes/desactualizados.
- Botón JUGAR preparado para conectar el arranque de Forge.

## Siguiente fase
1. Definir URL real del servidor/CDN.
2. Implementar descarga de mods faltantes/desactualizados.
3. Implementar instalación/verificación de Forge 1.20.1.
4. Implementar detección de Java.
5. Lanzar Minecraft con el perfil correcto.
6. Añadir autoactualización del propio launcher.

## Compilación
Requiere .NET 8 SDK en Windows.

`dotnet restore`

`dotnet build -c Release`

Para publicar un EXE autocontenido:

`dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`
