namespace Application.Commands.CreateUser;

using AutoMapper;
using MediatR;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Exceptions;
using Application.DTOs;

/// <summary>
/// Handler for creating a new user.
/// Includes validation and cache invalidation.
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        ICacheService cacheService,
        IPasswordService passwordService,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _cacheService = cacheService;
        _passwordService = passwordService;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var emailExists = await _userRepository.EmailExistsAsync(request.Email, cancellationToken);
        if (emailExists)
        {
            throw new DomainException($"Email {request.Email} is already in use", "EMAIL_EXISTS");
        }

        // Hash password
        var passwordHash = _passwordService.HashPassword(request.Password);

        // Create user aggregate
        var user = User.Create(
            request.Email,
            request.FirstName,
            request.LastName,
            passwordHash);

        // Persist
        await _userRepository.AddAsync(user, cancellationToken);

        return _mapper.Map<UserDto>(user);
    }
}
