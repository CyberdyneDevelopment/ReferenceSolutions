# ManagementUI-Tailwind Feature Matrix

## Pages

| Page | Route | Description |
|------|-------|-------------|
| Dashboard | `/` | System overview, metrics, activity feed |
| Connections | `/connections` | Connection management with health testing |
| Connection Editor | `/connections/new`, `/connections/{name}/edit` | Create/edit connections |
| DataStores | `/datastores` | DataStore registry with path management |
| DataStore Editor | `/datastores/new`, `/datastores/{name}/edit` | Create/edit DataStores |
| DataStore Detail | `/datastores/{name}` | DataStore overview, paths, fields |
| DataSets | `/datasets` | DataSet management |
| DataSet Detail | `/datasets/{name}` | Fields, sources, lineage, preview |
| Calculations | `/calculations` | Calculation definitions |
| Calculation Editor | `/calculations/new`, `/calculations/{id}/edit` | Formula editor with validation |
| Pipelines | `/pipelines` | Pipeline management |
| Pipeline Builder | `/pipelines/new`, `/pipelines/{id}/edit` | Visual ETL pipeline editor |
| Schedules | `/schedules` | Schedule management |
| Lineage | `/lineage` | Cross-system data lineage visualization |
| Dataflow | `/dataflow` | Real-time dataflow monitoring |
| Field Mapper | `/mapper` | Source-to-target field mapping |
| Data Preview | `/data-preview` | Live data preview with schema browsing |
| Schema Explorer | `/schema` | Interactive schema visualization |
| Configuration | `/configuration` | Configuration instance management |
| Audit | `/audit` | Audit log viewer |
| Settings | `/settings` | General, notifications, security settings |
| Appearance | `/settings/appearance` | Theme management |
| Login | `/login` | JWT authentication |

## Key Features

- **Visual Pipeline Builder:** Drag-and-drop ETL pipeline construction
- **Data Lineage:** Cross-system lineage with interactive SVG graph
- **Schema Explorer:** Database schema visualization
- **Calculation Editor:** Formula editor with live validation and preview
- **Field Mapper:** Source-to-target field mapping with auto-map
- **Theme Management:** Customizable UI themes
