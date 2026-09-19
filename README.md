## AsyncTaskScheduler
Консольное приложение для асинхронного выполнения задач с ограничением параллелизма.
## Что делает
- Читает задачи из `tasks.json`
- Выполняет с ограничением до 3 одновременно (SemaphoreSlim)
- Поддерживает отмену по нажатию клавиши (CancellationToken)
## Как запустить
dotnet run
## Стек
.NET 10, C#, System.Text.Json, async/await

## Changelog

### 1.1
- Логирование вынесено в `IStatusLogger<T>` / `ConsoleLogger<T>` (DIP, SRP).
- `TimeProvider` — зависимость логгера (тестируемость, DIP).
- Время в логах — **UTC** вместо локального (корреляция логов между сервисами).
- `TaskPhase` — фазы жизненного цикла задачи: `Start`, `Stop`, `Cancel`.
- `SystemStatus` — системные события: `Shutdown`.