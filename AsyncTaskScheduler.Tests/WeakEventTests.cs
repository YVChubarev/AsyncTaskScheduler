using AsyncTaskScheduler.WeakEvents;
using FluentAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace AsyncTaskScheduler.Tests
{
    public class WeakEventTests
    {
        public class ImportService
        {

            public WeakEvent<int> Event { get; }

            public ImportService()
            {
                Event = new(this);
            }
        }

        public class Tracker
        {
            private readonly Action<object?, int> _action;
            public Tracker(ImportService service, Action<object?, int> action)
            {
                service.Event.Handler += OnProgress;
                _action = action;
            }

            private void OnProgress(object? sender, int progress)
            {
                _action(sender, progress);
            }
        }

        [Fact]
        public void Raise_CorrectEvent_DoesInvoke()
        {
            int counter = 0;
            int i = 0;
            object? s = null;
            ImportService service = new();
            Tracker tracker = new(service, (sender, args) =>
            {
                counter++;
                i = args;
                s = sender;
            });
            service.Event.Raise(1);
            counter.Should().Be(1);
            i.Should().Be(1);
            s.Should().Be(service);
        }

        [Fact]
        public void Raise_Removed_DoesNotInvoke()
        {
            int counter = 0;
            ImportService service = new();
            SubscribeTrackerAndForget(service, (sender, args) => counter++);

            CollectGarbage();

            service.Event.Raise(1);
            counter.Should().Be(0);
        }

        [Fact]
        public void Raise_AfterUnsubscribe_DoesNotInvoke()
        {
            int counter = 0;
            int i = 0;
            object? s = null;
            void handler(object? sender, int e)
            {
                counter++;
                i = e;
                s = sender;
            }

            ImportService service = new();
            service.Event.Handler += handler;
            service.Event.Raise(1);
            counter.Should().Be(1);
            i.Should().Be(1);
            s.Should().Be(service);

            service.Event.Handler -= handler;
            service.Event.Raise(2);

            counter.Should().Be(1);
            i.Should().Be(1);
            s.Should().Be(service);
        }

        [Fact]
        public void Raise_Lambda()
        {
            int counter = 0;

            ImportService service = new();
            SubscribeLambdaAndForget(service, () => counter++);

            CollectGarbage();

            service.Event.Raise(1);
            counter.Should().Be(0);
        }

        [Fact]
        public void Raise_MultipleSubscribers_InvokedInSubscriptionOrder()
        {
            var order = new List<string>();
            ImportService service = new();

            // важно: держим ссылки в переменных, иначе GC их соберёт
            var t1 = new Tracker(service, (_, _) => order.Add("first"));
            var t2 = new Tracker(service, (_, _) => order.Add("second"));
            var t3 = new Tracker(service, (_, _) => order.Add("third"));

            service.Event.Raise(0);

            order.Should().Equal("first", "second", "third");
        }

        private static void CollectGarbage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SubscribeTrackerAndForget(ImportService service, Action<object?, int> action)
        {
            _ = new Tracker(service, action);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SubscribeLambdaAndForget(ImportService service, Action action)
        {
            service.Event.Handler += (sender, e) => action();
        }
    }
}
