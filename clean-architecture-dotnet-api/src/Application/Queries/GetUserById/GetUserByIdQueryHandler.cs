namespace Application.Queries.GetUserById;

using AutoMapper;
using MediatR;
using Domain.Interfaces;
using Domain.Exceptions;
using Application.DTOs;

/// <summary>
/// Query handler with Redis caching for user retrieval.
/// Demonstrates cache-aside pattern.
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private const string CacheKeyPrefix = "user:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;

    public GetUserByIdQueryHandler(
        IUserRepository userRepository,
        ICacheService cacheService,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _cacheService = cacheService;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{request.UserId}";

        // Try to get from cache first
        var cachedUser = await _cacheService.GetAsync<UserDto>(cacheKey, cancellationToken);
        if (cachedUser is not null)
        {
            return cachedUser;
        }

        // If not in cache, get from database
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException($"User with ID {request.UserId} not found");
        }

        var userDto = _mapper.Map<UserDto>(user);

        // Populate cache
        await _cacheService.SetAsync(cacheKey, userDto, CacheDuration, cancellationToken);

        return userDto;
    }
}
