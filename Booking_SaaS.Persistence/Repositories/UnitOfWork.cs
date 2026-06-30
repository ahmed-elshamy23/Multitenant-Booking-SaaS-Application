using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Persistence.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace Booking_SaaS.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly Lazy<IBookingRepository> _bookingRepository;
    private readonly AppDbContext _context;
    private readonly Lazy<IRefreshTokenRepository> _refreshTokenRepository;
    private readonly Dictionary<Type, object> _repositories;

    private readonly Lazy<IResourceRepository> _resourceRepository;
    private readonly Lazy<IScheduleRepository> _scheduleRepository;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        _repositories = new Dictionary<Type, object>();
        _resourceRepository = new Lazy<IResourceRepository>(() => new ResourceRepository(_context));
        _scheduleRepository = new Lazy<IScheduleRepository>(() => new ScheduleRepository(_context));
        _bookingRepository = new Lazy<IBookingRepository>(() => new BookingRepository(_context));
        _refreshTokenRepository = new Lazy<IRefreshTokenRepository>(() => new RefreshTokenRepository(_context));
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction != null)
            await _currentTransaction.DisposeAsync();

        await _context.DisposeAsync();
    }

    public IResourceRepository ResourceRepository => _resourceRepository.Value;
    public IScheduleRepository ScheduleRepository => _scheduleRepository.Value;
    public IBookingRepository BookingRepository => _bookingRepository.Value;
    public IRefreshTokenRepository RefreshTokenRepository => _refreshTokenRepository.Value;

    public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : BaseEntity<TKey>
    {
        var key = typeof(IGenericRepository<TEntity, TKey>);
        if (!_repositories.TryGetValue(key, out var repo))
        {
            repo = new GenericRepository<TEntity, TKey>(_context);
            _repositories[key] = repo;
        }

        return (IGenericRepository<TEntity, TKey>)repo;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
            throw new InvalidOperationException("A transaction is already active.");
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No active transaction.");

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No active transaction.");

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
}