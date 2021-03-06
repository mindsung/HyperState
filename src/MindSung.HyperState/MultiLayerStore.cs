﻿using System;
using System.Threading.Tasks;

namespace MindSung.HyperState
{
    public static class MultiLayerStore
    {
        class _MultiLayerStore<TKey, TValue, TMid1, TMid2, TPersist> : IStore<TKey, TValue>
            where TValue : class
            where TMid1 : class
            where TMid2 : class
            where TPersist : class
        {
            public IStore<TKey, TValue> valueProvider;
            public Func<TMid1, TValue> valueFromMid1;
            public IStore<TKey, TMid1> mid1Provider;
            public Func<TMid2, TMid1> mid1FromMid2;
            public IStore<TKey, TMid2> mid2Provider;
            public Func<TPersist, TMid2> mid2FromPersist;
            public IStore<TKey, TPersist> persistProvider;
            public Func<TValue, TPersist> valueToPersist;

            public async Task<TValue> Get(TKey key)
            {
                var value = await valueProvider.Get(key);
                if (value != null) return value;

                if (mid1Provider != null)
                {
                    var mid1 = await mid1Provider.Get(key);
                    if (mid1 != null)
                    {
                        value = valueFromMid1(mid1);
                        await valueProvider.Put(key, value);
                    }

                    if (value == null && mid2Provider != null)
                    {
                        var mid2 = await mid2Provider.Get(key);
                        if (mid2 != null)
                        {
                            mid1 = mid1FromMid2(mid2);
                            await mid1Provider.Put(key, mid1);
                            value = valueFromMid1(mid1);
                            await valueProvider.Put(key, value);
                        }
                    }
                }

                if (value == null)
                {
                    var persist = await persistProvider.Get(key);
                    if (persist != null)
                    {
                        var mid2 = mid2FromPersist(persist);
                        if (mid2Provider != null) await mid2Provider.Put(key, mid2);
                        var mid1 = mid1FromMid2(mid2);
                        if (mid1Provider != null) await mid1Provider.Put(key, mid1);
                        value = valueFromMid1(mid1);
                        await valueProvider.Put(key, value);
                    }
                }

                return value;
            }

            public async Task<TValue> Put(TKey key, TValue value)
            {
                // Start at the bottom persisted level and repopulate back up.
                var mid2 = mid2FromPersist(await persistProvider.Put(key, valueToPersist(value)));
                if (mid2Provider != null) mid2 = await mid2Provider.Put(key, mid2);
                var mid1 = mid1FromMid2(mid2);
                if (mid1Provider != null) mid1 = await mid1Provider.Put(key, mid1);
                value = await valueProvider.Put(key, valueFromMid1(mid1));
                return value;
            }

            public async Task Delete(TKey key)
            {
                // Delete from the bottom up.
                await persistProvider.Delete(key);
                if (mid2Provider != null) await mid2Provider.Delete(key);
                if (mid1Provider != null) await mid1Provider.Delete(key);
                await valueProvider.Delete(key);
            }
        }

        public static IStore<TKey, TValue> Create<TKey, TValue, TMid1, TMid2, TPersist>(
            IStore<TKey, TValue> valueProvider,
            Func<TMid1, TValue> valueFromMid1,
            IStore<TKey, TMid1> mid1Provider,
            Func<TMid2, TMid1> mid1FromMid2,
            IStore<TKey, TMid2> mid2Provider,
            Func<TPersist, TMid2> mid2FromPersist,
            IStore<TKey, TPersist> persistProvider,
            Func<TValue, TPersist> valueToPersist)
            where TValue : class
            where TMid1 : class
            where TMid2 : class
            where TPersist : class
        {
            return new _MultiLayerStore<TKey, TValue, TMid1, TMid2, TPersist>
            {
                valueProvider = valueProvider,
                valueFromMid1 = valueFromMid1,
                mid1Provider = mid1Provider,
                mid1FromMid2 = mid1FromMid2,
                mid2Provider = mid2Provider,
                mid2FromPersist = mid2FromPersist,
                persistProvider = persistProvider,
                valueToPersist = valueToPersist
            };
        }

        public static IStore<TKey, TValue> Create<TKey, TValue, TMid1, TPersist>(
            IStore<TKey, TValue> valueProvider,
            Func<TMid1, TValue> valueFromMid1,
            IStore<TKey, TMid1> mid1Provider,
            Func<TPersist, TMid1> mid1FromPersist,
            IStore<TKey, TPersist> persistProvider,
            Func<TValue, TPersist> valueToPersist)
            where TValue : class
            where TMid1 : class
            where TPersist : class
        {
            return Create(valueProvider, valueFromMid1, mid1Provider, val => val, null, mid1FromPersist, persistProvider, valueToPersist);
        }

        public static IStore<TKey, TValue> Create<TKey, TValue, TPersist>(
            IStore<TKey, TValue> valueProvider,
            Func<TPersist, TValue> valueFromPersist,
            IStore<TKey, TPersist> persistProvider,
            Func<TValue, TPersist> valueToPersist)
            where TValue : class
            where TPersist : class
        {
            return Create(valueProvider, val => val, null, val => val, null, valueFromPersist, persistProvider, valueToPersist);
        }
    }
}
