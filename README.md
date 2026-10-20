## AsyncTaskScheduler
![CI](https://github.com/YVChubarev/AsyncTaskScheduler/actions/workflows/ci.yml/badge.svg)

Консольное приложение для асинхронного выполнения задач с ограничением параллелизма.
## Что делает
- Читает задачи из `tasks.json`
- Выполняет с ограничением до 3 одновременно (SemaphoreSlim)
- Поддерживает отмену по нажатию клавиши (CancellationToken)
## Как запустить
dotnet run
## Стек
.NET 10, C#, System.Text.Json, async/await

## Архитектура

Рефакторинг по SOLID:

- **SRP** — каждая ответственность в своём классе:
  `ConsoleLogger`, `TaskItemExecutor`, `TaskItemsOrchestrator`, `KeyPressTrigger`.
- **DIP** — зависимости через абстракции:
  `IStatusLogger<T>`, `ITaskItemExecutor`, `ITrigger`, `TimeProvider`.
- **ISP** — `IStatusLogger<TaskPhase>` и `IStatusLogger<SystemStatus>` разделены по клиентам.
- **OCP** — новые статусы/триггеры/логгеры добавляются без правки существующих классов.

Composition root — `Program.cs`. Все зависимости собираются в одном месте.

## Changelog

### 1.1
- Логирование вынесено в `IStatusLogger<T>` / `ConsoleLogger<T>` (DIP, SRP).
- `TimeProvider` — зависимость логгера (тестируемость, DIP).
- Время в логах — **UTC** вместо локального (корреляция логов между сервисами).
- `TaskPhase` — фазы жизненного цикла задачи: `Start`, `Stop`, `Cancel`.
- `SystemStatus` — системные события: `Shutdown`.

### 1.2
- Ожидание отмены вынесено в `ITrigger` / `KeyPressTrigger`
- упрощение вызова **using**

### 1.3
- Процесс выполнения `TaskItem` вынесен в `ITaskItemExecutor` / `TaskItemExecutor`

### 1.4
- Процесс параллельного выполнения `TaskItem[]` вынесен в `TaskItemsOrchestrator`

### 1.5
- `ConsoleLogger` заменен на `StatusLogger`
- Выравнивание статусов в логах теперь вычисляется автолатически по максимальной длине статуса.
- `StatusLogger` покрыт тестами