# SyrlasAI-Studio
dotnet publish "C:\Users\alexs\SyrlasStudio\SyrlasStudio\SyrlasStudio\SyrlasStudio.csproj" -c Release -f net9.0-windows10.0.19041.0 --self-contained true
## 1\. Архитектура UI и Инсталлятора (Аналог VS Code)

Поскольку вы ориентируетесь на опыт VS Code, идеальным стеком для фронтенда будет связка **Electron + React**. Это позволит завернуть ваш веб-интерфейс в нативное окно и запускать .NET 8 бэкенд фоновым процессом прямо при старте приложения.

**Оконный интерфейс (Electron):** Будет управлять жизненным циклом (запуск Kestrel на `127.0.0.1:5000` при открытии приложения и его корректное завершение при закрытии).

**Интерфейс (React + Tailwind):** Реализует рабочие области: панель файлов (как в IDE), окно чата с агентами, панель просмотра сгенерированного кода и графов (UML/BPMN).

**Инсталлятор:** Используем `electron-builder` (или NSIS). Он соберет единый `.exe` или `.dmg` файл, который при установке распакует и React-фронтенд, и скомпилированный бинарник .NET 8 бэкенда, и, при необходимости, сами GGUF-модели (если они не скачиваются отдельно).

## 2\. Реализация мультиагентности (БА, СА и Кодер)

Держать три разные LLM-модели (например, по 7-14B параметров) одновременно загруженными в VRAM/RAM локальной машины — очень ресурсоемкая задача. Чтобы система работала быстро и стабильно, я предлагаю два пути:

**Путь А: Контекстное мультиплексирование (Рекомендуемый)**

Используется одна мощная модель (например, _Qwen 2.5 14B/32B Coder_), но бэкенд на лету подменяет `System Prompt` и параметры генерации (Temperature, Top-P) в зависимости от того, какой агент сейчас активен.

**Путь Б: Горячая подмена GGUF-моделей**

Если для бизнес-анализа нужна специфическая текстовая модель, а для кода — строго Coder-модель, мы добавляем в LLM Inference Engine механизм выгрузки текущей модели из памяти и загрузки новой через `LLamaSharp` при переключении вкладки агента.

### Как агенты будут общаться между собой

Чтобы кодер писал код на основании "полного и непротиворечивого ТЗ", нам нужен **Пайплайн состояний**:

**Бизнес-аналитик (БА):** Собирает сырые требования пользователя, задает уточняющие вопросы, формирует бизнес-процессы (может генерировать код для PlantUML). Сохраняет результат как артефакт (документ в SQLite).

**Системный аналитик (СА):** Через RAG подтягивает артефакт БА. Формирует архитектуру, API-контракты, структуру БД. Сохраняет ТЗ.

**Кодер:** В его системный промпт жестко инжектируется ТЗ от СА. Модель получает команду писать код строго по спецификации, без фантазий.

## 3\. Необходимые дополнения в ваше ТЗ

Чтобы бэкенд поддерживал эту логику, нужно внести несколько дополнений в раздел спецификации API:

### Новые Endpoints

| **Модуль** | **Метод** | **Endpoint** | **Описание** |
| --- | --- | --- | --- |
| **Agent** | `GET` | `/api/agent/roles` | Получение списка доступных агентов (BA, SA, Coder) и их статусов. |
| **Agent** | `POST` | `/api/agent/switch` | Переключение активной роли/модели (если используется горячая подмена GGUF). |
| **Workspace** | `POST` | `/api/workspace/artifact` | Сохранение утвержденного этапа (например, ТЗ от СА) как контекста для следующего агента. |

### Обновление модуля 2.3 (LLM Inference Engine)

**Динамический System Prompt:** Внедрение фабрики промптов (Prompt Factory), которая конструирует контекст в зависимости от `AgentRole` (БА, СА, Кодер).

