using BounceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Model.Entities;
using Model.Services;

namespace BounceAPI.Controllers;

[ApiController]
[Route("hospitals/{hospitalId:int}")]
public class HospitalsController : ControllerBase
{
    private readonly ISchedulerService _schedulerService;

    public HospitalsController(ISchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }

    [HttpPost("reservation")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ValidationErrorModel), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(NotAuthenticatedErrorModel), StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(typeof(NotAuthorizedErrorModel), StatusCodes.Status403Forbidden)]
    public ReservationResponse Reserve(int hospitalId, ReservationHttpRequest httpRequest)
    {
        var reservationRequest = new ReservationRequest()
        {
            HospitalId = hospitalId,
            DoctorId = httpRequest.DoctorId
        };
        var reservation = _schedulerService.Process(reservationRequest);

        return new ReservationResponse(reservation, reservationRequest);
    }
}
