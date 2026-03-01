using AIMS.BackendServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AIMS.BackendServer.Services;

// ── Cache key model ────────────────────────────────────────────
public record PermissionCacheKey(string FunctionId, string CommandId);

// ── Interface ──────────────────────────────────────────────────
public interface IPermissionCacheService
{
    Task<HashSet<PermissionCacheKey>> GetUserPermissionsAsync(string userId);
    void InvalidateUser(string userId);
    void InvalidateAll();
}

// ── Implementation ─────────────────────────────────────────────
public class PermissionCacheService : IPermissionCacheService
{
    private readonly IMemoryCache _cache;
    private readonly AimsDbContext _db;

    // Cache tồn tại 10 phút, sliding (gia hạn mỗi lần truy cập)
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    // Prefix key để dễ invalidate theo pattern
    private const string KeyPrefix = "permissions_";

    public PermissionCacheService(IMemoryCache cache, AimsDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    // ─────────────────────────────────────────────────────────
    // Lấy permissions của user — từ cache hoặc query DB
    // ─────────────────────────────────────────────────────────
    public async Task<HashSet<PermissionCacheKey>> GetUserPermissionsAsync(
        string userId)
    {
        var cacheKey = $"{KeyPrefix}{userId}";

        // ── Cache HIT → trả về ngay ───────────────────────────
        if (_cache.TryGetValue(cacheKey,
            out HashSet<PermissionCacheKey>? cached) && cached != null)
        {
            return cached;
        }

        // ── Cache MISS → query DB ─────────────────────────────
        var roleIds = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var permissions = await _db.Permissions
            .Where(p => roleIds.Contains(p.RoleId))
            .Select(p => new PermissionCacheKey(p.FunctionId, p.CommandId))
            .ToListAsync();

        var permSet = permissions.ToHashSet();

        // ── Lưu vào cache với sliding expiration ─────────────
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(CacheDuration)
            .SetAbsoluteExpiration(TimeSpan.FromHours(1)) // Tối đa 1 giờ
            .SetPriority(CacheItemPriority.Normal);

        _cache.Set(cacheKey, permSet, cacheOptions);

        return permSet;
    }

    // ─────────────────────────────────────────────────────────
    // Xóa cache của 1 user cụ thể
    // Gọi khi: đổi role, đổi password, deactivate user
    // ─────────────────────────────────────────────────────────
    public void InvalidateUser(string userId)
    {
        var cacheKey = $"{KeyPrefix}{userId}";
        _cache.Remove(cacheKey);
    }

    // ─────────────────────────────────────────────────────────
    // Xóa cache của TẤT CẢ users
    // Gọi khi: PUT /permissions (thay đổi permission của role)
    // ─────────────────────────────────────────────────────────
    public void InvalidateAll()
    {
        // IMemoryCache không có GetAllKeys() nên dùng trick:
        // Lưu danh sách userId đang được cache vào 1 key đặc biệt
        if (_cache.TryGetValue("cached_user_ids",
            out HashSet<string>? userIds) && userIds != null)
        {
            foreach (var uid in userIds)
                _cache.Remove($"{KeyPrefix}{uid}");

            _cache.Remove("cached_user_ids");
        }
    }
}