<<<<<<< HEAD
yrlasAI-Studio: Полная техническая спецификация и архитектура (Обновленная версия)

PowerShell

dotnet publish "X:\\SyrlasStudio\\SyrlasStudio\\SyrlasStudio\\SyrlasStudio.csproj" -c Release -f net9.0-windows10.0.19041.0 -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true  
 

1\. Архитектура UI, Инсталлятора и Сборки

Приложение построено как автономная десктопная среда для локального взаимодействия с LLM в едином исполняемом файле (Single-File), что обеспечивается флагом `-p:PublishSingleFile=true` и безопасной распаковкой нативных библиотек (`-p:IncludeNativeLibrariesForSelfExtract=true`), критически важных для работы LLamaSharp и CUDA-бэкендов.

**Платформа:** .NET MAUI (WinUI 3) с нативным рендерингом под Windows x64 (`net9.0-windows10.0.19041.0`), исключающий накладные расходы на сторонние веб-мосты.

**Дизайн-система (UI/UX):** Полная репликация канонического интерфейса VS Code (Dark+ Theme):

**Activity Bar:** Узкая левая панель управления режимами.

**Sidebar:** Панель навигации по сессиям, файлам и системным логам.

**Editor/Chat Area:** Область работы с вкладками, чатом, блоками кода и контекстным вводом.

**Status Bar:** Нижняя статусная строка с системными метриками (CPU, RAM) и состоянием окружения.

**Инсталляция и дистрибуция:** Сборка упаковывает .NET рантайм, нативные DLL/SO компоненты инференса и пользовательский интерфейс в единый оптимизированный бинарник.  
 

2\. Реализация мультиагентности и 4-этапный конвейер

Держать несколько независимых тяжелых LLM-моделей одновременно в памяти локального ПК неэффективно. Архитектура использует контекстное мультиплексирование: одна квантованная базовая модель (например, Qwen 2.5) на лету переключает профили генерации (System Prompt, Temperature, Top-P) с помощью Agent State Manager.  
 

Последовательный конвейер разработки (Pipeline)
=======
yrlasAI-Studio: Полная техническая спецификация и архитектура
dotnet publish "C:\Users\alexs\SyrlasStudio\SyrlasStudio\SyrlasStudio\SyrlasStudio.csproj" -c Release -f net9.0-windows10.0.19041.0 -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
1. Архитектура UI, Инсталлятора и Сборки
Приложение построено как автономная десктопная среда для локального взаимодействия с LLM в едином исполняемом файле (Single-File), что обеспечивается флагом -p:PublishSingleFile=true и безопасной распаковкой нативных библиотек (-p:IncludeNativeLibrariesForSelfExtract=true), критически важных для работы LLamaSharp и CUDA-бэкендов.

Платформа: .NET MAUI (WinUI 3) с нативным рендерингом под Windows x64 (net9.0-windows10.0.19041.0), что исключает накладные расходы на сторонние веб-мосты.

Дизайн-система (UI/UX): Полная репликация канонического интерфейса VS Code (Dark+ Theme):

Activity Bar: Узкая левая панель управления режимами.

Sidebar: Панель навигации по сессиям, файлам и системным логам.

Editor/Chat Area: Область работы с вкладками, чатом, блоками кода и контекстным вводом.

Status Bar: Нижняя статусная строка с системными метриками (CPU, RAM) и состоянием окружения.

Инсталляция и дистрибуция: Сборка упаковывает .NET рантайм, нативные DLL/SO компоненты инференса и пользовательский интерфейс в единый оптимизированный бинарник.

2. Реализация мультиагентности и 4-этапный конвейер
Держать несколько независимых тяжелых LLM-моделей одновременно в памяти локального ПК неэффективно. Архитектура использует контекстное мультиплексирование: одна квантованная базовая модель (например, Qwen 2.5 Coder) на лету переключает профили генерации (System Prompt, Temperature, Top-P) с помощью Agent State Manager.
>>>>>>> 91f963cdcef4d64ea3c54043828f2eba0507b4ee

