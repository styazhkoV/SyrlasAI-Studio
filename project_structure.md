# Структура решения

В корне репозитория находится основное решение `SyrlasStudio.sln`. Оно включает все проекты приложения:

| Проект | Путь | Назначение | Целевая платформа |
| --- | --- | --- | --- |
| SyrlasStudio | `SyrlasStudio/SyrlasStudio/` | UI на .NET MAUI | .NET 10 для Android, iOS и Mac Catalyst; Windows добавляется при сборке на Windows |
| SyrlasAIEngine | `SyrlasStudio/SyrlasAIEngine/` | AI-движок и общая логика | .NET 10 |
| SyrlasAI.McpServer | `SyrlasStudio/SyrlasAI.McpServer/` | MCP-сервер | .NET 10 |

Вложенное решение `SyrlasStudio/SyrlasStudio.sln` сохранено для совместимости и содержит те же проекты, что и корневое. Для обычной работы предпочтительно открывать корневое решение.

Каталоги `bin/` и `obj/` содержат генерируемые результаты сборки и игнорируются Git. Файлы платформенных манифестов и XML-ресурсы являются исходниками и не должны игнорироваться общими шаблонами.
