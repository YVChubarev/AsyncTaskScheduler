namespace AsyncTaskScheduler.WeakEvents
{

    /// <summary>
    /// Событие, которое хранит слабые ссылки на подписчиков.
    /// Подписчик может быть собран GC, даже если не отписался.
    /// ВАЖНО: подписка через метод экземпляра. Лямбды не работают.
    /// </summary>
    public sealed class WeakEvent<T> (object publisher)
    {
        private readonly List<WeakReference<EventHandler<T>>> _handlers = [];
        private readonly object _lock = new();

        public event EventHandler<T> Handler
        {
            add
            {
                if (value is null) return;

                lock (_lock)
                {
                    foreach (var d in value.GetInvocationList())
                    {
                        if (d is not EventHandler<T> action) continue;
                        _handlers.Add(new WeakReference<EventHandler<T>>(action));
                    }
                }
            }
            remove
            {
                if (value is null) return;

                lock (_lock)
                {
                    foreach (var d in value.GetInvocationList())
                    {
                        if (d is not EventHandler<T> action) continue;
                        for (int i = _handlers.Count - 1; i >= 0; i--)
                        {
                            if (!_handlers[i].TryGetTarget(out var target))
                            {
                                _handlers.RemoveAt(i);
                                continue;
                            }

                            if (target.Target == action.Target && target.Method == action.Method)
                            {
                                _handlers.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void Raise(T args)
        {
            List<EventHandler<T>> alive;

            lock (_lock)
            {
                alive = new(_handlers.Count);

                for (int i = _handlers.Count-1; i >= 0; i--)
                {
                    if (_handlers[i].TryGetTarget(out var action))
                        alive.Add(action);
                    else
                        _handlers.RemoveAt(i);
                }
            }

            alive.Reverse();
            foreach (var action in alive) action(publisher, args);
        }
    }
}
