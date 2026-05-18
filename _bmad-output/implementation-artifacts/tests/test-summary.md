# Test Automation Summary

## Generated Tests

### E2E Tests
- [x] `src/Web/Main/tests/e2e/public-discovery.spec.ts` - Home publica, busqueda global, navegacion a ficha, 404 y smoke responsive

## Coverage
- UI features: cobertura automatizada de los flujos publicos entregados en Epica 2
- Home publica: layout base, placeholders y busqueda global
- Ficha publica: navegacion, secciones clave y estado de ticker inexistente
- Accesibilidad basica: skip link y locator semantico del combobox
- Responsive smoke: viewport de 360px sin overflow horizontal

## Verification
- [x] `npm run build --workspace=src/Web/Main`
- [x] `npm test --workspace=src/Web/Main`
- [x] `npm run test:e2e:main`

Resultado final:
- Unit/frontend checks: `15 passed, 0 failed`
- E2E Playwright: `4 passed, 0 failed`

## Notes
- Los E2E interceptan `/api/v1/fibras` y `/api/v1/fibras/{ticker}` para evitar dependencia del SQL Server local configurado en `appsettings.Development.json`.
- La cobertura de backend/API real de Epicas 1 y 2 sigue recayendo en los tests .NET ya existentes del repositorio.

## Next Steps
- Ejecutar `npm run test:e2e:main` en CI despues de provisionar navegadores de Playwright.
- Si se quiere E2E contra backend real, agregar un entorno portable para la API y la base de datos de desarrollo.