Последовательный конвейер разработки (Pipeline)
Каждый агент выполняет строго свою роль, передавая результаты следующему этапу в виде неизменяемых артефактов:

<<<<<<< HEAD
\[Пользователь\]  
      │  
      ▼  
┌─────────────┐     Артефакт: User Stories & BPMN  
│  1. BA Agent │ ──────────────────────────────────────┐  
└─────────────┘                                       │  
                                                      ▼  
┌─────────────┐     Артефакт: OpenAPI & DB Schema    ┌─────────────┐  
│  2. SA Agent │ ───────────────────────────────────►│  3. Arch    │  
└─────────────┘                                      │  Agent      │  
                                                     └─────────────┘  
                                                            │ Артефакт: Class Diagrams  
                                                            │ & Solution Structure  
                                                            ▼  
                                                     ┌─────────────┐  
                                                     │ 4. Coder    │ ──► \[Готовый C# код\]  
                                                     │    Agent    │  
                                                     └─────────────┘  
 

**Бизнес-аналитик (БА / Business Analyst):**

**Задача:** Интервьюирование пользователя, сбор бизнес-требований, поиск рисков, формирование User Stories.

**Параметры:** Температура 0.6 (высокая эвристика), Top-P: 0.90.

**Системный аналитик (СА / System Analyst)**

**Задача:** Перевод бизнес-требований в технические спецификации, проектирование схем реляционных БД и OpenAPI 3.0 контрактов.

**Параметры:** Температура 0.2 (строгая логика), Top-P: 0.85.

**Архитектор (Architect):**

**Задача:** Проектирование Clean Architecture / CQRS, определение структуры классов и интерфейсов для .NET.

**Параметры:** Температура 0.1 (минимум отклонений), Top-P: 0.90.  
 

**Кодер (Coder):**

**Задача:** Написание чистого, компилируемого кода (C#, React) строго по утвержденным спецификациям.

**Параметры:** Температура 0.0 (нулевая креативность), Top-P: 0.95.  
 

3\. Спецификация API и расширения бэкенда

Для поддержки многоуровневой логики бэкенд дополнен эндпоинтами управления состоянием сессий, передачи артефактов и интеграции ИИ-агентов с локальным терминалом разработки:

  
 

| **Модуль** | **Метод** | **Endpoint** | **Описание** |
| --- | --- | --- | --- |
| **Agent** | GET | `/api/agent/roles` | Получение списка доступных ролей (BA, SA, Architect, Coder) и их статусов. |
| **Agent** | POST | `/api/agent/switch` | Переключение активного профиля генерации и системного промпта. |
| **Workspace** | POST | `/api/workspace/artifact` | Сохранение утвержденного этапа (ТЗ, схемы, контракты) как контекста для следующего агента. |
| **Terminal** | POST | `/api/terminal/execute` | Запуск контролируемых команд (сборка, тесты, миграции) через интеграцию с консолью. |

4\. Интеграция с Cursor, терминалом и автоматизация тестирования

**Интеграция с IDE Cursor:** Настройка среды на работу с локальным эндпоинтом бэкенда (`[http://127.0.0.1:8080/v1](http://127.0.0.1:8080/v1)`) в качестве альтернативы облачным провайдерам, задействуя Composer/Agent Mode для многофайлового редактирования кода на C# и React.

**Написание и тестирование кода:** Применение специализированных моделей для автодополнения (FIM) и автоматическая генерация юнит-тестов (xUnit/NUnit) при реализации бизнес-фич.

**Доступ к терминалу для ИИ-агента:** Предоставление ИИ-агенту через интеграцию с терминалом прав на выполнение регламентных команд (`dotnet build`, `dotnet test`), автоматический перехват ошибок компиляции (например, `MSB4025`, конфликты NuGet вроде `NU1504`) и генерацию патчей исправлений.

5\. Модуль RAG и умный чанкинг документов (Smart Chunking)

Обычное разбиение текста по фиксированному числу символов разрушает логику кода и таблиц. Реализована стратегия дифференцированного парсинга:

**Исходный код (.cs, .java, .js, .ts, .py, .sql):** Синтаксический анализ для разделения кода на логические блоки (классы, интерфейсы, методы) с сохранением метаданных путей файлов и зависимостей.

**Документы MS Office и PDF (.docx, .xlsx, .pdf):**

_Word (.docx):_ Чанкинг по абзацам и заголовкам через `DocumentFormat.OpenXml`.

_Excel (.xlsx):_ Автоматическая конвертация таблиц в читаемый для LLM формат Markdown перед векторизацией.

_PDF:_ Извлечение текста с сохранением структуры страниц с использованием библиотеки `PdfPig`.

6\. Промышленная схема реляционной базы данных (SQLite)

База данных оптимизирована под режим WAL (Write-Ahead Logging) и внешние ключи, обеспечивая одновременную работу UI, FTS5-поиска и векторного хранилища.

```
SQL

=======
[Пользователь]
      │
      ▼
┌─────────────┐     Артефакт: User Stories & BPMN
│  1. BA Agent │ ──────────────────────────────────────┐
└─────────────┘                                       │
                                                      ▼
┌─────────────┐     Артефакт: OpenAPI & DB Schema    ┌─────────────┐
│  2. SA Agent │ ───────────────────────────────────►│  3. Arch    │
└─────────────┘                                      │  Agent      │
                                                     └─────────────┘
                                                            │ Артефакт: Class Diagrams
                                                            │ & Solution Structure
                                                            ▼
                                                     ┌─────────────┐
                                                     │ 4. Coder    │ ──► [Готовый C# код]
                                                     │    Agent    │
                                                     └─────────────┘

Бизнес-аналитик (БА / Business Analyst):
Задача: Интервьюирование пользователя, сбор бизнес-требований, поиск рисков, формирование User Stories.
Параметры: Температура 0.6 (высокая эвристика), Top-P: 0.90.
Системный аналитик (СА / System Analyst):
Задача: Перевод бизнес-требований в технические спецификации, проектирование схем реляционных БД и OpenAPI 3.0 контрактов.
Параметры: Температура 0.2 (строгая логика), Top-P: 0.85.
Архитектор (Architect):
Задача: Проектирование Clean Architecture / CQRS, определение структуры классов и интерфейсов для .NET.
Параметры: Температура 0.1 (минимум отклонений), Top-P: 0.90.
Кодер (Coder):
Задача: Написание чистого, компилируемого кода (C#, TypeScript/React) строго по утвержденным спецификациям.
Параметры: Температура 0.0 (нулевая креативность), Top-P: 0.95.
3. Спецификация API и расширения бэкенда
Для поддержки многоуровневой логики бэкенд дополнен эндпоинтами управления состоянием сессий и передачи артефактов:

Модуль	Метод	Endpoint	Описание
Agent	GET	/api/agent/roles	Получение списка доступных ролей (BA, SA, Architect, Coder) и их статусов.
Agent	POST	/api/agent/switch	Переключение активного профиля генерации и системного промпта.
Workspace	POST	/api/workspace/artifact	Сохранение утвержденного этапа (ТЗ, схемы, контракты) как контекста для следующего агента.
4. Модуль RAG и умный чанкинг документов (Smart Chunking)
Обычное разбиение текста по фиксированному числу символов разрушает логику кода и таблиц. Реализована стратегия дифференцированного парсинга:

Исходный код (.cs, .java, .js, .ts, .py, .sql): Применяется синтаксический анализ (или регулярные выражения) для разделения кода на логические блоки (классы, интерфейсы, методы) с сохранением метаданных путей файлов и зависимостей.
Документы MS Office и PDF (.docx, .xlsx, .pdf):
Word (.docx): Чанкинг по абзацам и заголовкам через DocumentFormat.OpenXml.
Excel (.xlsx): Автоматическая конвертация таблиц в читаемый для LLM формат Markdown перед векторизацией.
PDF: Извлечение текста с сохранением структуры страниц с использованием библиотеки PdfPig.
5. Промышленная схема реляционной базы данных (SQLite)
База данных оптимизирована под режим WAL (Write-Ahead Logging) и внешние ключи, обеспечивая одновременную работу UI, FTS5-поиска и векторного хранилища.

>>>>>>> 91f963cdcef4d64ea3c54043828f2eba0507b4ee
-- Включение поддержки внешних ключей и режима WAL
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;

-- 1. СЕССИИ И РАБОЧИЕ ОБЛАСТИ
CREATE TABLE IF NOT EXISTS sessions (
    id TEXT PRIMARY KEY,
    title TEXT NOT NULL,
    active_agent_role TEXT NOT NULL CHECK (active_agent_role IN ('BA', 'SA', 'ARCHITECT', 'CODER')),
    system_prompt_override TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

-- 2. ИСТОРИЯ СООБЩЕНИЙ ЧАТА
CREATE TABLE IF NOT EXISTS messages (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    sender TEXT NOT NULL CHECK (sender IN ('user', 'assistant', 'system')),
    agent_role TEXT NOT NULL CHECK (agent_role IN ('BA', 'SA', 'ARCHITECT', 'CODER')),
    content TEXT NOT NULL,
    prompt_tokens INTEGER DEFAULT 0,
    completion_tokens INTEGER DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE
);

-- 3. РЕПОЗИТОРИЙ АРТЕФАКТОВ И ФАЙЛОВ
CREATE TABLE IF NOT EXISTS artifacts (
    id TEXT PRIMARY KEY,
    session_id TEXT,
    file_name TEXT NOT NULL,
    file_path TEXT NOT NULL,
    file_extension TEXT NOT NULL,
    file_type TEXT NOT NULL CHECK (file_type IN ('CODE', 'OFFICE_DOC', 'EXCEL', 'PDF', 'SPECIFICATION')),
    file_hash TEXT NOT NULL,
    file_size_bytes INTEGER NOT NULL,
    status TEXT NOT NULL CHECK (status IN ('PENDING', 'INDEXED', 'FAILED')) DEFAULT 'PENDING',
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE SET NULL
);

-- 4. ВЕКТОРНЫЕ И ТЕКСТОВЫЕ ЧАНКИ ДЛЯ RAG
CREATE TABLE IF NOT EXISTS document_chunks (
    id TEXT PRIMARY KEY,
    artifact_id TEXT NOT NULL,
    chunk_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    metadata_json TEXT,
    embedding BLOB,
    token_count INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (artifact_id) REFERENCES artifacts(id) ON DELETE CASCADE
);

-- 5. ПОЛНОТЕКСТОВЫЙ ИНДЕКС (SQLite FTS5)
CREATE VIRTUAL TABLE IF NOT EXISTS document_chunks_fts USING fts5(
    chunk_id UNINDEXED,
    content,
    metadata_json,
    content='document_chunks',
    content_rowid='rowid'
);

-- 6. ПАЙПЛАЙН КОНТЕКСТА МЕЖДУ АГЕНТАМИ
CREATE TABLE IF NOT EXISTS agent_contexts (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    stage TEXT NOT NULL CHECK (stage IN ('BUSINESS_REQUIREMENTS', 'TECHNICAL_SPEC', 'ARCHITECTURE')),
    source_agent_role TEXT NOT NULL,
    artifact_id TEXT,
    summary_content TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1 CHECK (is_active IN (0, 1)),
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE,
    FOREIGN KEY (artifact_id) REFERENCES artifacts(id) ON DELETE SET NULL
);

-- 7. ИНДЕКСЫ ДЛЯ УСКОРЕНИЯ ВЫБОРОК
CREATE INDEX IF NOT EXISTS idx_messages_session ON messages(session_id, created_at);
CREATE INDEX IF NOT EXISTS idx_artifacts_hash ON artifacts(file_hash);
CREATE INDEX IF NOT EXISTS idx_artifacts_session ON artifacts(session_id);
CREATE INDEX IF NOT EXISTS idx_chunks_artifact ON document_chunks(artifact_id);
CREATE INDEX IF NOT EXISTS idx_agent_contexts_session ON agent_contexts(session_id, stage, is_active);
<<<<<<< HEAD
```

7\. Пример реализации Оркестратора агентов на C# (.NET 9 / LLamaSharp)

```
=======
6. Пример реализации Оркестратора агентов на C#
>>>>>>> 91f963cdcef4d64ea3c54043828f2eba0507b4ee
using LLama;
using LLama.Common;

namespace Syrlas.AI.Engine.Services;

public enum AgentRole { BusinessAnalyst, SystemAnalyst, Architect, Coder }

public class AgentOrchestrator
{
    private readonly LLamaWeights _model;
    private readonly ModelParams _modelParams;

    public AgentOrchestrator(string modelPath)
    {
        _modelParams = new ModelParams(modelPath)
        {
            ContextSize = 4096,
            GpuLayerCount = 33 // Оптимально под NVIDIA GTX 1070
        };
        _model = LLamaWeights.LoadFromFile(_modelParams);
    }

    public async IAsyncEnumerable<string> ExecuteAgentStageAsync(
        AgentRole role, 
        string userInput, 
        string previousContext)
    {
        var (systemPrompt, inferenceParams) = GetAgentProfile(role, previousContext);
        
        using var context = _model.CreateContext(_modelParams);
        var executor = new InteractiveExecutor(context);
        var session = new ChatSession(executor);

        session.History.AddMessage(AuthorRole.System, systemPrompt);

        await foreach (var token in session.ChatAsync(
            new ChatHistory.Message(AuthorRole.User, userInput), 
            inferenceParams))
        {
            yield return token;
        }
    }

    private (string SystemPrompt, InferenceParams Params) GetAgentProfile(AgentRole role, string previousContext)
    {
        return role switch
        {
            AgentRole.BusinessAnalyst => (
                "Ты — Senior Business Analyst. Твоя задача — извлечь бизнес-требования, выявить риски и описать User Stories.\n" +
                $"Контекст проекта:\n{previousContext}",
                new InferenceParams { Temperature = 0.6f, TopP = 0.9f, AntiPrompts = new[] { "User:" } }
            ),

            AgentRole.SystemAnalyst => (
                "Ты — Senior System Analyst. Переведи бизнес-требования в OpenAPI 3.0 контракты и схемы БД.\n" +
                $"Утвержденные бизнес-требования:\n{previousContext}",
                new InferenceParams { Temperature = 0.2f, TopP = 0.85f, AntiPrompts = new[] { "User:" } }
            ),

            AgentRole.Architect => (
                "Ты — Principal .NET Architect. Спроектируй C# интерфейсы, CQRS команды и структуру классов в .NET 9.\n" +
                $"Техническое задание:\n{previousContext}",
                new InferenceParams { Temperature = 0.1f, TopP = 0.9f, AntiPrompts = new[] { "User:" } }
            ),

            AgentRole.Coder => (
                "Ты — Senior C# Developer (.NET 9). Пиши чистый, компилируемый код без лишних пояснений. " +
                "Используй C# 12/13, pattern matching и первичные конструкторы.\n" +
                $"Архитектурный план и контракты:\n{previousContext}",
                new InferenceParams { Temperature = 0.0f, TopP = 0.95f, AntiPrompts = new[] { "User:" } }
            ),

            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
    }
}
<<<<<<< HEAD
```
=======
>>>>>>> 91f963cdcef4d64ea3c54043828f2eba0507b4ee
