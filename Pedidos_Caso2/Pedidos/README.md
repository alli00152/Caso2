# Pedidos — Lección 11: Gestión segura de órdenes, despacho y rastreo

Aplicación ASP.NET MVC 5 (.NET Framework 4.8) con arquitectura por capas, Autofac, Service/Result Pattern,
ASP.NET Identity (roles y propiedad de la orden), FluentValidation, AutoMapper, filtros MVC, Serilog,
concurrencia optimista (RowVersion) y pruebas con xUnit/Moq/FluentAssertions/Coverlet.

> Curso Programación Avanzada (SC-601), Universidad Fidélitas. Trabajo grupal.
> Identity, roles, filtros personalizados, Serilog, RowVersion/índice único y las librerías de pruebas
> **no se vieron en el material de clase**; se usan únicamente porque el enunciado de la Lección 11 las exige.

## 1. Estructura de la solución

| Proyecto | Responsabilidad | Referencias |
|---|---|---|
| `Pedidos.Domain` | Entidades `Orden`, `DetalleOrden`, enum `EstadoOrden`, reglas de transición | — |
| `Pedidos.Application` | Servicio, `Resultado`, contratos, requests, validadores, DTO, perfil AutoMapper | Domain |
| `Pedidos.Infrastructure` | `PedidosDbContext` (Identity), repositorio, migraciones, seed | Application, Domain |
| `Pedidos.Web` | Controladores, filtros, ViewModels/FormModels, vistas, Autofac, OWIN | Application, Domain, Infrastructure (solo para registrar Autofac) |
| `Pedidos.Application.Tests` | Pruebas unitarias (puntos extra) | Application, Domain |

## 2. Requisitos
- Windows con Visual Studio 2019/2022 (carga de trabajo "ASP.NET y desarrollo web") y .NET Framework 4.8.
- SQL Server LocalDB o SQL Server Express.
- Acceso a NuGet.

## 3. Paquetes NuGet
Los proyectos SDK (`Domain`, `Application`, `Application.Tests`) ya declaran sus paquetes (restauración automática).
Los proyectos de estilo antiguo (`Infrastructure`, `Web`) requieren instalar los paquetes desde
**Herramientas > Administrador de paquetes NuGet > Consola** (si ya existe `packages.config`, solo faltarán los marcados con *):

```powershell
# Infrastructure
Install-Package Microsoft.AspNet.Identity.EntityFramework -Version 2.2.4 -ProjectName Pedidos.Infrastructure   # *
Install-Package EntityFramework -Version 6.5.2 -ProjectName Pedidos.Infrastructure

# Web
Install-Package Autofac.Mvc5 -ProjectName Pedidos.Web
Install-Package AutoMapper -Version 16.2.0 -ProjectName Pedidos.Web
Install-Package Microsoft.Extensions.Logging.Abstractions -ProjectName Pedidos.Web          # requerido por AutoMapper 16 (NullLoggerFactory)
Install-Package FluentValidation -Version 8.6.3 -ProjectName Pedidos.Web
Install-Package FluentValidation.MVC5 -Version 8.6.1 -ProjectName Pedidos.Web               # * verificar compatibilidad con FluentValidation 8.x
Install-Package Microsoft.AspNet.Identity.EntityFramework -Version 2.2.4 -ProjectName Pedidos.Web   # * Web usa ApplicationUser (hereda de IdentityUser)
Install-Package Microsoft.AspNet.Identity.Owin -Version 2.2.4 -ProjectName Pedidos.Web      # *
Install-Package Microsoft.Owin.Host.SystemWeb -ProjectName Pedidos.Web                      # *
Install-Package Microsoft.Owin.Security.Cookies -ProjectName Pedidos.Web                    # *
Install-Package Serilog -ProjectName Pedidos.Web                                            # *
Install-Package Serilog.Sinks.Console -ProjectName Pedidos.Web                              # *
Install-Package Serilog.Sinks.File -ProjectName Pedidos.Web                                 # *
```

Si NuGet reporta un conflicto de versiones (p. ej. FluentValidation.MVC5 con FluentValidation 8.6.3), instale
la versión de `FluentValidation.MVC5` que declare compatibilidad con la versión de `FluentValidation` elegida y avísele al grupo/docente.

## 4. Cadena de conexión (sin credenciales)
En `Pedidos.Web/Web.config` (autenticación integrada de Windows; no hay usuario ni contraseña):

