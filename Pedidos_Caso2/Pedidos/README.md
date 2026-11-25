# Pedidos — Lección 11: Gestión segura de órdenes, despacho y rastreo
## 1. Estructura de la solución

| Proyecto | Responsabilidad | Referencias |
|---|---|---|
| `Pedidos.Domain` | Entidades `Orden`, `DetalleOrden`, enum `EstadoOrden`, reglas de transición | — |
| `Pedidos.Application` | Servicio, `Resultado`, contratos, requests, validadores, DTO, perfil AutoMapper | Domain |
| `Pedidos.Infrastructure` | `PedidosDbContext` (Identity), repositorio, migraciones, seed | Application, Domain |
| `Pedidos.Web` | Controladores, filtros, ViewModels/FormModels, vistas, Autofac, OWIN | Application, Domain, Infrastructure (solo para registrar Autofac) |
| `Pedidos.Application.Tests` | Pruebas unitarias (puntos extra) | Application, Domain |


