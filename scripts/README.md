# Scripts

- **`fetch_maven_jars.py`**: descarga en `lib/` las dependencias publicadas en Maven Central. Si falta `ucanload.jar` (no está en Central bajo el mismo artefacto), se extrae de `ucanload.jar.b64` en esta carpeta (mismo paquete que UCanAccess; respete la licencia LGPL al redistribuir).
- **`ucanload.jar.b64`**: copia en Base64 de `ucanload.jar` para entornos sin los JAR en el repositorio (p. ej. GitHub Actions).

Si ya tiene todos los `.jar` en `lib/`, no hace falta ejecutar el fetch.