```xml
<connectionStrings>
  <add name="PedidosConnection"
       connectionString="Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=PedidosDb;Integrated Security=True;MultipleActiveResultSets=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

## 5. Usuarios iniciales (seed) sin credenciales en el repositorio
1. Copie `Pedidos.Web/Secrets.config.example` a `Pedidos.Web/Secrets.config` (este archivo **no se versiona**).
2. Complete los valores (correo y contraseña de Administrador y Operador). La contraseña debe cumplir la política:
   mínimo 8 caracteres, con mayúscula, minúscula y dígito.
3. El `Seed` crea los roles `Administrador`, `Operador` y `Cliente` y, solo si hay valores configurados, los dos usuarios.
   Las contraseñas las procesa Identity (hash); nunca se guardan en texto plano ni se escriben en el log.
4. Los clientes se crean desde la pantalla **Registro** y reciben el rol `Cliente`.

## 6. Migraciones
Use una **base de datos nueva**: la migración inicial ya existente (`CrearOrdenes`) no tiene propietario, y agregar
la columna obligatoria `ApplicationUserId` fallaría con filas previas.

```powershell
# 1. Tablas de Identity, propietario y RowVersion
Add-Migration AgregarIdentityYPropietarioOrden -ProjectName Pedidos.Infrastructure -StartUpProjectName Pedidos.Web

# 2. Migración vacía para el índice único filtrado del número de rastreo
Add-Migration UnicidadRastreoFiltrada -ProjectName Pedidos.Infrastructure -StartUpProjectName Pedidos.Web
```

Edite `UnicidadRastreoFiltrada` (EF6 no genera un índice único filtrado por anotaciones):

```csharp
public override void Up()
{
    DropIndex("dbo.Ordenes", "IX_Ordenes_NumeroRastreo");
    Sql("CREATE UNIQUE NONCLUSTERED INDEX [UX_Ordenes_NumeroRastreo] " +
        "ON [dbo].[Ordenes]([NumeroRastreo]) WHERE [NumeroRastreo] IS NOT NULL");
}

public override void Down()
{
    Sql("DROP INDEX [UX_Ordenes_NumeroRastreo] ON [dbo].[Ordenes]");
    CreateIndex("dbo.Ordenes", "NumeroRastreo", name: "IX_Ordenes_NumeroRastreo");
}
```

*Justificación:* muchas órdenes aún no tienen rastreo (`NULL`); un índice único normal solo permitiría un `NULL`.
El filtro `WHERE NumeroRastreo IS NOT NULL` garantiza unicidad solo para los rastreos asignados.

Luego:

```powershell
Update-Database -ProjectName Pedidos.Infrastructure -StartUpProjectName Pedidos.Web
```

## 7. Ejecución
1. Establezca `Pedidos.Web` como proyecto de inicio y presione **F5**.
2. Ingrese con el Administrador/Operador del seed, o regístrese como Cliente.
3. Los registros de Serilog se escriben en consola y en `Pedidos.Web/App_Data/logs/pedidos-AAAAMMDD.log`.

## 8. Pruebas y cobertura
```powershell
dotnet test Pedidos.Application.Tests --collect:"XPlat Code Coverage"
```
El reporte `coverage.cobertura.xml` queda en `Pedidos.Application.Tests/TestResults/<guid>/`. También puede usar el
Explorador de pruebas de Visual Studio.

## 9. Decisiones de diseño relevantes
- **Autorización en el servidor:** el listado y las consultas de un `Cliente` se filtran por `ApplicationUserId` en servicio/repositorio; ocultar botones no es seguridad.
- **Propietario nunca desde el formulario:** se toma de `IUsuarioActual`.
- **Filtros:** `RegistrarAuditoria` (selectivo, solo UserId/OrdenId/resultado; sin dirección ni contraseñas), `RequiereOrdenPropia` (por acción, devuelve 400/403/404), `ManejarExcepcion` (global; 400 para antiforgery, 500 con vista segura y `ErrorId`).
- **Result Pattern separado de excepciones:** errores esperados → `Resultado` (400/403/404/409); inesperados → filtro (500).
- **Concurrencia:** `RowVersion` viaja en un campo oculto; si cambió, 409 con mensaje de recarga (no se sobrescribe).
- **Rastreo duplicado:** verificación previa en el servicio + índice único filtrado + captura del error SQL 2601/2627 → 409.
- **Cancelación:** solo antes del envío y solo el Administrador. Operador: confirmar, preparar, enviar y marcar entregada.
- **Rastreo ajeno:** un Cliente que consulta el rastreo de otra persona recibe 404 (no se confirma su existencia).
- **PRG:** todo POST exitoso redirige con `TempData`; los POST inválidos vuelven a mostrar el formulario con errores (400).
- **AutoMapper con Autofac:** `ApplicationMappingProfile` y `WebMappingProfile` se registran como `Profile` en `AutofacConfig`; al iniciar se resuelve la `MapperConfiguration` y se ejecuta `AssertConfigurationIsValid()`. Además, una prueba automatizada verifica el perfil de Application.
- **No se mapeó `CrearOrdenRequest → Orden`:** el enunciado lo pide solo "si el servicio lo requiere"; la entidad se construye con su constructor validado.
- **Filtro de autorización:** el enunciado menciona `AuthorizationFilterAttribute` (Web API); en MVC 5 el equivalente es `FilterAttribute, IAuthorizationFilter`.

## 10. Estado de verificación
El código fue elaborado sin poder compilar ni ejecutar en el entorno de desarrollo del asistente. Debe compilarse en Visual Studio,
generar las migraciones y ejecutar las pruebas; los errores que aparezcan deben corregirse antes de grabar el video y entregar.
