using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Iridium.DB
{
    internal static class Aggregator
    {
        private static readonly Dictionary<Type, IAggregator> _aggregators = new Dictionary<Type, IAggregator>
        {
            {typeof(int),new IntAggregator() },
            {typeof(int?),new NullableIntAggregator() },
            {typeof(long),new LongAggregator() },
            {typeof(long?),new NullableLongAggregator() },
            {typeof(double),new DoubleAggregator() },
            {typeof(double?),new NullableDoubleAggregator() },
            {typeof(float),new FloatAggregator() },
            {typeof(float?),new NullableFloatAggregator() },
            {typeof(decimal),new DecimalAggregator() },
            {typeof(decimal?),new NullableDecimalAggregator() },
            {typeof(DateTime),new DateTimeAggregator() },
            {typeof(DateTime?),new NullableDateTimeAggregator() },
        };

        public static T AggregateValues<T>(Aggregate agg, IEnumerable<T> values)
        {
            if (_aggregators.TryGetValue(typeof(T), out var aggregator))
                return (T)aggregator.Execute(agg, values);

            return default(T);
        }
    }

    internal interface IAggregator
    {
        object Execute(Aggregate agg, IEnumerable values);
    }

    internal abstract class Aggregator<T> : IAggregator
    {
        public object Execute(Aggregate agg, IEnumerable values)
        {
            return Execute(agg, (IEnumerable<T>)values);
        }

        public abstract T Execute(Aggregate agg, IEnumerable<T> values);
    }

    internal class IntAggregator : Aggregator<int>
    {
        public override int Execute(Aggregate agg, IEnumerable<int> objects)
        {
            switch (agg)
            {
                case Aggregate.Sum: return objects.Sum();
                case Aggregate.Max: return objects.Max();
                case Aggregate.Min: return objects.Min();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class NullableIntAggregator : Aggregator<int?>
    {
        public override int? Execute(Aggregate agg, IEnumerable<int?> objects)
        {
            switch (agg)
            {
                case Aggregate.Sum: return objects.Sum();
                case Aggregate.Max: return objects.Max();
                case Aggregate.Min: return objects.Min();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class LongAggregator : Aggregator<long>
    {
        public override long Execute(Aggregate agg, IEnumerable<long> objects)
        {
            switch (agg)
            {
                case Aggregate.Sum: return objects.Sum();
                case Aggregate.Max: return objects.Max();
                case Aggregate.Min: return objects.Min();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class NullableLongAggregator : Aggregator<long?>
    {
        public override long? Execute(Aggregate agg, IEnumerable<long?> objects)
        {
            switch (agg)
            {
                case Aggregate.Sum: return objects.Sum();
                case Aggregate.Max: return objects.Max();
                case Aggregate.Min: return objects.Min();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class DoubleAggregator : Aggregator<double>
    {
        public override double Execute(Aggregate agg, IEnumerable<double> objects)
        {
            switch (agg)
            {
                case Aggregate.Sum: return objects.Sum();
                case Aggregate.Average: return objects.Average();
                case Aggregate.Max: return objects.Max();
                case Aggregate.Min: return objects.Min();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class NullableDoubleAggregator : Aggregator<double?>
    {
        public override double? Execute(Aggregate agg, IEnumerable<double?> objects)
        {
            switch (agg)
            {
                case Aggregate.Sum: return objects.Sum();
                case Aggregate.Average: return objects.Average();
                case Aggregate.Max: return objects.Max();
                case Aggregate.Min: return objects.Min();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class FloatAggregator : Aggregator<float>
    {
        public override float Execute(Aggregate agg, IEnumerable<float> objects)
        {
            switch (agg)
            {
                case Aggregate.Sum: return objects.Sum();
                case Aggregate.Max: return objects.Max();
                case Aggregate.Min: return objects.Min();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class NullableFloatAggregator : Aggregator<float?>
    {
        public override float? Execute(Aggregate agg, IEnumerable<float?> objects)
        {
            switch (agg)
            {
                case Aggregate.Sum: return objects.Sum();
                case Aggregate.Average: return objects.Average();
                case Aggregate.Max: return objects.Max();
                case Aggregate.Min: return objects.Min();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class DecimalAggregator : Aggregator<decimal>
    {
        public override decimal Execute(Aggregate agg, IEnumerable<decimal> objects)
        {
            switch (agg)
            {
                case Aggregate.Sum: return objects.Sum();
                case Aggregate.Average: return objects.Average();
                case Aggregate.Max: return objects.Max();
                case Aggregate.Min: return objects.Min();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class NullableDecimalAggregator : Aggregator<decimal?>
    {
        public override decimal? Execute(Aggregate agg, IEnumerable<decimal?> objects)
        {
            switch (agg)
            {
                case Aggregate.Sum: return objects.Sum();
                case Aggregate.Average: return objects.Average();
                case Aggregate.Max: return objects.Max();
                case Aggregate.Min: return objects.Min();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class DateTimeAggregator : Aggregator<DateTime>
    {
        public override DateTime Execute(Aggregate agg, IEnumerable<DateTime> objects)
        {
            switch (agg)
            {
                case Aggregate.Max: return objects.Max();
                case Aggregate.Min: return objects.Min();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class NullableDateTimeAggregator : Aggregator<DateTime?>
    {
        public override DateTime? Execute(Aggregate agg, IEnumerable<DateTime?> objects)
        {
            switch (agg)
            {
                case Aggregate.Max: return objects.Max();
                case Aggregate.Min: return objects.Min();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }


}