**Изоляция контекста сессий:** В базе данных SQLite (в таблице сессий) необходимо добавить поле `AgentRole`, чтобы диалог с Кодером не смешивался с брейнштормом БА.

## 1\. Архитектура контекстного мультиплексирования

Вместо загрузки разных моделей, мы создадим **Agent State Manager**. Перед каждым обращением к `LLamaSharp` бэкенд будет применять специфичный профиль генерации.

| **Агент** | **Задача** | **Температура** | **Top-P** | **Особенности System Prompt** |
| --- | --- | --- | --- | --- |
| **Бизнес-аналитик** | Интервьюирование, сбор требований, поиск болей | 0.7 (Выше) | 0.90 | Разрешена эвристика. Промпт требует задавать уточняющие вопросы и формировать User Stories. |
| **Системный аналитик** | Проектирование БД (PostgreSQL), API контрактов, BPMN/UML | 0.3 (Ниже) | 0.85 | Фокус на строгой логике. Промпт требует переводить User Stories в технические спецификации и таблицы. |
| **Кодер** | Написание кода (C#, Java, React) строго по ТЗ | 0.1 (Минимум) | 0.90 | Нулевая креативность. Промпт жестко ограничивает отклонения от переданного ТЗ Системного аналитика. |

## 2\. Универсальный парсер артефактов (Код + MS Office)

Чтобы модель могла "анализировать и давать объяснение" загруженным файлам, нам нужно прокачать модуль `2.2. Модуль RAG & Document Processing`. Обычное разбиение текста на куски (чанки) по количеству символов убьет структуру кода.

Нам понадобится **Умный Чанкинг (Smart Chunking)** в зависимости от расширения файла:

### А. Парсинг исходного кода (.cs, .java, .js, .ts, .py, .sql)

При загрузке файла с кодом, RAG-модуль не должен резать его посередине функции.

**Реализация:** Использование регулярных выражений или легковесных AST-парсеров (например, Tree-sitter) для разбиения кода на логические блоки: класс, метод, интерфейс.

**Метаданные вектора:** В базу SQLite (FTS5/Vector) вместе с чанком кода сохраняется его путь (например, `src/services/AuthService.cs`) и зависимости, чтобы кодер понимал контекст проекта.

### Б. Парсинг MS Office и PDF (.docx, .xlsx, .pdf)

**Word (.docx):** Парсинг через библиотеку `DocumentFormat.OpenXml` (нативная для .NET). Текст бьется на чанки по абзацам или заголовкам.

**Excel (.xlsx):** Модели плохо понимают сырые таблицы. При парсинге `.xlsx` RAG-модуль будет конвертировать таблицы в формат Markdown или CSV перед векторизацией. Это позволит агенту-аналитику легко читать финансовые или структурные данные.

**PDF:** Использование `PdfPig` (отличная .NET библиотека) для извлечения текста с сохранением структуры страниц.

## 3\. Обновление пайплайна в ТЗ (Модуль RAG)

Дополняем вашу архитектуру следующими механизмами обработки загрузок:

C#

```
// Примерная структура интерфейса фабрики парсеров
public interface IDocumentParser
{
    bool SupportsExtension(string extension);
    Task<IEnumerable<DocumentChunk>> ParseAsync(Stream fileStream, string fileName);
}

// Имплементации, которые инжектятся через DI в ASP.NET Core:
// - CodeFileParser (.cs, .ts, .java - чанкинг по методам)
// - OpenXmlWordParser (.docx - чанкинг по параграфам)
// - OpenXmlExcelParser (.xlsx - конвертация таблиц в Markdown)
```

**Процесс работы агента с артефактом:**

Вы перетаскиваете файл `LegacyController.java` или `BusinessReqs.docx` в окно приложения.

Фронтенд (React) отправляет файл через `POST /api/rag/upload`.

Бэкенд (.NET 8) определяет парсер по расширению, нарезает файл на смысловые куски и сохраняет их в SQLite.

Вы пишете в чат: _"Объясни, как работает аутентификация в этом контроллере"_.

Векторный поиск находит нужный кусок кода/документа, подмешивает его в System Prompt и отправляет квантованной Qwen 2.5. Модель выдает точный ответ.

Проектирование схемы SQLite для десктопного ИИ-приложения требует баланса между **быстрой реляционной выборкой (для UI)**, **полнотекстовым поиском (FTS5)** и **векторным RAG (Embeddings)**.

Ниже представлена готовая промышленная схема реляционной базы данных для SQLite, оптимизированная под режим `WAL` (Write-Ahead Logging). Она гарантирует полную сохранность истории чатов, метаданных парсинга файлов и переданного контекста между агентами (БА -> СА -> Кодер) даже при неожиданном завершении работы приложения.

### Архитектура связей (ER-Диаграмма)

```
[sessions] ───┬───< [messages] (История диалога с конкретным агентом)
              ├───< [artifacts] (Загруженные файлы и созданные ТЗ)
              │         └───< [document_chunks] (Чанки кода/документов)
              │                   └─── [document_chunks_fts] (FTS5 Поиск)
              └───< [agent_contexts] (Утвержденные артефакты для передачи между агентами)
```

### DDL Скрипт создания таблиц (SQLite SQL)

SQL

```
-- Включение поддержки внешних ключей и режима WAL для высокой производительности
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;

-- ---------------------------------------------------------------------
-- 1. СЕССИИ И РАБОЧИЕ ОБЛАСТИ (Sessions)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS sessions (
    id TEXT PRIMARY KEY,                       -- UUID v4
    title TEXT NOT NULL,                       -- Название сессии / проекта
    active_agent_role TEXT NOT NULL CHECK (active_agent_role IN ('BA', 'SA', 'CODER')),
    system_prompt_override TEXT,               -- Кастомный промпт (если пользователь менял настройки)
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

-- ---------------------------------------------------------------------
-- 2. ИСТОРИЯ СООБЩЕНИЙ ЧАТА (Messages)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS messages (
    id TEXT PRIMARY KEY,                       -- UUID v4
    session_id TEXT NOT NULL,                  -- Ссылка на сессию
    sender TEXT NOT NULL CHECK (sender IN ('user', 'assistant', 'system')),
    agent_role TEXT NOT NULL CHECK (agent_role IN ('BA', 'SA', 'CODER')),
    content TEXT NOT NULL,                     -- Текст сообщения
    prompt_tokens INTEGER DEFAULT 0,           -- Затрачено входных токенов
    completion_tokens INTEGER DEFAULT 0,       -- Затрачено сгенерированных токенов
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE
);

-- ---------------------------------------------------------------------
-- 3. РЕПОЗИТОРИЙ АРТЕФАКТОВ И ФАЙЛОВ (Artifacts)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS artifacts (
    id TEXT PRIMARY KEY,                       -- UUID v4
    session_id TEXT,                           -- Сессия, к которой привязан файл (может быть NULL для глобальных)
    file_name TEXT NOT NULL,                   -- Оригинальное имя файла (e.g. LegacyService.cs)
    file_path TEXT NOT NULL,                   -- Локальный путь на диске
    file_extension TEXT NOT NULL,              -- Расширение (.cs, .docx, .xlsx, .pdf)
    file_type TEXT NOT NULL CHECK (file_type IN ('CODE', 'OFFICE_DOC', 'EXCEL', 'PDF', 'SPECIFICATION')),
    file_hash TEXT NOT NULL,                   -- SHA-256 для дедупликации и кеширования
    file_size_bytes INTEGER NOT NULL,
    status TEXT NOT NULL CHECK (status IN ('PENDING', 'INDEXED', 'FAILED')) DEFAULT 'PENDING',
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE SET NULL
);

-- ---------------------------------------------------------------------
-- 4. ВЕКТОРНЫЕ И ТЕКСТОВЫЕ ЧАНКИ ДЛЯ RAG (Document Chunks)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS document_chunks (
    id TEXT PRIMARY KEY,                       -- UUID v4
    artifact_id TEXT NOT NULL,                 -- Ссылка на исходный файл
    chunk_index INTEGER NOT NULL,              -- Порядковый номер чанка в файле
    content TEXT NOT NULL,                     -- Извлеченный фрагмент текста/кода
    metadata_json TEXT,                        -- JSON-метаданные: {"start_line": 15, "end_line": 60, "class": "UserService"}
    embedding BLOB,                            -- Сериализованный вектор (float[1536] или float[4096] в binary)
    token_count INTEGER NOT NULL DEFAULT 0,    -- Размер чанка в токенах
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (artifact_id) REFERENCES artifacts(id) ON DELETE CASCADE
);

-- ---------------------------------------------------------------------
-- 5. ПОЛНОТЕКСТОВЫЙ ИНДЕКС СЕMАНТИЧЕСКОГО ПОИСКА (SQLite FTS5)
-- ---------------------------------------------------------------------
CREATE VIRTUAL TABLE IF NOT EXISTS document_chunks_fts USING fts5(
    chunk_id UNINDEXED,                        -- Ссылка на ID из document_chunks
    content,                                   -- Текст для полнотекстового поиска
    metadata_json,                             -- Классы, пути, заголовки
    content='document_chunks',                 -- Внешний источник данных
    content_rowid='rowid'
);

-- ---------------------------------------------------------------------
-- 6. ПАЙПЛАЙН КОНТЕКСТА МЕЖДУ АГЕНТАМИ (Agent Contexts / Stage Artifacts)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS agent_contexts (
    id TEXT PRIMARY KEY,                       -- UUID v4
    session_id TEXT NOT NULL,                  -- Ссылка на сессию
    stage TEXT NOT NULL CHECK (stage IN ('BUSINESS_REQUIREMENTS', 'TECHNICAL_SPEC', 'ARCHITECTURE')),
    source_agent_role TEXT NOT NULL,           -- Кто создал (BA или SA)
    artifact_id TEXT,                          -- Ссылка на сгенерированный файл-документ (если есть)
    summary_content TEXT NOT NULL,             -- Чистый текст утвержденного ТЗ/требований
    is_active INTEGER NOT NULL DEFAULT 1 CHECK (is_active IN (0, 1)), -- Инжектировать ли в System Prompt
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE,
    FOREIGN KEY (artifact_id) REFERENCES artifacts(id) ON DELETE SET NULL
);

-- ---------------------------------------------------------------------
-- 7. ИНДЕКСЫ ДЛЯ УСКОРЕНИЯ ВЫБОРКИ
-- ---------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_messages_session ON messages(session_id, created_at);
CREATE INDEX IF NOT EXISTS idx_artifacts_hash ON artifacts(file_hash);
CREATE INDEX IF NOT EXISTS idx_artifacts_session ON artifacts(session_id);
CREATE INDEX IF NOT EXISTS idx_chunks_artifact ON document_chunks(artifact_id);
CREATE INDEX IF NOT EXISTS idx_agent_contexts_session ON agent_contexts(session_id, stage, is_active);
```

### Ключевые особенности архитектуры этой БД

**Изоляция сессий и мультиагентность (**`**sessions**` **&** `**messages**`**):**

Поле `agent_role` фиксирует, с кем именно ведется диалог.

При переключении в UI на вкладыш **«Кодер»**, UI запрашивает историю `SELECT * FROM messages WHERE session_id = @id AND agent_role = 'CODER'`.

**Память и сквозной контекст (**`**agent_contexts**`**):**

Когда Бизнес-аналитик формирует требования, они записываются в `agent_contexts` со статусом `stage = 'BUSINESS_REQUIREMENTS'`.

При обращении к Системному аналитику бэкенд вычитывает записи из `agent_contexts` со статусом `is_active = 1` и **автоматически внедряет их в System Prompt**, избавляя пользователя от необходимости копировать текст вручную.

**Умный RAG и метаданные (**`**document_chunks**`**):**

Поле `metadata_json` позволяет хранить структуру конкретного файла. Для C#/Java кода там будут сохранены `{ "namespace": "...", "class": "...", "methods": ["Init", "Execute"] }`, а для Word/Excel — `{ "page": 3, "sheet_name": "Финансы" }`.

**Гибридный поиск (Hybrid Search):**

Поле `embedding BLOB` сохраняет векторные эмбеддинги (массив `float[]`, сконвертированный в массив байтов).

Таблица `document_chunks_fts` предоставляет быстрое ключевое совпадение по стандарту FTS5 (BM25). Вы сможете объединять результаты точного поиска по ключевым словам и векторов (Reciprocal Rank Fusion).

## Концепция Syrlas AI: 4-этапный конвейер разработки

Раз мы строим единого ИИ-инженера для всего цикла (от бизнес-требований до готовых C#-модулей), система должна работать как **последовательный конвейер (Pipeline)**. Каждый предыдущий шаг генерирует жесткий артефакт, который становится неизменяемым контекстом для следующего.

```
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
```

### Архитектура модулей Syrlas AI в .NET 8

### 1\. Модуль Бизнес-Анализа (BA)

**Запромптован под:** Поиск противоречий, сбор требований, формирование User Stories и критериев приемки (Acceptance Criteria).

**Выходной артефакт:** Документ `REQUIREMENTS.md` или описание бизнес-процесса в нотации PlantUML / BPMN.

### 2\. Модуль Системного Анализа (SA)

**Запромптован под:** Строгий транслятор бизнес-требований в технические термины.

**Выходной артефакт:** OpenAPI (Swagger) спецификации, структуры JSON-ответов, DDL-скрипты SQLite/PostgreSQL, Use Case диаграммы.

### 3\. Модуль Архитектора (Architect)

**Запромптован под:** Проектирование Clean Architecture / CQRS / Vertical Slice Architecture для .NET 8.

**Выходной артефакт:** Структура проекта (интерфейсы, сервисы, репозитории), правила обработки ошибок, DTO-картирование.

### 4\. Модуль Кодера (Coder)

**Запромптован под:** Написание чистого, компилируемого C# 12 / .NET 8 кода.

**Особенность:** Ему запрещено "фантазировать" — он пишет код строго по C# интерфейсам и OpenAPI спецификации, переданной от Архитектора и Системного аналитика.

## Пример реализации Оркестратора агентов на C# (.NET 8)

Ниже пример C#-сервиса, который управляет переключением профилей генерации и автоматическим пробросом артефактов между агентами через `LLamaSharp`:

C#

```
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
            ContextSize = 8192, // Расширенный контекст для длинных ТЗ
            GpuLayerCount = 35  // Загрузка слоев на GPU (если доступен)
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

        // Внедрение системного промпта с историей контекста
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
                "Ты — Principal .NET Architect. Спроектируй C# интерфейсы, CQRS команды и структуру классов в .NET 8.\n" +
                $"Техническое задание:\n{previousContext}",
                new InferenceParams { Temperature = 0.1f, TopP = 0.9f, AntiPrompts = new[] { "User:" } }
            ),

            AgentRole.Coder => (
                "Ты — Senior C# Developer (.NET 8). Пиши чистый, компилируемый код без лишних пояснений. " +
                "Используй C# 12, pattern matching и первичные конструкторы.\n" +
                $"Архитектурный план и контракты:\n{previousContext}",
                new InferenceParams { Temperature = 0.0f, TopP = 0.95f, AntiPrompts = new[] { "User:" } }
            ),

            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
    }
}
```