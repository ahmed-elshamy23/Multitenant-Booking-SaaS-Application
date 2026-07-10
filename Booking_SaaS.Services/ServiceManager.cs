using AutoMapper;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Authentication;
using Booking_SaaS.Services.Abstraction.DTOs.Bookings;
using Booking_SaaS.Services.Abstraction.DTOs.Email;
using Booking_SaaS.Services.Abstraction.DTOs.Resource;
using Booking_SaaS.Services.Abstraction.DTOs.Schedule;
using Booking_SaaS.Services.Abstraction.DTOs.Tenant;
using Booking_SaaS.Services.Abstraction.Options;
using Booking_SaaS.Services.Abstraction.Orchestrators;
using Booking_SaaS.Services.Orchestrators;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Booking_SaaS.Services;

public class ServiceManager : IServiceManager
{
    private readonly Lazy<IAuthenticationOrchestrator> _authenticationOrchestrator;
    private readonly Lazy<IBookingOrchestrator> _bookingOrchestrator;
    private readonly Lazy<IResourceOrchestrator> _resourceOrchestrator;
    private readonly Lazy<ITenantService> _tenantService;

    public ServiceManager(UserManager<AppUser> userManager, IOptions<JwtOptions> jwtOptions,
        IValidator<RegisterDto> registerValidator, IValidator<ChangePasswordDto> changePasswordValidator,
        IOptions<DomainOptions> domainOptions, IOptions<EmailOptions> emailOptions,
        ITenantResolver tenantResolver, IUnitOfWork unitOfWork, IValidator<TenantAddDto> tenantValidator,
        IValidator<PaginatedDto<TenantResultDto>> tenantPaginationValidator, IMapper mapper,
        IValidator<ResourceAddDto> resourceValidator,
        IValidator<PaginatedDto<ResourceResultDto>> resourcePaginationValidator,
        IValidator<ResourceFilterDto> resourceFilterValidator, IValidator<ScheduleFilterDto> scheduleFilterValidator,
        IValidator<PaginatedDto<ScheduleResultDto>> schedulePaginationValidator,
        IValidator<ScheduleAddDto> scheduleValidator, IValidator<BookingFilterDto> bookingFilterValidator,
        IValidator<PaginatedDto<BookingDto>> bookingPaginationValidator, IValidator<BookingAddDto> bookingValidator,
        ICachingService cachingService)
    {
        var emailService = new Lazy<IEmailService>(() => new EmailService(emailOptions));

        _tenantService =
            new Lazy<ITenantService>(() =>
                new TenantService(unitOfWork, tenantValidator, tenantPaginationValidator, mapper));

        var resourceService = new Lazy<IResourceService>(() =>
            new ResourceService(resourceValidator, unitOfWork, mapper, resourcePaginationValidator,
                resourceFilterValidator));

        var scheduleService = new Lazy<IScheduleService>(() => new ScheduleService(unitOfWork, scheduleFilterValidator,
            mapper, schedulePaginationValidator, scheduleValidator));

        var bookingService = new Lazy<IBookingService>(() =>
            new BookingService(unitOfWork, bookingFilterValidator, mapper, bookingPaginationValidator));

        _resourceOrchestrator = new Lazy<IResourceOrchestrator>(() =>
            new ResourceOrchestrator(_tenantService.Value, tenantResolver, resourceService.Value,
                scheduleService.Value, bookingService.Value, unitOfWork));

        var authenticationService = new Lazy<IAuthenticationService>(() =>
            new AuthenticationService(userManager, jwtOptions, registerValidator, changePasswordValidator, unitOfWork));

        _authenticationOrchestrator = new Lazy<IAuthenticationOrchestrator>(() =>
            new AuthenticationOrchestrator(authenticationService.Value, _tenantService.Value, tenantResolver,
                emailService.Value, domainOptions, unitOfWork));

        _bookingOrchestrator = new Lazy<IBookingOrchestrator>(() => new BookingOrchestrator(_tenantService.Value,
            tenantResolver, scheduleService.Value, bookingService.Value, unitOfWork, bookingValidator, cachingService));
    }

    public ITenantService TenantService => _tenantService.Value;

    public IAuthenticationOrchestrator AuthenticationOrchestrator => _authenticationOrchestrator.Value;
    public IResourceOrchestrator ResourceOrchestrator => _resourceOrchestrator.Value;
    public IBookingOrchestrator BookingOrchestrator => _bookingOrchestrator.Value;
}