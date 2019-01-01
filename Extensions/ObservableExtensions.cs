using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Extensions
{
    public static class ObservableExtensions
    {
        public static IConnectableObservable<T> ReplayOne<T>(this IObservable<T> stream)
        {
            return stream.Multicast(new ReplayOneSubject<T>());
        }

        public static IObservable<T> NeverComplete<T>(this IObservable<T> stream)
        {
            return stream.Concat(Observable.Never<T>());
        }

        public static IObservable<IReadOnlyList<T>> NonBlockingBuffer<T>(this IObservable<T> stream, TimeSpan period, IScheduler scheduler = null)
        {
            return Observable.Create<IReadOnlyList<T>>(observer =>
            {
                var buffer = new List<T>();

                var timer = Observable.Interval(period, scheduler ?? TaskPoolScheduler.Default)
                                      .Select(_ => Interlocked.CompareExchange(ref buffer, new List<T>(), buffer))
                                      .Subscribe(observer.OnNext);

                var subscription = stream.Subscribe(item => buffer.Add(item), observer.OnError, observer.OnCompleted);

                return new CompositeDisposable(timer, subscription, Disposable.Create(buffer.Clear));
            });
        }

        public static IObservable<T> ConnectNow<T>(this IConnectableObservable<T> stream, SingleAssignmentDisposable subscription)
        {
            subscription.Disposable = stream.Connect();
            return stream;
        }

        public static IObservable<T> LazilyConnect<T>(this IConnectableObservable<T> stream, SingleAssignmentDisposable futureSubscription)
        {
            var connected = 0;
            return Observable.Create<T>(observer =>
            {
                var subscription = stream.Subscribe(observer);
                if (Interlocked.CompareExchange(ref connected, 1, 0) == 0)
                {
                    if (!futureSubscription.IsDisposed)
                    {
                        futureSubscription.Disposable = stream.Connect();
                    }
                }

                return subscription;
            });
        }

        public static IConnectableObservable<T> ReplayUntilSubscribed<T>(this IObservable<T> stream)
        {
            return stream.Multicast(new ReplayUntilSubscribedSubject<T>());
        }

        public static IObservable<T> OnSubscribe<T>(this IObservable<T> stream, Action subscriptionAction)
        {
            return Observable.Create<T>(observer =>
            {
                subscriptionAction();
                return stream.Subscribe(observer);
            });
        }

        public static IObservable<T> OnUnSubscribed<T>(this IObservable<T> stream, Action unSubscribedAction)
        {
            return Observable.Create<T>(observer => new CompositeDisposable(stream.Subscribe(observer), Disposable.Create(unSubscribedAction)));
        }

        public static IObservable<T> AddDisposables<T>(this IObservable<T> stream, params IDisposable[] disposables)
        {
            return Observable.Create<T>(observer => new CompositeDisposable(disposables.Prepend(stream.Subscribe(observer))));
        }

        public static IObservable<Batch<T>> Batch<T>(this IObservable<T> stream, Func<T, bool> isStartFunction, Func<T, bool> isEndFunction)
        {
            return Observable.Create<Batch<T>>(observer =>
            {
                Batch<T> buffer = null;

                return stream.Subscribe(next =>
                {
                    if (isStartFunction(next))
                    {
                        buffer = new Batch<T>(BatchType.Initial);
                        return;
                    }

                    if (isEndFunction(next))
                    {
                        observer.OnNext(buffer);
                        buffer = null;
                        return;
                    }

                    if (buffer == null)
                    {
                        observer.OnNext(new Batch<T>(BatchType.Update, next));
                    }
                    else
                    {
                        buffer.Add(next);
                    }
                },
                observer.OnError,
                observer.OnCompleted);
            });
        }

        public static IObservable<List<T>> BatchOnlyWithin<T>(this IObservable<T> stream, Func<T, bool> isStartFunction, Func<T, bool> isEndFunction)
        {
            return Observable.Create<List<T>>(observer =>
            {
                List<T> buffer = null;

                return stream.Subscribe(next =>
                {
                    if (isStartFunction(next))
                    {
                        buffer = new List<T>();
                        return;
                    }

                    if (isEndFunction(next))
                    {
                        observer.OnNext(buffer);
                        buffer = null;
                        return;
                    }
                    
                    buffer?.Add(next);
                },
                observer.OnError,
                observer.OnCompleted);
            });
        }

        public static IObservable<T> IgnoreBetween<T>(this IObservable<T> stream, Func<T, bool> isStartFunction, Func<T, bool> isEndFunction)
        {
            return Observable.Create<T>(observer =>
            {
                var startTaking = true;

                return stream.Where(next =>
                {
                    if (isStartFunction(next))
                    {
                        startTaking = false;
                    }

                    if (startTaking) return true;

                    if (isEndFunction(next))
                    {
                        startTaking = true;
                        return false;
                    }

                    return false;
                })
                .Subscribe(observer);
            });
        }

        public static IObservable<T> SkipUntil<T>(this IObservable<T> stream, Func<T, bool> predicate)
        {
            return Observable.Create<T>(observer =>
            {
                var startTaking = false;

                return stream.Where(item =>
                {
                    if (startTaking) return true;

                    if (predicate(item))
                    {
                        startTaking = true;
                        return false;
                    }

                    return false;
                })
                .Subscribe(observer);
            });
        }

        public static IObservable<T> BackOffRetry<T>(this IObservable<T> source, Func<TimeSpan, TimeSpan> backOffUnfold, IScheduler scheduler)
        {
            return Observable.Create<T>(observer =>
            {
                var subscriptionDisposable = new SerialDisposable();
                var disposableOut = new CompositeDisposable(subscriptionDisposable);

                var initialTimeout = TimeSpan.Zero;

                void Next(T t)
                {
                    initialTimeout = TimeSpan.Zero;
                    observer.OnNext(t);
                }

                subscriptionDisposable.Disposable = source.Subscribe(Next,
                                                                     ex =>
                                                                     {
                                                                         var schedulerDisposable = new SerialDisposable();
                                                                         var innerSchedulerDisposable = new SerialDisposable();
                                                                         disposableOut.Add(schedulerDisposable);
                                                                         disposableOut.Add(innerSchedulerDisposable);
                                                                         schedulerDisposable.Disposable = scheduler.Schedule(ex, (error, self) =>
                                                                         {
                                                                             initialTimeout = backOffUnfold(initialTimeout);
                                                                             innerSchedulerDisposable.Disposable = scheduler.Schedule(initialTimeout, () => subscriptionDisposable.Disposable = source.Subscribe(Next, self, observer.OnCompleted));
                                                                         });
                                                                     },
                                                                     observer.OnCompleted);
                return disposableOut;
            });
        }

        public static IObservable<T> DoError<T>(this IObservable<T> stream, Action<Exception> onErrorAction)
        {
            return stream.Do(_ => { }, onErrorAction);
        }

        public static IObservable<T> EveryNthMessage<T>(this IObservable<T> stream, long nthMessageCount, Action<long> callback)
        {
            return Observable.Create<T>(observer =>
            {
                var counter = 0L;

                return stream.Do(_ =>
                {
                    if (++counter % nthMessageCount == 0)
                    {
                        callback(counter);
                    }
                })
                .Subscribe(observer);
            });
        }

        public static IObservable<T> TimeForFirstMessage<T>(this IObservable<T> stream, Action<TimeSpan> firstMessageCallback, Func<T, bool> predicate = null)
        {
            return Observable.Create<T>(observer =>
            {
                var stopwatch = Stopwatch.StartNew();

                return stream.Do(item =>
                {
                    if (stopwatch != null && (predicate?.Invoke(item) ?? true))
                    {
                        stopwatch.Stop();
                        firstMessageCallback(stopwatch.Elapsed);
                        stopwatch = null;
                    }
                })
                .Subscribe(observer);
            });
        }

        public static IObservable<Unit> ToUnit<T>(this IObservable<T> stream)
        {
            return stream.Select(_ => Unit.Default);
        }

        public static IObservable<NotifyCollectionChangedEventArgs> ObserveCollection(this INotifyCollectionChanged source)
        {
            return Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(h => source.CollectionChanged += h,
                                                                                                                      h => source.CollectionChanged -= h)
                             .Select(evt => evt.EventArgs);
        } 

        public static IObservable<PropertyChangedEventArgs> ObserveProperties(this INotifyPropertyChanged source)
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(h => source.PropertyChanged += h,
                                                                                                      h => source.PropertyChanged -= h)
                             .Select(evt => evt.EventArgs);
        }

        public static IObservable<TProperty> ObserveProperty<TSource, TProperty>(this TSource source, Expression<Func<TSource, TProperty>> propertyExpression, bool observeInitialValue = false)
            where TSource : INotifyPropertyChanged
        {
            return Observable.Create<TProperty>(observer =>
            {
                var propertyName = GetPropertyName(propertyExpression);
                var selector = CompiledExpressionHelper<TSource, TProperty>.GetFunc(propertyExpression);

                var observable = Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(h => source.PropertyChanged += h,
                                                                                                                    h => source.PropertyChanged -= h)
                                            .Where(evt => evt.EventArgs.PropertyName == propertyName)
                                            .Select(_ => selector(source));

                return (observeInitialValue ? observable.StartWith(selector(source)) : observable).Subscribe(observer);
            });
        }

        private static string GetPropertyName<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyExpression) where TSource : INotifyPropertyChanged
        {
            return CompiledExpressionHelper<TSource, TProperty>.GetMemberExpression(propertyExpression).Member.Name;
        }

        private sealed class CompiledExpressionHelper<TSource, TProperty>
        {
            private static readonly Dictionary<string, Func<TSource, TProperty>> Functions = new Dictionary<string, Func<TSource, TProperty>>();

            public static Func<TSource, TProperty> GetFunc(Expression<Func<TSource, TProperty>> propertyExpression)
            {
                var memberExpression = GetMemberExpression(propertyExpression);
                var propertyName = memberExpression.Member.Name;
                var key = typeof(TSource).FullName + "." + propertyName;

                lock (Functions)
                {
                    if (!Functions.TryGetValue(key, out var func))
                    {
                        return Functions[key] = propertyExpression.Compile();
                    }

                    return func;
                }
            }

            public static MemberExpression GetMemberExpression(Expression<Func<TSource, TProperty>> propertyExpression)
            {
                MemberExpression memberExpression;

                if (propertyExpression.Body is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
                {
                    memberExpression = (MemberExpression) unaryExpression.Operand;
                }
                else
                {
                    memberExpression = propertyExpression.Body as MemberExpression;
                }

                if (memberExpression == null || memberExpression.Expression.NodeType != ExpressionType.Parameter && memberExpression.Expression.NodeType != ExpressionType.Constant)
                {
                    throw new InvalidOperationException("Unable to get member from the expression provided.");
                }

                return memberExpression;
            }
        }

        private sealed class ReplayOneSubject<TItem> : ISubject<TItem>
        {
            private ReplaySubject<TItem> _stream;

            public ReplayOneSubject()
            {
                _stream = new ReplaySubject<TItem>(1);
            }

            public void OnCompleted()
            {
                _stream.OnCompleted();
            }

            public void OnError(Exception error)
            {
                _stream.OnError(error);
                _stream.Dispose();
                _stream = new ReplaySubject<TItem>(1);
            }

            public void OnNext(TItem value)
            {
                _stream.OnNext(value);
            }

            public IDisposable Subscribe(IObserver<TItem> observer)
            {
                return _stream.Subscribe(observer);
            }
        }

        private sealed class ReplayUntilSubscribedSubject<T> : ISubject<T>
        {
            private readonly Subject<T> _subject = new Subject<T>();
            private ReplaySubject<T> _replaySubject = new ReplaySubject<T>();
            private bool _isSubscribed;

            public void OnCompleted()
            {
                _replaySubject?.OnCompleted();
                _subject.OnCompleted();
            }

            public void OnError(Exception error)
            {
                _replaySubject?.OnError(error);
                _subject.OnError(error);
            }

            public void OnNext(T value)
            {
                if (_isSubscribed)
                    _subject.OnNext(value);
                else
                    _replaySubject.OnNext(value);
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                var disposable = (_replaySubject ?? Observable.Empty<T>()).Merge(_subject).Subscribe(observer);

                _isSubscribed = true;

                if (_replaySubject != null)
                {
                    _replaySubject.OnCompleted();
                    _replaySubject = null;
                }

                return disposable;
            }
        }
    }
}
