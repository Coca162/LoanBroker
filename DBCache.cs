namespace LoanBroker;

using System.Collections.Concurrent;
using LoanBroker.Models;
using Microsoft.EntityFrameworkCore;

public class DBCacheItemAddition
{
    public Type Type { get; set; }
    public object Item { get; set; }

    public void AddToDB()
    {
        if (Type == typeof(BrokerAccount))
        {
            var account = (BrokerAccount)Item;
            if (DBCache.Get<BrokerAccount>(account.Id) is null)
                DBCache.dbctx.Add((BrokerAccount)Item);
        }
        else if (Type == typeof(TimeInfo))
            DBCache.dbctx.Add((TimeInfo)Item);
    }
}

public static class DBCache
{
    /// <summary>
    /// The high level cache object which contains the lower level caches
    /// </summary>
    public static Dictionary<Type, ConcurrentDictionary<long, object>> HCache = new();

    public static ConcurrentQueue<DBCacheItemAddition> ItemQueue = new();

    public static BrokerContext dbctx { get; set; }

    public static TimeInfo TimeInfo { get; set; }

    public static IEnumerable<T> GetAll<T>() where T : class
    {
        var type = typeof(T);

        if (!HCache.ContainsKey(type))
            yield break;

        foreach (T item in HCache[type].Values)
            yield return item;
    }

    /// <summary>
    /// Places an item into the cache
    /// </summary>
    public static void Remove<T>(long Id) where T : class
    {

        // Get the type of the item
        var type = typeof(T);

        // If there isn't a cache for this type, create one
        if (!HCache.ContainsKey(type))
            HCache.TryAdd(type, new ConcurrentDictionary<long, object>());

        if (!HCache[type].ContainsKey(Id))
        {
            HCache[type].Remove(Id, out _);
        }
    }

    /// <summary>
    /// Returns true if the cache contains the item
    /// </summary>
    public static bool Contains<T>(long Id) where T : class
    {
        var type = typeof(T);

        if (!HCache.ContainsKey(typeof(T)))
            return false;

        return HCache[type].ContainsKey(Id);
    }

    /// <summary>
    /// Places an item into the cache
    /// </summary>
    public static void Put<T>(long Id, T? obj) where T : class
    {
        // Empty object is ignored
        if (obj == null)
            return;

        // Get the type of the item
        var type = typeof(T);

        // If there isn't a cache for this type, create one
        if (!HCache.ContainsKey(type))
            HCache.Add(type, new ConcurrentDictionary<long, object>());

        if (!HCache[type].ContainsKey(Id))
        {
            HCache[type].TryAdd(Id, obj);
            //HCache[type][Id] = obj;
        }
    }

    public static void AddNew<T>(long Id, T? obj) where T : class
    {
        Put(Id, obj);
        ItemQueue.Enqueue(new() { Type = typeof(T), Item = obj });
    }

    /// <summary>
    /// Returns the item for the given id, or null if it does not exist
    /// </summary>
    public static T? Get<T>(long Id) where T : class
    {
        var type = typeof(T);

        if (HCache.ContainsKey(type))
            if (HCache[type].ContainsKey(Id))
                return HCache[type][Id] as T;

        return null;
    }

    public static T? Get<T>(long? Id) where T : class
    {
        if (Id is null)
            return null;
        var type = typeof(T);

        if (HCache.ContainsKey(type))
            if (HCache[type].ContainsKey((long)Id))
                return HCache[type][(long)Id] as T;
        return null;
    }

    public static async Task LoadAsync()
    {
        dbctx = BrokerContext.DbFactory.CreateDbContext();
        //#if !DEBUG

        TimeInfo = await dbctx.TimeInfos.FirstOrDefaultAsync();
        if (TimeInfo is null)
        {
            TimeInfo = new()
            {
                Id = 1000,
                LastLoanUpdate = DateTime.UtcNow
            };
            AddNew(TimeInfo.Id, TimeInfo);
        }

        foreach (var _obj in dbctx.BrokerAccounts)
            Put(_obj.Id, _obj);
        //#endif
    }

    public static async Task SaveAsync()
    {
        while (ItemQueue.Count > 0)
        {
            if (ItemQueue.TryDequeue(out var item))
                item.AddToDB();
        }
        await dbctx.SaveChangesAsync();
    }
}
